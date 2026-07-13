using TransactionService.Domain.Common;

namespace TransactionService.Domain.Events;

public sealed class RechargeCreatedDomainEvent : DomainEvent
{
    public Guid    RechargeId   { get; }
    public Guid    WalletId     { get; }
    public decimal Amount       { get; }
    public string  Currency     { get; }
    public string  MethodType   { get; }
    public decimal ExchangeRate { get; }

    public RechargeCreatedDomainEvent(
        Guid    rechargeId,
        Guid    walletId,
        decimal amount,
        string  currency,
        string  methodType,
        decimal exchangeRate)
    {
        RechargeId   = rechargeId;
        WalletId     = walletId;
        Amount       = amount;
        Currency     = currency;
        MethodType   = methodType;
        ExchangeRate = exchangeRate;
    }
}
