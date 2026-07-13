using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TransactionService.Application.Abstractions.Messaging;
using TransactionService.Application.Abstractions.Secrets;
using TransactionService.Infrastructure.Messaging;
using TransactionService.Infrastructure.Messaging.Consumers;

namespace TransactionService.Infrastructure.Configuration;

public static class ServiceBusManagerConfigurationExtension
{
    public static IServiceCollection AddServiceBusConfiguration(
        this IServiceCollection services,
        IConfiguration          configuration)
    {
        var serviceBusOptions = configuration
            .GetSection(ServiceBusOptions.SectionName)
            .Get<ServiceBusOptions>() ?? new ServiceBusOptions();

        services.AddSingleton(serviceBusOptions);

        services.AddMassTransit(busConfig =>
        {
            // ── Consumers (eventos entrantes de WalletService) ────────────────
            busConfig.AddConsumer<TransactionCompletedConsumer>();
            busConfig.AddConsumer<TransactionFailedConsumer>();

            busConfig.UsingAzureServiceBus((context, cfg) =>
            {
                var secretProvider = context.GetRequiredService<ISecretProvider>();
                var secretName     = configuration.GetValue<string>("ServiceBusConnectionString")
                                     ?? throw new InvalidOperationException(
                                         "Falta la configuración 'ServiceBusConnectionString'.");

                var connectionString = secretProvider.GetSecretAsync(secretName).GetAwaiter().GetResult()
                                       ?? throw new InvalidOperationException(
                                           $"El secreto '{secretName}' no fue encontrado en KeyVault.");

                cfg.Host(connectionString);

                // ── Topic: transaction-completed → TransactionCompletedConsumer ──
                cfg.SubscriptionEndpoint(
                    serviceBusOptions.SubscriptionName,
                    serviceBusOptions.TransactionCompletedTopic,
                    e => e.ConfigureConsumer<TransactionCompletedConsumer>(context));

                // ── Topic: transaction-failed → TransactionFailedConsumer ────────
                cfg.SubscriptionEndpoint(
                    serviceBusOptions.SubscriptionName,
                    serviceBusOptions.TransactionFailedTopic,
                    e => e.ConfigureConsumer<TransactionFailedConsumer>(context));

                cfg.ConfigureEndpoints(context);
            });
        });

        // IEventBus: publica TransactionCreated hacia Azure Service Bus (topic)
        services.AddScoped<IEventBus, EventBus>();

        // IProducer: mantiene la funcionalidad de envío a colas para otros flujos
        services.AddScoped<IProducer, Producer>();

        return services;
    }
}

