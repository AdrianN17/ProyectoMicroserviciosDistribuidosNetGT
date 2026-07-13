using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using WalletService.Application.Wallets.Commands.ProcessRechargeCreated;
using WalletService.Infrastructure.Messaging.Events;

namespace WalletService.Infrastructure.Messaging.Consumers;

/// <summary>
/// Consume el mensaje RechargeCreated desde Azure Service Bus y despacha
/// ProcessRechargeCreatedCommand vía MediatR.
/// </summary>
public sealed class RechargeCreatedConsumer(
    ISender sender,
    ILogger<RechargeCreatedConsumer> logger)
    : IConsumer<RechargeCreatedMessage>
{
    public async Task Consume(ConsumeContext<RechargeCreatedMessage> context)
    {
        var msg = context.Message;

        logger.LogInformation(
            "Recibido RechargeCreated. RechargeId: {RechargeId}, WalletId: {WalletId}",
            msg.RechargeId, msg.WalletId);

        await sender.Send(
            new ProcessRechargeCreatedCommand(
                RechargeId:   msg.RechargeId,
                WalletId:     msg.WalletId,
                Amount:       msg.Amount,
                Currency:     msg.Currency,
                MethodType:   msg.MethodType,
                ExchangeRate: msg.ExchangeRate),
            context.CancellationToken);

        logger.LogInformation(
            "RechargeCreated procesado exitosamente. RechargeId: {RechargeId}",
            msg.RechargeId);
    }
}
