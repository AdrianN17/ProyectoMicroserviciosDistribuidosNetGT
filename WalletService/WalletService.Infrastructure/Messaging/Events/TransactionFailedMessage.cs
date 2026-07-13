namespace WalletService.Infrastructure.Messaging.Events;

/// <summary>
/// Mensaje que WalletService publica cuando una transferencia falla.
/// </summary>
public sealed record TransactionFailedMessage(
    Guid TransactionId,
    Guid FromWalletId,
    Guid ToWalletId,
    decimal Amount,
    string Currency,
    string Reason
);

