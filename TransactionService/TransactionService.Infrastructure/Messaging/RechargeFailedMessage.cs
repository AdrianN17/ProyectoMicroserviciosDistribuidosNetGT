using MassTransit;

namespace TransactionService.Infrastructure.Messaging;

/// <summary>
/// Contrato de Infrastructure para el evento RechargeFailed enviado por WalletService.
/// El [MessageUrn] debe coincidir exactamente con el que usa WalletService al publicar.
/// </summary>
[MessageUrn("WalletService.Application.Events.Integration:RechargeFailed")]
public sealed record RechargeFailedMessage(
    Guid   RechargeId,
    string Reason
);
