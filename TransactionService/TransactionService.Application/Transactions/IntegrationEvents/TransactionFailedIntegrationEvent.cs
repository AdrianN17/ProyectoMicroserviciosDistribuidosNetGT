namespace TransactionService.Application.Transactions.IntegrationEvents;

/// <summary>
/// Evento publicado por WalletService cuando no puede procesar una transacción.
/// TransactionService actualiza el estado a FAILED y persiste el motivo.
/// </summary>
public sealed record TransactionFailedIntegrationEvent(
    Guid   TransactionId,
    string Reason
);

