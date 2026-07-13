namespace TransactionService.Application.Transactions.IntegrationEvents;

/// <summary>
/// Evento publicado por WalletService cuando completa exitosamente una transacción.
/// TransactionService actualiza el estado a COMPLETED.
/// </summary>
public sealed record TransactionCompletedIntegrationEvent(
    Guid TransactionId
);

