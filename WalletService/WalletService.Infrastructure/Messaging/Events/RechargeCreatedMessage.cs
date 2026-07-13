using MassTransit;

namespace WalletService.Infrastructure.Messaging.Events;

/// <summary>
/// Contrato de Infrastructure para el evento RechargeCreated publicado por TransactionService.
/// El [MessageUrn] debe coincidir exactamente con el de TransactionService.Infrastructure.
/// </summary>
[MessageUrn("TransactionService.Application.Events.Integration:RechargeCreated")]
public sealed record RechargeCreatedMessage(
    Guid    RechargeId,
    Guid    WalletId,
    decimal Amount,
    string  Currency,
    string  MethodType,
    decimal ExchangeRate
);
