namespace WalletService.Application.Common.Interfaces;

/// <summary>
/// Repositorio para garantizar idempotencia en el procesamiento de transacciones.
/// Evita procesar dos veces el mismo TransactionId.
/// </summary>
public interface IProcessedTransactionRepository
{
    Task<bool> ExistsAsync(Guid transactionId, CancellationToken cancellationToken = default);
    Task AddAsync(Guid transactionId, CancellationToken cancellationToken = default);
}

