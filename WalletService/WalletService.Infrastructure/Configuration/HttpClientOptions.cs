namespace WalletService.Infrastructure.Configuration;

public sealed class HttpClientOptions
{
    public const string SectionName = "HttpClients";

    /// <summary>URL base del MockCurrency service (ej. "https://mock-currency.example.com").</summary>
    public string ExchangeServiceBaseUrl { get; set; } = string.Empty;
}
