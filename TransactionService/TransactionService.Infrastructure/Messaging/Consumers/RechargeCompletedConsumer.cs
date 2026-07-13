using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using TransactionService.Application.Recharge.Commands.CompleteRecharge;

namespace TransactionService.Infrastructure.Messaging.Consumers;

/// <summary>
/// Consume el mensaje RechargeCompleted desde Azure Service Bus y despacha
/// CompleteRechargeCommand vía MediatR.
/// </summary>
public sealed class RechargeCompletedConsumer(
    ISender sender,
    ILogger<RechargeCompletedConsumer> logger)
    : IConsumer<RechargeCompletedMessage>
{
    public async Task Consume(ConsumeContext<RechargeCompletedMessage> context)
    {
        var msg = context.Message;

        logger.LogInformation(
            "Recibido RechargeCompleted para RechargeId {RechargeId}",
            msg.RechargeId);

        var result = await sender.Send(
            new CompleteRechargeCommand(msg.RechargeId),
            context.CancellationToken);

        if (result.IsError)
        {
            logger.LogError(
                "Error procesando RechargeCompleted para RechargeId {RechargeId}: {Error}",
                msg.RechargeId, result.FirstError.Description);
            throw new InvalidOperationException(result.FirstError.Description);
        }

        logger.LogInformation(
            "RechargeCompleted procesado exitosamente para RechargeId {RechargeId}",
            msg.RechargeId);
    }
}
