using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WalletService.Application.Abstractions.Secrets;
using WalletService.Infrastructure.Messaging;
using WalletService.Infrastructure.Messaging.Consumers;

namespace WalletService.Infrastructure.Configuration;

public static class ServiceBusManagerConfigurationExtension
{
    public static IServiceCollection AddServiceBusConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var serviceBusOptions = configuration.GetSection(ServiceBusOptions.SectionName).Get<ServiceBusOptions>() ?? new ServiceBusOptions();
        if (string.IsNullOrEmpty(serviceBusOptions.QueueName)) throw new InvalidOperationException("ServiceBus QueueName is not configured.");
        if (string.IsNullOrEmpty(serviceBusOptions.TransactionCreatedQueueName)) throw new InvalidOperationException("ServiceBus TransactionCreatedQueueName is not configured.");
        if (string.IsNullOrEmpty(serviceBusOptions.TransactionCompletedQueueName)) throw new InvalidOperationException("ServiceBus TransactionCompletedQueueName is not configured.");
        if (string.IsNullOrEmpty(serviceBusOptions.TransactionFailedQueueName)) throw new InvalidOperationException("ServiceBus TransactionFailedQueueName is not configured.");

        services.AddSingleton(serviceBusOptions);

        services.AddMassTransit(busConfig =>
        {
            // Consumer existente (SendOperation/UpdateBalance)
            busConfig.AddConsumer<UpdateBalanceConsumer>();

            // Consumer nuevo para la Saga TransactionCreated
            busConfig.AddConsumer<TransactionCreatedConsumer>();

            busConfig.UsingAzureServiceBus((context, cfg) =>
            {
                var secretProvider = context.GetRequiredService<ISecretProvider>();
                var secretName = configuration.GetValue<string>("ServiceBusConnectionString")
                                 ?? throw new InvalidOperationException("Falta la configuración 'ServiceBusConnectionString'.");

                var connectionString = secretProvider.GetSecretAsync(secretName).GetAwaiter().GetResult()
                                       ?? throw new InvalidOperationException($"El secreto '{secretName}' no fue encontrado en KeyVault.");

                cfg.Host(connectionString);

                // ── Cola existente: UpdateBalance / SendOperation ──────────────────
                cfg.ReceiveEndpoint(serviceBusOptions.QueueName, (IServiceBusReceiveEndpointConfigurator e) =>
                {
                    e.ConfigureConsumeTopology = false;
                    e.AutoDeleteOnIdle = TimeSpan.MaxValue;
                    e.DiscardSkippedMessages();
                    e.DiscardFaultedMessages();
                    e.ConfigureConsumer<UpdateBalanceConsumer>(context);
                });

                // ── Cola nueva: TransactionCreated (Saga Coreografiada) ────────────
                cfg.ReceiveEndpoint(serviceBusOptions.TransactionCreatedQueueName, (IServiceBusReceiveEndpointConfigurator e) =>
                {
                    // Azure Service Bus Basic tier: solo colas, sin topics/subscriptions
                    e.ConfigureConsumeTopology = false;
                    e.AutoDeleteOnIdle = TimeSpan.MaxValue;
                    e.DiscardSkippedMessages();
                    e.DiscardFaultedMessages();
                    e.ConfigureConsumer<TransactionCreatedConsumer>(context);
                });
            });
        });


        return services;
    }
}