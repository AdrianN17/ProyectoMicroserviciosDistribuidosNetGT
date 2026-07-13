namespace TransactionService.Application.Transactions.IntegrationEvents;

/// <summary>
/// Evento publicado por TransactionService cuando se crea una transacción PENDING.
/// WalletService es el consumidor responsable de validar y mover saldos.
/// </summary>
public sealed record TransactionCreatedIntegrationEvent(
    Guid    TransactionId,
    Guid    FromWalletId,
    Guid    ToWalletId,
    decimal Amount,
    string  Currency,
    string  SourceType
);

