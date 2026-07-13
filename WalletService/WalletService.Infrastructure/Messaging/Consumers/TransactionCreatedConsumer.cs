using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using WalletService.Application.Wallets.Commands.ProcessTransactionCreated;
using WalletService.Infrastructure.Messaging.Events;

namespace WalletService.Infrastructure.Messaging.Consumers;

/// <summary>
/// Consumer de Azure Service Bus que escucha el evento TransactionCreated.
/// Delega el procesamiento al Command Handler vía MediatR para mantener separación de capas.
/// </summary>
public sealed class TransactionCreatedConsumer : IConsumer<TransactionCreatedMessage>
{
    private readonly ISender _sender;
    private readonly ILogger<TransactionCreatedConsumer> _logger;

    public TransactionCreatedConsumer(ISender sender, ILogger<TransactionCreatedConsumer> logger)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Consume(ConsumeContext<TransactionCreatedMessage> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "Mensaje TransactionCreated recibido. TransactionId: {TransactionId}, " +
            "FromWalletId: {FromWalletId}, ToWalletId: {ToWalletId}, Amount: {Amount} {Currency}",
            message.TransactionId,
            message.FromWalletId,
            message.ToWalletId,
            message.Amount,
            message.Currency);

        var command = new ProcessTransactionCreatedCommand(
            TransactionId: message.TransactionId,
            FromWalletId: message.FromWalletId,
            ToWalletId: message.ToWalletId,
            Amount: message.Amount,
            Currency: message.Currency,
            SourceType: message.SourceType
        );

        await _sender.Send(command, context.CancellationToken);
    }
}

