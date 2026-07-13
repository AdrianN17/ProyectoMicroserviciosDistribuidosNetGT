using MassTransit;

namespace WalletService.Infrastructure.Messaging.Events;

/// <summary>
/// Contrato de Infrastructure para el evento RechargeCompleted que publica WalletService.
/// El [MessageUrn] debe coincidir exactamente con el de TransactionService.Infrastructure (consumidor).
/// </summary>
[MessageUrn("WalletService.Application.Events.Integration:RechargeCompleted")]
public sealed record RechargeCompletedMessage(
    Guid RechargeId
);
