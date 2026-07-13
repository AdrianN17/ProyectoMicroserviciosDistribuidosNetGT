namespace TransactionService.Application.Recharge.IntegrationEvents;

/// <summary>
/// Evento recibido desde WalletService cuando no pudo procesar la recarga.
/// TransactionService actualiza el estado de la recarga a FAILED.
/// </summary>
public sealed record RechargeFailedIntegrationEvent(
    Guid   RechargeId,
    string Reason
);
