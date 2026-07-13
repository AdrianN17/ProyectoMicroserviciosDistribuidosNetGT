namespace TransactionService.Application.Recharge.IntegrationEvents;

/// <summary>
/// Evento recibido desde WalletService cuando acreditó el saldo exitosamente.
/// TransactionService actualiza el estado de la recarga a COMPLETED.
/// </summary>
public sealed record RechargeCompletedIntegrationEvent(
    Guid RechargeId
);
