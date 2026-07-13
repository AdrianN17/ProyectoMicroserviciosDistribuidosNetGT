# ProyectoMicroserviciosNetGT


<img width="800" height="1091" alt="Diagrama_net_microservicios-Diagrama drawio" src="https://github.com/user-attachments/assets/492233d2-9972-4e51-b379-2a424c8d8c81" />
<img width="755" height="1290" alt="Diagrama_net_microservicios-BD drawio" src="https://github.com/user-attachments/assets/3a4a85fd-0e5e-4ef6-b472-7433d62061c0" />

---

## Saga Coreografiada — Transferencia entre Wallets

La transferencia entre wallets se implementa mediante un **Saga Coreografiado** (Choreography-Based Saga). No existe un orquestador central: cada microservicio reacciona de forma autónoma a los eventos publicados en Azure Service Bus y decide el siguiente paso.

La idempotencia se garantiza en dos niveles:
- **TransactionService**: el cliente envía un `TransactionId` (UUID v4) junto a cada petición. Si ya existe, se devuelve el ID sin crear un duplicado.
- **WalletService**: la tabla `ProcessedTransactions` registra cada `TransactionId` procesado. Si el bus reenvía el mensaje (retry), el handler lo descarta antes de tocar los saldos.

---

### Flujo Positivo

```mermaid
sequenceDiagram
    autonumber
    actor Cliente
    participant TS as TransactionService
    participant ASB as Azure Service Bus
    participant WS as WalletService

    Cliente->>TS: POST /transactions {TransactionId, from, to, amount, currency}
    TS->>TS: Idempotencia: ¿TransactionId ya existe?
    alt Ya existe
        TS-->>Cliente: 200 OK — ID existente (sin duplicar)
    else Es nuevo
        TS->>TS: Transaction.Create() → estado PENDING
        TS->>TS: SaveChanges → dispara TransactionCreatedDomainEvent
        TS->>ASB: Publica TransactionCreatedMessage\n(cola: transaction-created)
        TS-->>Cliente: 201 Created — TransactionId

        ASB->>WS: TransactionCreatedMessage
        WS->>WS: Idempotencia: ¿TransactionId en ProcessedTransactions?
        WS->>WS: Busca fromWallet y toWallet
        WS->>WS: Valida estado, límite diario y saldo
        WS->>WS: Convierte moneda si aplica
        WS->>WS: Debita fromWallet / Acredita toWallet
        WS->>WS: SaveChanges atómico\n(wallets + ProcessedTransaction)
        WS->>ASB: Publica TransactionCompletedMessage\n(cola: transaction-completed)

        ASB->>TS: TransactionCompletedMessage
        TS->>TS: Idempotencia: ¿ya está COMPLETED?
        TS->>TS: Transaction.Complete() → estado COMPLETED
    end
```

---

### Flujos Negativos

```mermaid
sequenceDiagram
    autonumber
    participant ASB as Azure Service Bus
    participant WS as WalletService
    participant TS as TransactionService

    Note over WS: El mensaje TransactionCreated ya fue recibido

    alt fromWallet no existe
        WS->>ASB: TransactionFailedMessage (FROM_WALLET_NOT_FOUND)
    else toWallet no existe
        WS->>ASB: TransactionFailedMessage (TO_WALLET_NOT_FOUND)
    else Wallet bloqueada
        WS->>ASB: TransactionFailedMessage (WALLET_BLOCKED)
    else Límite diario superado
        WS->>ASB: TransactionFailedMessage (LIMIT_EXCEEDED)
    else Saldo insuficiente
        WS->>ASB: TransactionFailedMessage (INSUFFICIENT_BALANCE)
    else Error interno de BD (rollback EF Core)
        WS->>ASB: TransactionFailedMessage (INTERNAL_ERROR)
    end

    ASB->>TS: TransactionFailedMessage
    TS->>TS: Idempotencia: ¿ya está FAILED?
    TS->>TS: Transaction.Fail(reason) → estado FAILED
```

---

## Saga Coreografiada — Recarga de Wallet

La recarga de saldo también se implementa con el mismo patrón **Choreography-Based Saga**. El cliente crea una recarga en `TransactionService`, y `WalletService` es el responsable de acreditar el saldo y confirmar o rechazar la operación.

La idempotencia se garantiza en dos niveles:
- **TransactionService**: los handlers `CompleteRecharge` y `FailRecharge` verifican el estado actual antes de modificarlo. Si ya está en el estado destino, ignoran el evento sin error.
- **WalletService**: la tabla `ProcessedRecharges` registra cada `RechargeId` procesado. Si el bus reenvía el mensaje, el handler lo descarta antes de tocar el saldo.

---

### Flujo Positivo

```mermaid
sequenceDiagram
    autonumber
    actor Cliente
    participant TS as TransactionService
    participant ASB as Azure Service Bus
    participant WS as WalletService

    Cliente->>TS: POST /recharges {walletId, amount, currency, methodType}
    TS->>TS: Obtiene tipo de cambio (ExchangeRateService)
    TS->>TS: Recharge.Create() → estado PENDING
    TS->>TS: SaveChanges → dispara RechargeCreatedDomainEvent
    TS->>ASB: Publica RechargeCreatedMessage\n(cola: recharge-created)
    TS-->>Cliente: 201 Created — RechargeId

    ASB->>WS: RechargeCreatedMessage
    WS->>WS: Idempotencia: ¿RechargeId en ProcessedRecharges?
    WS->>WS: Busca wallet destino
    WS->>WS: Valida estado de la wallet (ACTIVE)
    WS->>WS: Calcula monto convertido con ExchangeRate guardado
    WS->>WS: wallet.UpdateBalance(Addition)
    WS->>WS: SaveChanges atómico\n(wallet + ProcessedRecharge)
    WS->>ASB: Publica RechargeCompletedMessage\n(cola: recharge-completed)

    ASB->>TS: RechargeCompletedMessage
    TS->>TS: Idempotencia: ¿ya está COMPLETED?
    TS->>TS: Recharge.Complete() → estado COMPLETED
```

---

### Flujos Negativos

```mermaid
sequenceDiagram
    autonumber
    participant ASB as Azure Service Bus
    participant WS as WalletService
    participant TS as TransactionService

    Note over WS: El mensaje RechargeCreated ya fue recibido

    alt Moneda inválida en el mensaje
        WS->>ASB: RechargeFailedMessage (INVALID_CURRENCY)
    else Wallet no existe
        WS->>ASB: RechargeFailedMessage (WALLET_NOT_FOUND)
    else Wallet bloqueada
        WS->>ASB: RechargeFailedMessage (WALLET_BLOCKED)
    else Error interno de BD (rollback EF Core)
        WS->>ASB: RechargeFailedMessage (INTERNAL_ERROR)
    end

    ASB->>TS: RechargeFailedMessage
    TS->>TS: Idempotencia: ¿ya está FAILED?
    TS->>TS: Recharge.Fail(reason) → estado FAILED
```

---

### Cancelación de Recarga (DELETE)

La cancelación solo está permitida cuando la recarga ya está en estado `COMPLETED`. Al cancelar, `TransactionService` envía una operación de débito a `WalletService` vía la cola existente (`SendOperation`) para revertir el saldo acreditado.

```mermaid
sequenceDiagram
    autonumber
    actor Cliente
    participant TS as TransactionService
    participant ASB as Azure Service Bus
    participant WS as WalletService

    Cliente->>TS: DELETE /recharges/{rechargeId}
    TS->>TS: Busca recarga
    TS->>TS: Recharge.SoftDelete()\n⚠️ Solo si está COMPLETED
    alt Estado inválido (PENDING / FAILED / CANCELLED)
        TS-->>Cliente: 409 Conflict — recharge.invalid_state
    else COMPLETED
        TS->>TS: Busca wallet para obtener moneda
        TS->>TS: SaveChanges → estado CANCELLED
        TS->>ASB: Publica SendOperation (Subtract)\n(cola: legacy UpdateBalance)
        TS-->>Cliente: 200 OK — RechargeId
        ASB->>WS: UpdateBalanceConsumer → Débita saldo
    end
```

---

### Contratos de Mensajes (Azure Service Bus — Basic tier, solo colas)

#### Transferencia entre Wallets

| Cola | Publicado por | Consumido por | Tipo de mensaje |
|---|---|---|---|
| `transaction-created` | TransactionService | WalletService | `TransactionCreatedMessage` |
| `transaction-completed` | WalletService | TransactionService | `TransactionCompletedMessage` |
| `transaction-failed` | WalletService | TransactionService | `TransactionFailedMessage` |

#### Recarga de Wallet

| Cola | Publicado por | Consumido por | Tipo de mensaje |
|---|---|---|---|
| `recharge-created` | TransactionService | WalletService | `RechargeCreatedMessage` |
| `recharge-completed` | WalletService | TransactionService | `RechargeCompletedMessage` |
| `recharge-failed` | WalletService | TransactionService | `RechargeFailedMessage` |

Todos los tipos de mensaje llevan el atributo `[MessageUrn]` con un URN canónico para que MassTransit enrute correctamente entre servicios sin compartir ensamblados.

---

### Idempotencia ante reintentos de ASB

#### Transferencia

| Evento reenviado | Comportamiento |
|---|---|
| `TransactionCreated` (reintento) | WalletService consulta `ProcessedTransactions` → ya existe → descarta sin modificar saldos |
| `TransactionCompleted` (reintento) | TransactionService verifica `Status == COMPLETED` → ignora sin error |
| `TransactionFailed` (reintento) | TransactionService verifica `Status == FAILED` → ignora sin error |
| `POST /transactions` con mismo `TransactionId` | TransactionService devuelve el ID sin crear una nueva transacción |

#### Recarga

| Evento reenviado | Comportamiento |
|---|---|
| `RechargeCreated` (reintento) | WalletService consulta `ProcessedRecharges` → ya existe → descarta sin acreditar saldo |
| `RechargeCompleted` (reintento) | TransactionService verifica `Status == COMPLETED` → ignora sin error |
| `RechargeFailed` (reintento) | TransactionService verifica `Status == FAILED` → ignora sin error |

---

### Estados de la Transacción

```mermaid
stateDiagram-v2
    [*] --> PENDING : Transaction.Create()
    PENDING --> COMPLETED : WalletService procesa exitosamente
    PENDING --> FAILED : WalletService rechaza (saldo, límite, etc.)
    PENDING --> CANCELLED : SoftDelete()
    COMPLETED --> [*]
    FAILED --> [*]
    CANCELLED --> [*]
```

### Estados de la Recarga

```mermaid
stateDiagram-v2
    [*] --> PENDING : Recharge.Create()
    PENDING --> COMPLETED : WalletService acredita exitosamente
    PENDING --> FAILED : WalletService rechaza (wallet bloqueada, error, etc.)
    COMPLETED --> CANCELLED : SoftDelete() → revierte saldo vía SendOperation
    COMPLETED --> [*]
    FAILED --> [*]
    CANCELLED --> [*]
```
