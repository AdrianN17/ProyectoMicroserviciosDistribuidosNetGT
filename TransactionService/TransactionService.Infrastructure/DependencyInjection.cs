using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TransactionService.Application.Abstractions.Secrets;
using TransactionService.Application.Commmon.Interfaces;
using TransactionService.Domain.Interfaces;
using TransactionService.Infrastructure.Caching;
using TransactionService.Infrastructure.Configuration;
using TransactionService.Infrastructure.Persistence.Contexts;
using TransactionService.Infrastructure.Persistence.Repositories;

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
            services.AddPersistence(configuration);
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
    }
}
