namespace WalletService.Application.Contracts;

/// <summary>
/// Evento publicado por WalletService cuando acreditó exitosamente el saldo de una recarga.
/// TransactionService lo consume para actualizar el estado de la recarga a COMPLETED.
/// </summary>
public sealed record RechargeCompletedEvent(
    Guid    RechargeId,
    Guid    WalletId,
    decimal Amount,
    string  Currency
);
