using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using TransactionService.Application.Recharge.Commands.FailRecharge;

namespace TransactionService.Infrastructure.Messaging.Consumers;

/// <summary>
/// Consume el mensaje RechargeFailed desde Azure Service Bus y despacha
/// FailRechargeCommand vía MediatR.
/// </summary>
public sealed class RechargeFailedConsumer(
    ISender sender,
    ILogger<RechargeFailedConsumer> logger)
    : IConsumer<RechargeFailedMessage>
{
    public async Task Consume(ConsumeContext<RechargeFailedMessage> context)
    {
        var msg = context.Message;

        logger.LogInformation(
            "Recibido RechargeFailed para RechargeId {RechargeId} - Razón: {Reason}",
            msg.RechargeId, msg.Reason);

        var result = await sender.Send(
            new FailRechargeCommand(msg.RechargeId, msg.Reason),
            context.CancellationToken);

        if (result.IsError)
        {
            logger.LogError(
                "Error procesando RechargeFailed para RechargeId {RechargeId}: {Error}",
                msg.RechargeId, result.FirstError.Description);
            throw new InvalidOperationException(result.FirstError.Description);
        }

        logger.LogInformation(
            "RechargeFailed procesado exitosamente para RechargeId {RechargeId}",
            msg.RechargeId);
    }
}
