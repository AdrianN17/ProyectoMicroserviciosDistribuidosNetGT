using TransactionService.Application.Recharge.IntegrationEvents;

namespace TransactionService.Application.Abstractions.Messaging;

/// <summary>
/// Abstracción para publicar eventos de integración a Azure Service Bus.
/// La capa Application no conoce MassTransit.
/// </summary>
public interface IEventBus
{
    Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class;
    Task PublishRechargeCreatedAsync(RechargeCreatedIntegrationEvent @event, CancellationToken cancellationToken = default);
}

