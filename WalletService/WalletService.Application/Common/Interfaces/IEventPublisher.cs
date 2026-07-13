using WalletService.Application.Contracts;

namespace WalletService.Application.Common.Interfaces;

/// <summary>
/// Abstracción para publicar eventos de integración hacia el bus de mensajes.
/// La implementación vive en Infrastructure (Azure Service Bus).
/// </summary>
public interface IEventPublisher
{
    Task PublishTransactionCompletedAsync(TransactionCompletedEvent @event, CancellationToken cancellationToken = default);
    Task PublishTransactionFailedAsync(TransactionFailedEvent @event, CancellationToken cancellationToken = default);
    Task PublishRechargeCompletedAsync(RechargeCompletedEvent @event, CancellationToken cancellationToken = default);
    Task PublishRechargeFailedAsync(RechargeFailedEvent @event, CancellationToken cancellationToken = default);
}

