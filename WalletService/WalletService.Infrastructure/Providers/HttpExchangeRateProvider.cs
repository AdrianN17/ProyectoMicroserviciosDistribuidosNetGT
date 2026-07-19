using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using WalletService.Application.Common.Interfaces;

namespace WalletService.Infrastructure.Providers;

/// <summary>
/// Implementación de IExchangeRateProvider que consulta el servicio MockCurrency via HTTP.
/// </summary>
public sealed class HttpExchangeRateProvider : IExchangeRateProvider
{
    private readonly HttpClient _httpClient;

    public HttpExchangeRateProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<decimal> GetUsdToPenRateAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetFromJsonAsync<ExchangeRateResponse>(
            "/convert/currency/USD", cancellationToken);

        if (response is null)
            throw new InvalidOperationException("MockCurrency devolvió una respuesta vacía.");

        if (!decimal.TryParse(response.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var rate) || rate <= 0)
            throw new InvalidOperationException($"MockCurrency devolvió un tipo de cambio inválido: '{response.Value}'.");

        return rate;
    }

    private sealed record ExchangeRateResponse(
        [property: JsonPropertyName("currency")] string Currency,
        [property: JsonPropertyName("value")] string Value);
}
