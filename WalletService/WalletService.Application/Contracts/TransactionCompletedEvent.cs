namespace WalletService.Application.Contracts;

/// <summary>
/// Evento de integración publicado cuando una transferencia se completa exitosamente.
/// </summary>
public sealed record TransactionCompletedEvent(
    Guid TransactionId,
    Guid FromWalletId,
    Guid ToWalletId,
    decimal Amount,
    string Currency
);

