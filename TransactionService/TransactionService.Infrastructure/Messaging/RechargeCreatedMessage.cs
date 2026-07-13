using MassTransit;

namespace TransactionService.Infrastructure.Messaging;

/// <summary>
/// Contrato de Infrastructure para el evento RechargeCreated.
/// El [MessageUrn] garantiza que TransactionService (productor) y
/// WalletService (consumidor) coincidan en el tipo del mensaje en ASB.
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
