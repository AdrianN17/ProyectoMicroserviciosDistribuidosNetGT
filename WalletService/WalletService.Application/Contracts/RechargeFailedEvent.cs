namespace WalletService.Application.Contracts;

/// <summary>
/// Evento publicado por WalletService cuando no pudo procesar la recarga.
/// TransactionService lo consume para actualizar el estado de la recarga a FAILED.
/// </summary>
public sealed record RechargeFailedEvent(
    Guid    RechargeId,
    Guid    WalletId,
    decimal Amount,
    string  Currency,
    string  Reason
);
