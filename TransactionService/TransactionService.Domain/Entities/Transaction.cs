using TransactionService.Domain.Events;

namespace TransactionService.Domain.Entities;

public class Transaction : AggregateRoot<TransactionId>
{
    public WalletId          FromWalletId      { get; private set; }
    public WalletId          ToWalletId        { get; private set; }
    public Amount            Amount            { get; private set; }
    public TransactionStatus TransactionStatus { get; private set; }
    public SourceType        SourceType        { get; private set; }

    /// <summary>Motivo del fallo, poblado cuando Status = FAILED.</summary>
    public string? FailureReason { get; private set; }

    private Transaction() { }

    // ── Factory ───────────────────────────────────────────────────────────────
    /// <summary>
    /// Crea una transacción en estado PENDING y levanta el evento de dominio
    /// TransactionCreatedDomainEvent. No valida saldo ni límites.
    /// El <paramref name="transactionId"/> es provisto por el cliente para idempotencia.
    /// </summary>
    public static Transaction Create(
        Guid         transactionId,
        Guid         fromWalletId,
        Guid         toWalletId,
        decimal      amount,
        CurrencyType currency,
        SourceType   sourceType)
    {
        var errors = ValidateFieldsRequired(fromWalletId, toWalletId, amount, currency, sourceType);

        if (errors.Count != 0)
            throw new DomainValidationException("transaction.invalid", "Validation failed", errors);

        var transaction = new Transaction
        {
            Id                = new TransactionId(transactionId),
            FromWalletId      = new WalletId(fromWalletId),
            ToWalletId        = new WalletId(toWalletId),
            Amount            = Amount.Create(amount, currency, exchangeRate: 1m),
            TransactionStatus = TransactionStatus.PENDING,
            SourceType        = sourceType
        };

        transaction.AddDomainEvent(new TransactionCreatedDomainEvent(
            transactionId: transaction.Id.Value,
            fromWalletId:  fromWalletId,
            toWalletId:    toWalletId,
            amount:        amount,
            currency:      currency.ToString(),
            sourceType:    sourceType.ToString()
        ));

        return transaction;
    }

    public static Dictionary<string, string[]> ValidateFieldsRequired(
        Guid         fromWalletId,
        Guid         toWalletId,
        decimal      amount,
        CurrencyType currency,
        SourceType   sourceType)
    {
        var errors = new Dictionary<string, string[]>();

        if (fromWalletId == Guid.Empty)
            errors["fromWalletId"] = ["El identificador de la billetera origen es requerido."];

        if (toWalletId == Guid.Empty)
            errors["toWalletId"] = ["El identificador de la billetera destino es requerido."];

        if (fromWalletId == toWalletId)
            errors["walletId"] = ["El identificador de la billetera origen y destino no deben ser iguales."];

        if (amount <= 0m)
            errors["amount"] = ["El monto debe ser mayor a cero."];

        if (!Enum.IsDefined(typeof(CurrencyType), currency) || currency.Equals(default(CurrencyType)))
            errors["currency"] = ["El tipo de moneda es requerido y debe ser válido."];

        if (!Enum.IsDefined(typeof(SourceType), sourceType) || sourceType.Equals(default(SourceType)))
            errors["sourceType"] = ["El origen es requerido y debe ser válido."];

        return errors;
    }

    // ── Saga state transitions ────────────────────────────────────────────────
    /// <summary>Actualiza el estado a COMPLETED. Idempotente.</summary>
    public void Complete()
    {
        if (TransactionStatus == TransactionStatus.COMPLETED) return;

        if (TransactionStatus != TransactionStatus.PENDING)
            throw new InvalidDomainStateException(
                "transaction.invalid_state",
                $"No se puede completar una transacción en estado {TransactionStatus}.");

        TransactionStatus = TransactionStatus.COMPLETED;
    }

    /// <summary>Actualiza el estado a FAILED y guarda el motivo. Idempotente.</summary>
    public void Fail(string reason)
    {
        if (TransactionStatus == TransactionStatus.FAILED) return;

        if (TransactionStatus != TransactionStatus.PENDING)
            throw new InvalidDomainStateException(
                "transaction.invalid_state",
                $"No se puede fallar una transacción en estado {TransactionStatus}.");

        TransactionStatus = TransactionStatus.FAILED;
        FailureReason     = reason;
    }

    public void SoftDelete()
    {
        SetDeleted();
        TransactionStatus = TransactionStatus.CANCELLED;
    }
}