using TransactionService.Domain.Common;

namespace TransactionService.Domain.Events;

public sealed class TransactionCreatedDomainEvent : DomainEvent
{
    public Guid   TransactionId { get; }
    public Guid   FromWalletId  { get; }
    public Guid   ToWalletId    { get; }
    public decimal Amount       { get; }
    public string  Currency     { get; }
    public string  SourceType   { get; }

    public TransactionCreatedDomainEvent(
        Guid   transactionId,
        Guid   fromWalletId,
        Guid   toWalletId,
        decimal amount,
        string  currency,
        string  sourceType)
    {
        TransactionId = transactionId;
        FromWalletId  = fromWalletId;
        ToWalletId    = toWalletId;
        Amount        = amount;
        Currency      = currency;
        SourceType    = sourceType;
    }
}

