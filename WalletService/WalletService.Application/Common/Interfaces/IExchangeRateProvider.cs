namespace WalletService.Application.Common.Interfaces;

/// <summary>
/// Proveedor del tipo de cambio USD/PEN.
/// La implementación lee el valor desde la configuración.
/// </summary>
public interface IExchangeRateProvider
{
    decimal GetUsdToPenRate();
}

