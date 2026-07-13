using MediatR;
using Microsoft.Extensions.Logging;
using TransactionService.Application.Abstractions.Messaging;
using TransactionService.Application.Transactions.IntegrationEvents;
using TransactionService.Domain.Events;

namespace TransactionService.Application.Transactions.Events;

/// <summary>
/// Maneja el evento de dominio TransactionCreatedDomainEvent y publica
/// el evento de integración TransactionCreatedIntegrationEvent en Azure Service Bus.
/// </summary>
public sealed class TransactionCreatedDomainEventHandler
    : INotificationHandler<TransactionCreatedDomainEvent>
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<TransactionCreatedDomainEventHandler> _logger;

    public TransactionCreatedDomainEventHandler(
        IEventBus eventBus,
        ILogger<TransactionCreatedDomainEventHandler> logger)
    {
        _eventBus = eventBus;
        _logger   = logger;
    }

    public async Task Handle(
        TransactionCreatedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Publicando TransactionCreatedIntegrationEvent para TransactionId {TransactionId}",
            notification.TransactionId);

        var integrationEvent = new TransactionCreatedIntegrationEvent(
            TransactionId: notification.TransactionId,
            FromWalletId:  notification.FromWalletId,
            ToWalletId:    notification.ToWalletId,
            Amount:        notification.Amount,
            Currency:      notification.Currency,
            SourceType:    notification.SourceType
        );

        await _eventBus.PublishAsync(integrationEvent, cancellationToken);

        _logger.LogInformation(
            "TransactionCreatedIntegrationEvent publicado exitosamente para TransactionId {TransactionId}",
            notification.TransactionId);
    }
}

