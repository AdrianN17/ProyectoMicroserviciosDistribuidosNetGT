using MassTransit;
using Microsoft.Extensions.Logging;
using TransactionService.Application.Abstractions.Messaging;

namespace TransactionService.Infrastructure.Messaging;

/// <summary>
/// Adaptador que implementa IEventBus usando MassTransit IPublishEndpoint.
/// Publica eventos en el topic de Azure Service Bus correspondiente al tipo del mensaje.
/// La capa Application no conoce MassTransit.
/// </summary>
public sealed class EventBus(
    IPublishEndpoint publishEndpoint,
    ILogger<EventBus> logger) : IEventBus
{
    public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class
    {
        logger.LogInformation(
            "Publicando evento {EventType} en Azure Service Bus",
            typeof(T).Name);

        await publishEndpoint.Publish(message, cancellationToken);

        logger.LogInformation(
            "Evento {EventType} publicado exitosamente.",
            typeof(T).Name);
    }
}

