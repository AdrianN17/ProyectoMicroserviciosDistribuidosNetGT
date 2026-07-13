using MassTransit;

namespace TransactionService.Infrastructure.Messaging;

/// <summary>
/// Contrato de Infrastructure para el evento RechargeCompleted enviado por WalletService.
/// El [MessageUrn] debe coincidir exactamente con el que usa WalletService al publicar.
/// </summary>
[MessageUrn("WalletService.Application.Events.Integration:RechargeCompleted")]
public sealed record RechargeCompletedMessage(
    Guid RechargeId
);
