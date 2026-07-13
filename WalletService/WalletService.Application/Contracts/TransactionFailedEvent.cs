namespace WalletService.Application.Contracts;

/// <summary>
/// Evento de integración publicado cuando una transferencia falla.
/// </summary>
public sealed record TransactionFailedEvent(
    Guid TransactionId,
    Guid FromWalletId,
    Guid ToWalletId,
    decimal Amount,
    string Currency,
    string Reason
);

