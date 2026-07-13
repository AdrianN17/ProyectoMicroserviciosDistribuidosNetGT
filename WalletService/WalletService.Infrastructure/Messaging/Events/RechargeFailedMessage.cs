using MassTransit;

namespace WalletService.Infrastructure.Messaging.Events;

/// <summary>
/// Contrato de Infrastructure para el evento RechargeFailed que publica WalletService.
/// El [MessageUrn] debe coincidir exactamente con el de TransactionService.Infrastructure (consumidor).
/// </summary>
[MessageUrn("WalletService.Application.Events.Integration:RechargeFailed")]
public sealed record RechargeFailedMessage(
    Guid   RechargeId,
    string Reason
);
