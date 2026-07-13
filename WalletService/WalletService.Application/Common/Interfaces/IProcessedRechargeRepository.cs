namespace WalletService.Application.Common.Interfaces;

/// <summary>
/// Repositorio de idempotencia para recargas.
/// Permite verificar y registrar los RechargeId ya procesados.
/// </summary>
public interface IProcessedRechargeRepository
{
    Task<bool> ExistsAsync(Guid rechargeId, CancellationToken cancellationToken = default);
    Task AddAsync(Guid rechargeId, CancellationToken cancellationToken = default);
}
