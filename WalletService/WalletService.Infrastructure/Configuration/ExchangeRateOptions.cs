namespace WalletService.Infrastructure.Configuration;

public sealed class ExchangeRateOptions
{
    public const string SectionName = "ExchangeRate";

    /// <summary>Tipo de cambio: cuántos soles equivale 1 dólar.</summary>
    public decimal UsdToPen { get; set; } = 3.75m;
}

