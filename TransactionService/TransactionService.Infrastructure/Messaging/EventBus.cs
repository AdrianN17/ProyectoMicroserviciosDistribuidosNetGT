using MassTransit;
using Microsoft.Extensions.Logging;
using TransactionService.Application.Abstractions.Messaging;
using TransactionService.Application.Recharge.IntegrationEvents;
using TransactionService.Application.Transactions.IntegrationEvents;
using TransactionService.Infrastructure.Configuration;

namespace TransactionService.Infrastructure.Messaging;

/// <summary>
/// Adaptador que implementa IEventBus usando MassTransit ISendEndpointProvider.
/// Convierte los eventos de integración de la capa Application en mensajes con
/// [MessageUrn] de Infrastructure y los envía directamente a la cola de destino
/// (compatible con Azure Service Bus Basic tier).
/// La capa Application no conoce MassTransit.
/// </summary>
public sealed class EventBus(
    ISendEndpointProvider sendEndpointProvider,
    ServiceBusOptions serviceBusOptions,
    ILogger<EventBus> logger) : IEventBus
{
    public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class
    {
        logger.LogInformation(
            "Publicando evento {EventType} en Azure Service Bus (cola: {Queue})",
            typeof(T).Name, serviceBusOptions.TransactionCreatedQueueName);

        if (message is not TransactionCreatedIntegrationEvent integrationEvent)
            throw new NotSupportedException(
                $"EventBus.PublishAsync solo soporta TransactionCreatedIntegrationEvent. Tipo recibido: {typeof(T).Name}");

        var contract = new TransactionCreatedMessage(
            TransactionId: integrationEvent.TransactionId,
            FromWalletId:  integrationEvent.FromWalletId,
            ToWalletId:    integrationEvent.ToWalletId,
            Amount:        integrationEvent.Amount,
            Currency:      integrationEvent.Currency,
            SourceType:    integrationEvent.SourceType);

        var endpoint = await sendEndpointProvider.GetSendEndpoint(
            new Uri($"queue:{serviceBusOptions.TransactionCreatedQueueName}"));

        await endpoint.Send(contract, cancellationToken);

        logger.LogInformation(
            "Evento {EventType} publicado exitosamente en cola {Queue}.",
            typeof(T).Name, serviceBusOptions.TransactionCreatedQueueName);
    }

    public async Task PublishRechargeCreatedAsync(
        RechargeCreatedIntegrationEvent @event,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Publicando RechargeCreated para RechargeId {RechargeId} en cola {Queue}",
            @event.RechargeId, serviceBusOptions.RechargeCreatedQueueName);

        var contract = new RechargeCreatedMessage(
            RechargeId:   @event.RechargeId,
            WalletId:     @event.WalletId,
            Amount:       @event.Amount,
            Currency:     @event.Currency,
            MethodType:   @event.MethodType,
            ExchangeRate: @event.ExchangeRate);

        var endpoint = await sendEndpointProvider.GetSendEndpoint(
            new Uri($"queue:{serviceBusOptions.RechargeCreatedQueueName}"));

        await endpoint.Send(contract, cancellationToken);

        logger.LogInformation(
            "RechargeCreated publicado exitosamente en cola {Queue}.",
            serviceBusOptions.RechargeCreatedQueueName);
    }
}

