using TransactionService.Domain.Events;

namespace TransactionService.Domain.Entities;

public class Recharge : AggregateRoot<RechargeId>
{
    public WalletId WalletId { get; private set; }
    public Amount Amount { get; private set; }
    public MethodType MethodType { get; private set; }
    public RechargeStatus RechargeStatus { get; private set; }

    /// <summary>Motivo del fallo, poblado cuando Status = FAILED.</summary>
    public string? FailureReason { get; private set; }

    private Recharge()
    {
    }

    public static Recharge Create(Guid rechargeId, Guid walletId, decimal amount, CurrencyType currency,
        MethodType methodType, decimal exchangeRate)
    {
        var errors = ValidateFieldsRequired(walletId, amount, currency, methodType, exchangeRate);

        if (errors.Any())
            throw new DomainValidationException("recharge.invalid", "Validation failed", errors);

        var recharge = new Recharge();

        try
        {
            recharge = new Recharge
            {
                Id = new RechargeId(rechargeId),
                WalletId = new WalletId(walletId),
                Amount = Amount.Create(amount, currency, exchangeRate),
                MethodType = methodType,
                RechargeStatus = RechargeStatus.PENDING
            };

            recharge.AddDomainEvent(new RechargeCreatedDomainEvent(
                rechargeId:  recharge.Id.Value,
                walletId:    walletId,
                amount:      amount,
                currency:    currency.ToString(),
                methodType:  methodType.ToString(),
                exchangeRate: exchangeRate
            ));
        }
        catch (InvalidValueObjectException iv)
        {
            var errorsVo = new Dictionary<string, string[]>();

            foreach (var kv in iv.Errors)
            {
                var key = kv.Key ?? string.Empty;

                var targetKey = key switch
                {
                    "walletId" => "walletId",
                    "amount" or "value" or "money" => "amount",
                    "currency" => "currency",
                    "exchangeRate" => "exchangeRate",
                    _ => key
                };

                errorsVo[targetKey] = kv.Value;
            }

            foreach (var kv in errorsVo)
            {
                if (errors.ContainsKey(kv.Key))
                {
                    var merged = errors[kv.Key].Concat(kv.Value).Distinct().ToArray();
                    errors[kv.Key] = merged;
                }
                else
                {
                    errors[kv.Key] = kv.Value;
                }
            }
        }

        return errors.Count != 0
            ? throw new DomainValidationException("recharge.invalid", "Validation failed", errors)
            : recharge;
    }

    public static Dictionary<string, string[]> ValidateFieldsRequired(Guid walletId,
        decimal amount, CurrencyType currency, MethodType methodType, decimal exchangeRate)
    {
        var errors = new Dictionary<string, string[]>();

        // walletId requerido
        if (walletId == Guid.Empty)
            errors["walletId"] = ["El identificador de la billetera es requerido."];

        // amount requerido
        if (amount <= 0m)
            errors["amount"] = ["El monto debe ser mayor a cero."];
        
        if (exchangeRate <= 0m)
            errors["exchangeRate"] = ["El ratio de cambio debe ser mayor a cero."];

        // currency requerido y válido
        if (!Enum.IsDefined(typeof(CurrencyType), currency) || currency.Equals(default(CurrencyType)))
            errors["currency"] = ["El tipo de moneda es requerido y debe ser válido."];

        // methodType requerido y válido
        if (!Enum.IsDefined(typeof(MethodType), methodType) || methodType.Equals(default(MethodType)))
            errors["methodType"] = ["El método de recarga es requerido y debe ser válido."];

        // Si ya hay errores base, no continuar con reglas de negocio
        if (errors.Count > 0)
            return errors;

        // Regla: La recarga en dólares solo se puede realizar por AGENCIA (Banco)
        if (currency == CurrencyType.USD && methodType != MethodType.AGENCIA)
            errors["currency"] = ["La recarga en dólares solo se puede realizar mediante Banco (AGENCIA)."];

        // Reglas por método de recarga en soles
        if (currency == CurrencyType.PEN)
        {
            switch (methodType)
            {
                case MethodType.AGENCIA:
                    // Banco: debe ser de 5,000 soles para arriba
                    if (amount < 5000m)
                        errors["amount"] = ["La recarga por Banco en soles debe ser de S/ 5,000.00 para arriba."];
                    break;

                case MethodType.TIENDA:
                    // Tienda: entre 1 sol y 200 soles
                    if (amount < 1m || amount > 200m)
                        errors["amount"] = ["La recarga en Tienda debe ser entre S/ 1.00 y S/ 200.00."];
                    break;

                case MethodType.AGENTE:
                    // Agente: entre 500 soles y 5,000 soles
                    if (amount < 500m || amount > 5000m)
                        errors["amount"] = ["La recarga en Agente debe ser entre S/ 500.00 y S/ 5,000.00."];
                    break;
            }
        }

        // Reglas por método de recarga en dólares (solo AGENCIA permitido, ya validado arriba)
        if (currency == CurrencyType.USD && methodType == MethodType.AGENCIA)
        {
            // Banco en dólares: mayor a 100 dólares
            if (amount <= 100m)
                errors["amount"] = ["La recarga por Banco en dólares debe ser mayor a $ 100.00."];
        }

        return errors;
    }

    public decimal TotalCalculated(CurrencyType currency)
    {
        return Amount.Currency == currency ? Amount.Value : Amount.ApplyExchange();
    }
    
    public Operation ToOperation(CurrencyType currency)
    {
        return new Operation()
        {
            WalletId = WalletId,
            Amount = TotalCalculated(currency),
            Currency = currency,
            Type = (RechargeStatus == RechargeStatus.COMPLETED ? TypeOperation.Addition : TypeOperation.Subtract)
        };
    }

    // ── Saga state transitions ────────────────────────────────────────────────
    /// <summary>Actualiza el estado a COMPLETED. Idempotente.</summary>
    public void Complete()
    {
        if (RechargeStatus == RechargeStatus.COMPLETED) return;

        if (RechargeStatus != RechargeStatus.PENDING)
            throw new InvalidDomainStateException(
                "recharge.invalid_state",
                $"No se puede completar una recarga en estado {RechargeStatus}.");

        RechargeStatus = RechargeStatus.COMPLETED;
    }

    /// <summary>Actualiza el estado a FAILED y guarda el motivo. Idempotente.</summary>
    public void Fail(string reason)
    {
        if (RechargeStatus == RechargeStatus.FAILED) return;

        if (RechargeStatus != RechargeStatus.PENDING)
            throw new InvalidDomainStateException(
                "recharge.invalid_state",
                $"No se puede fallar una recarga en estado {RechargeStatus}.");

        RechargeStatus = RechargeStatus.FAILED;
        FailureReason  = reason;
    }

    public void SoftDelete()
    {
        if (RechargeStatus != RechargeStatus.COMPLETED)
            throw new InvalidDomainStateException(
                "recharge.invalid_state",
                $"Solo se puede cancelar una recarga en estado COMPLETED. Estado actual: {RechargeStatus}.");

        SetDeleted();
        RechargeStatus = RechargeStatus.CANCELLED;
    }
}