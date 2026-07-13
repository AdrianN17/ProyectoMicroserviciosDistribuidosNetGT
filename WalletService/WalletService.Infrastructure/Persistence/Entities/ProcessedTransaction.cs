namespace WalletService.Infrastructure.Persistence.Entities;

/// <summary>
/// Entidad de infraestructura para garantizar idempotencia en el procesamiento de eventos.
/// Almacena los TransactionId ya procesados para evitar duplicados.
/// </summary>
public sealed class ProcessedTransaction
{
    /// <summary>Identificador único de la transacción (PK).</summary>
    public Guid TransactionId { get; private set; }

    /// <summary>Fecha y hora UTC en que fue procesada.</summary>
    public DateTime ProcessedAtUtc { get; private set; }

    private ProcessedTransaction() { }

    public static ProcessedTransaction Create(Guid transactionId)
    {
        if (transactionId == Guid.Empty)
            throw new ArgumentException("TransactionId no puede ser vacío.", nameof(transactionId));

        return new ProcessedTransaction
        {
            TransactionId = transactionId,
            ProcessedAtUtc = DateTime.UtcNow
        };
    }
}

