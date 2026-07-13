using MassTransit;
using Microsoft.Extensions.Logging;
using WalletService.Application.Common.Interfaces;
using WalletService.Application.Contracts;
using WalletService.Infrastructure.Configuration;
using WalletService.Infrastructure.Messaging.Events;

namespace WalletService.Infrastructure.Messaging.Publishers;

/// <summary>
/// Implementación de IEventPublisher que publica mensajes a Azure Service Bus
/// mediante MassTransit usando colas específicas (compatibilidad con tier Basic).
/// </summary>
public sealed class ServiceBusEventPublisher : IEventPublisher
{
    private readonly ISendEndpointProvider _sendEndpointProvider;
    private readonly ServiceBusOptions _options;
    private readonly ILogger<ServiceBusEventPublisher> _logger;

    public ServiceBusEventPublisher(
        ISendEndpointProvider sendEndpointProvider,
        ServiceBusOptions options,
        ILogger<ServiceBusEventPublisher> logger)
    {
        _sendEndpointProvider = sendEndpointProvider ?? throw new ArgumentNullException(nameof(sendEndpointProvider));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task PublishTransactionCompletedAsync(
        TransactionCompletedEvent @event,
        CancellationToken cancellationToken = default)
    {
        var message = new TransactionCompletedMessage(
            TransactionId: @event.TransactionId,
            FromWalletId: @event.FromWalletId,
            ToWalletId: @event.ToWalletId,
            Amount: @event.Amount,
            Currency: @event.Currency
        );

        var endpoint = await _sendEndpointProvider.GetSendEndpoint(
            new Uri($"queue:{_options.TransactionCompletedQueueName}"));

        await endpoint.Send(message, cancellationToken);

        _logger.LogInformation(
            "Publicado TransactionCompleted. TransactionId: {TransactionId}, Cola: {Queue}",
            @event.TransactionId, _options.TransactionCompletedQueueName);
    }

    public async Task PublishTransactionFailedAsync(
        TransactionFailedEvent @event,
        CancellationToken cancellationToken = default)
    {
        var message = new TransactionFailedMessage(
            TransactionId: @event.TransactionId,
            FromWalletId: @event.FromWalletId,
            ToWalletId: @event.ToWalletId,
            Amount: @event.Amount,
            Currency: @event.Currency,
            Reason: @event.Reason
        );

        var endpoint = await _sendEndpointProvider.GetSendEndpoint(
            new Uri($"queue:{_options.TransactionFailedQueueName}"));

        await endpoint.Send(message, cancellationToken);

        _logger.LogWarning(
            "Publicado TransactionFailed. TransactionId: {TransactionId}, Razón: {Reason}, Cola: {Queue}",
            @event.TransactionId, @event.Reason, _options.TransactionFailedQueueName);
    }
}

