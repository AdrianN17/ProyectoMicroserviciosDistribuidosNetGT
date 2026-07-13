using MassTransit;

namespace WalletService.Infrastructure.Messaging.Events;

/// <summary>
/// Mensaje que WalletService publica cuando una transferencia se completa exitosamente.
/// El MessageUrn debe coincidir con el declarado en TransactionCompletedMessage
/// del TransactionService.Infrastructure.
/// </summary>
[MessageUrn("WalletService.Application.Events.Integration:TransactionCompleted")]
public sealed record TransactionCompletedMessage(
    Guid TransactionId,
    Guid FromWalletId,
    Guid ToWalletId,
    decimal Amount,
    string Currency
);

