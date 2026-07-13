namespace WalletService.Infrastructure.Persistence.Entities;

/// <summary>
/// Entidad de infraestructura para garantizar idempotencia en el procesamiento de recargas.
/// Almacena los RechargeId ya procesados para evitar duplicados.
/// </summary>
public sealed class ProcessedRecharge
{
    /// <summary>Identificador único de la recarga (PK).</summary>
    public Guid RechargeId { get; private set; }

    /// <summary>Fecha y hora UTC en que fue procesada.</summary>
    public DateTime ProcessedAtUtc { get; private set; }

    private ProcessedRecharge() { }

    public static ProcessedRecharge Create(Guid rechargeId)
    {
        if (rechargeId == Guid.Empty)
            throw new ArgumentException("RechargeId no puede ser vacío.", nameof(rechargeId));

        return new ProcessedRecharge
        {
            RechargeId      = rechargeId,
            ProcessedAtUtc  = DateTime.UtcNow
        };
    }
}
