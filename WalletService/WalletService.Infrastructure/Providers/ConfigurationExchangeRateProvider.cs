using WalletService.Application.Common.Interfaces;
using WalletService.Infrastructure.Configuration;

namespace WalletService.Infrastructure.Providers;

/// <summary>
/// Implementación de IExchangeRateProvider que lee el tipo de cambio desde la configuración.
/// El valor se puede actualizar vía Azure App Configuration sin reiniciar el servicio.
/// </summary>
public sealed class ConfigurationExchangeRateProvider : IExchangeRateProvider
{
    private readonly ExchangeRateOptions _options;

    public ConfigurationExchangeRateProvider(ExchangeRateOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public decimal GetUsdToPenRate()
    {
        if (_options.UsdToPen <= 0)
            throw new InvalidOperationException(
                $"El tipo de cambio USD/PEN configurado ({_options.UsdToPen}) no es válido. Debe ser mayor a 0.");

        return _options.UsdToPen;
    }
}

