using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using TransactionService.Application.Transactions.Commands.CompleteTransaction;
using TransactionService.Infrastructure.Messaging;

namespace TransactionService.Infrastructure.Messaging.Consumers;

/// <summary>
/// Consumer de Azure Service Bus que escucha el mensaje TransactionCompleted
/// publicado por WalletService y delega la actualización a la capa Application
/// mediante MediatR (CompleteTransactionCommand).
/// El tipo TransactionCompletedMessage lleva el [MessageUrn] que coincide con el
/// publicado por WalletService.
/// </summary>
public sealed class TransactionCompletedConsumer(
    IMediator mediator,
    ILogger<TransactionCompletedConsumer> logger)
    : IConsumer<TransactionCompletedMessage>
{
    public async Task Consume(ConsumeContext<TransactionCompletedMessage> context)
    {
        var message = context.Message;

        logger.LogInformation(
            "Evento TransactionCompleted recibido para TransactionId {TransactionId}",
            message.TransactionId);

        var command = new CompleteTransactionCommand(message.TransactionId);
        var result  = await mediator.Send(command, context.CancellationToken);

        result.Switch(
            _ => logger.LogInformation(
                "TransactionId {TransactionId} procesado exitosamente como COMPLETED.",
                message.TransactionId),
            errors => logger.LogError(
                "Error al procesar TransactionCompleted para {TransactionId}: {Errors}",
                message.TransactionId,
                string.Join(", ", errors.Select(e => e.Description)))
        );
    }
}

