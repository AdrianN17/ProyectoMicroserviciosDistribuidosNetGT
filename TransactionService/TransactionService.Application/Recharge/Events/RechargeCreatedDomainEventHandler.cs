using MediatR;
using Microsoft.Extensions.Logging;
using TransactionService.Application.Abstractions.Messaging;
using TransactionService.Application.Recharge.IntegrationEvents;
using TransactionService.Domain.Events;

namespace TransactionService.Application.Recharge.Events;

/// <summary>
/// Maneja el evento de dominio RechargeCreatedDomainEvent y publica
/// el evento de integración RechargeCreatedIntegrationEvent en Azure Service Bus.
/// </summary>
public sealed class RechargeCreatedDomainEventHandler
    : INotificationHandler<RechargeCreatedDomainEvent>
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<RechargeCreatedDomainEventHandler> _logger;

    public RechargeCreatedDomainEventHandler(
        IEventBus eventBus,
        ILogger<RechargeCreatedDomainEventHandler> logger)
    {
        _eventBus = eventBus;
        _logger   = logger;
    }

    public async Task Handle(
        RechargeCreatedDomainEvent notification,
        CancellationToken          cancellationToken)
    {
        _logger.LogInformation(
            "Publicando RechargeCreatedIntegrationEvent para RechargeId {RechargeId}",
            notification.RechargeId);

        var integrationEvent = new RechargeCreatedIntegrationEvent(
            RechargeId:   notification.RechargeId,
            WalletId:     notification.WalletId,
            Amount:       notification.Amount,
            Currency:     notification.Currency,
            MethodType:   notification.MethodType,
            ExchangeRate: notification.ExchangeRate
        );

        await _eventBus.PublishRechargeCreatedAsync(integrationEvent, cancellationToken);

        _logger.LogInformation(
            "RechargeCreatedIntegrationEvent publicado exitosamente para RechargeId {RechargeId}",
            notification.RechargeId);
    }
}
