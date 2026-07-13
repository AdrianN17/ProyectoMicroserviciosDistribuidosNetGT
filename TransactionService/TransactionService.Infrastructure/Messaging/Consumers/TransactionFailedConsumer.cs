using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using TransactionService.Application.Transactions.Commands.FailTransaction;
using TransactionService.Application.Transactions.IntegrationEvents;

namespace TransactionService.Infrastructure.Messaging.Consumers;

/// <summary>
/// Consumer de Azure Service Bus que escucha el evento TransactionFailed
/// publicado por WalletService y delega la actualización a la capa Application
/// mediante MediatR (FailTransactionCommand).
/// </summary>
public sealed class TransactionFailedConsumer(
    IMediator mediator,
    ILogger<TransactionFailedConsumer> logger)
    : IConsumer<TransactionFailedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<TransactionFailedIntegrationEvent> context)
    {
        var message = context.Message;

        logger.LogInformation(
            "Evento TransactionFailed recibido para TransactionId {TransactionId} - Razón: {Reason}",
            message.TransactionId, message.Reason);

        var command = new FailTransactionCommand(message.TransactionId, message.Reason);
        var result  = await mediator.Send(command, context.CancellationToken);

        result.Switch(
            _ => logger.LogInformation(
                "TransactionId {TransactionId} procesado exitosamente como FAILED.",
                message.TransactionId),
            errors => logger.LogError(
                "Error al procesar TransactionFailed para {TransactionId}: {Errors}",
                message.TransactionId,
                string.Join(", ", errors.Select(e => e.Description)))
        );
    }
}

