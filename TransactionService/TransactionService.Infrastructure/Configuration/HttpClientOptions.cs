namespace TransactionService.Infrastructure.Configuration;

/// <summary>
/// Opciones de configuración para los clientes HTTP hacia servicios externos.
/// Se leen desde Azure App Configuration bajo la sección "HttpClients".
/// </summary>
public sealed class HttpClientOptions
{
    public const string SectionName = "HttpClients";

    /// <summary>URL base del WalletService (ej. "https://wallet-service.example.com").</summary>
    public string WalletServiceBaseUrl { get; set; } = string.Empty;

    /// <summary>URL base del MockCurrency service (ej. "https://mock-currency.example.com").</summary>
    public string ExchangeServiceBaseUrl { get; set; } = string.Empty;
}
