using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using TransactionService.Application.Abstractions.Secrets;
using TransactionService.Application.Abstractions.Services;
using TransactionService.Application.Commmon.Interfaces;
using TransactionService.Domain.Interfaces;
using TransactionService.Infrastructure.Caching;
using TransactionService.Infrastructure.Configuration;
using TransactionService.Infrastructure.Persistence.Contexts;
using TransactionService.Infrastructure.Persistence.Repositories;
using TransactionService.Infrastructure.Services;

namespace TransactionService.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration          configuration)
        {
            var secretProviderType = configuration.GetValue<string>("SecretProviderType")?.ToLower();
            if (string.IsNullOrEmpty(secretProviderType))
                throw new InvalidOperationException(
                    "SecretProviderType configuration is missing. Valid values are 'KeyVault'.");

            if (secretProviderType.Equals("keyvault", StringComparison.CurrentCultureIgnoreCase))
                services.AddKeyVaultConfiguration(configuration);
            else
                throw new InvalidOperationException(
                    "Invalid SecretProviderType configuration. Valid values are 'KeyVault'.");

            services.AddSingleton<InMemorySecretCache>();
            services.Configure<CosmosOptions>(configuration.GetSection(CosmosOptions.SectionName));
            services.AddPersistence(configuration);
            services.AddHttpClients(configuration);
            services.AddServiceBusConfiguration(configuration);

            return services;
        }

        private static IServiceCollection AddPersistence(
            this IServiceCollection services,
            IConfiguration          configuration)
        {
            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                var secrets          = sp.GetRequiredService<ISecretProvider>();
                var connectionString = secrets.GetSecretAsync("CosmosConnection").GetAwaiter().GetResult()
                                       ?? throw new InvalidOperationException(
                                           "Secret 'CosmosConnection' is not configured.");
                var databaseName     = configuration.GetValue<string>("CosmosDatabaseName")
                                       ?? throw new InvalidOperationException(
                                           "Configuration 'CosmosDatabaseName' is not configured.");

                options.UseCosmos(connectionString, databaseName);
            });

            services.AddScoped<ITransactionRepository, TransactionRepository>();
            services.AddScoped<IRechargeRepository,    RechargeRepository>();
            services.AddScoped<IUnitOfWork,            UnitOfWork>();

            return services;
        }

        private static IServiceCollection AddHttpClients(
            this IServiceCollection services,
            IConfiguration          configuration)
        {
            var options = configuration
                .GetSection(HttpClientOptions.SectionName)
                .Get<HttpClientOptions>() ?? new HttpClientOptions();

            // ── Política de reintentos: 3 intentos con backoff exponencial ────────
            // Reintenta en errores transitorios HTTP (5xx, timeout, network failures)
            var retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                    onRetry: (outcome, timespan, retryAttempt, context) =>
                    {
                        var loggerFactory = context.TryGetValue("ILoggerFactory", out var lf)
                            ? lf as ILoggerFactory
                            : null;
                        var logger = loggerFactory?.CreateLogger("HttpClientRetry");
                        logger?.LogWarning(
                            "Reintento {RetryAttempt} para {Url} en {Delay}s. Error: {Error}",
                            retryAttempt,
                            outcome.Result?.RequestMessage?.RequestUri,
                            timespan.TotalSeconds,
                            outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                    });

            // ── Circuit breaker: corta si 5 llamadas consecutivas fallan ─────────
            // Se recupera automáticamente tras 30 segundos
            var circuitBreakerPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 5,
                    durationOfBreak: TimeSpan.FromSeconds(30));

            // ── WalletService HTTP client ─────────────────────────────────────────
            services
                .AddHttpClient<IWalletReadService, WalletReadService>(client =>
                {
                    if (!string.IsNullOrEmpty(options.WalletServiceBaseUrl))
                        client.BaseAddress = new Uri(options.WalletServiceBaseUrl);
                    client.Timeout = TimeSpan.FromSeconds(10);
                })
                .AddPolicyHandler(retryPolicy)
                .AddPolicyHandler(circuitBreakerPolicy);

            // ── MockCurrency / Exchange HTTP client ───────────────────────────────
            services
                .AddHttpClient<IExcnangeReadService, ExchangeReadService>(client =>
                {
                    if (!string.IsNullOrEmpty(options.ExchangeServiceBaseUrl))
                        client.BaseAddress = new Uri(options.ExchangeServiceBaseUrl);
                    client.Timeout = TimeSpan.FromSeconds(10);
                })
                .AddPolicyHandler(retryPolicy)
                .AddPolicyHandler(circuitBreakerPolicy);

            return services;
        }
    }
}