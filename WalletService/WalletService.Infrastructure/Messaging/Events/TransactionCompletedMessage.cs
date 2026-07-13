namespace WalletService.Infrastructure.Messaging.Events;

/// <summary>
/// Mensaje que WalletService publica cuando una transferencia se completa exitosamente.
/// </summary>
public sealed record TransactionCompletedMessage(
    Guid TransactionId,
    Guid FromWalletId,
    Guid ToWalletId,
    decimal Amount,
    string Currency
);

