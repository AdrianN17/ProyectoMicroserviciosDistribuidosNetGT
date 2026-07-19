namespace WalletService.Application.Common.Interfaces;

/// <summary>
/// Proveedor del tipo de cambio USD/PEN.
/// La implementación consulta el servicio MockCurrency via HTTP.
/// </summary>
public interface IExchangeRateProvider
{
    Task<decimal> GetUsdToPenRateAsync(CancellationToken cancellationToken = default);
}

