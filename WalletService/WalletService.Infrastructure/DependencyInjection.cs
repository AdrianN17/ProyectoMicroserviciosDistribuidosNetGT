using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WalletService.Application.Abstractions.Secrets;
using WalletService.Application.Commmon.Interfaces;
using WalletService.Application.Common.Interfaces;
using WalletService.Domain.Interfaces;
using WalletService.Infrastructure.Caching;
using WalletService.Infrastructure.Configuration;
using WalletService.Infrastructure.Messaging.Publishers;
using WalletService.Infrastructure.Persistence.Contexts;
using WalletService.Infrastructure.Persistence.Repositories;
using WalletService.Infrastructure.Providers;

namespace WalletService.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var secretProviderType = configuration.GetValue<string>("SecretProviderType")?.ToLower();
            if (string.IsNullOrEmpty(secretProviderType)) throw new InvalidOperationException("SecretProviderType configuration is missing. Valid values are 'SecretsManager' or 'Vault'.");

            if (secretProviderType.Equals("keyvault", StringComparison.CurrentCultureIgnoreCase))
            {
                services.AddKeyVaultConfiguration(configuration);
            }
            else
            {
                throw new InvalidOperationException("Invalid SecretProviderType configuration. Valid values are 'SecretsManager' or 'Vault'.");
            }

            services.AddSingleton<InMemorySecretCache>();
            services.AddPersistence(configuration);
            services.AddExchangeRate(configuration);
            services.AddServiceBusConfiguration(configuration);

            // IEventPublisher: publica TransactionCompleted / TransactionFailed al Service Bus
            services.AddScoped<IEventPublisher, ServiceBusEventPublisher>();

            return services;
        }

        private static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                var secrets = sp.GetRequiredService<ISecretProvider>();
                var connectionString = secrets.GetSecretAsync("WalletSqlServerConnection").GetAwaiter().GetResult();
                if (connectionString is null) {
                    throw new InvalidOperationException("Connection string for CustomerSqlServerConnection is not configured in Vault");
                }

                options.UseSqlServer(connectionString);
            });

            services.AddScoped<IWalletRepository, WalletRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IProcessedTransactionRepository, ProcessedTransactionRepository>();
            services.AddScoped<IProcessedRechargeRepository, ProcessedRechargeRepository>();

            return services;
        }

        private static IServiceCollection AddExchangeRate(this IServiceCollection services, IConfiguration configuration)
        {
            var options = configuration.GetSection(ExchangeRateOptions.SectionName).Get<ExchangeRateOptions>()
                          ?? new ExchangeRateOptions();
            services.AddSingleton(options);
            services.AddSingleton<IExchangeRateProvider, ConfigurationExchangeRateProvider>();
            return services;
        }
    }
}
