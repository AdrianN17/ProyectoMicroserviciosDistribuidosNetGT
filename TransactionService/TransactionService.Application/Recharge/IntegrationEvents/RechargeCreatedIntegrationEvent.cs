namespace TransactionService.Application.Recharge.IntegrationEvents;

/// <summary>
/// Evento publicado por TransactionService cuando se crea una recarga en estado PENDING.
/// WalletService es el consumidor responsable de acreditar el saldo.
/// </summary>
public sealed record RechargeCreatedIntegrationEvent(
    Guid    RechargeId,
    Guid    WalletId,
    decimal Amount,
    string  Currency,
    string  MethodType,
    decimal ExchangeRate
);
