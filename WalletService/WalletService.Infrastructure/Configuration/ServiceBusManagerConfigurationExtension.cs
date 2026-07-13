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

            // Consumers para la Saga TransactionCreated
            busConfig.AddConsumer<TransactionCreatedConsumer>();

            // Consumer para la Saga RechargeCreated
            busConfig.AddConsumer<RechargeCreatedConsumer>();

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
                    e.UseMessageRetry(r => r.Intervals(
                        TimeSpan.FromSeconds(1),
                        TimeSpan.FromSeconds(5),
                        TimeSpan.FromSeconds(15)));
                    e.UseCircuitBreaker(cb =>
                    {
                        cb.TrackingPeriod  = TimeSpan.FromMinutes(1);
                        cb.TripThreshold   = 15;
                        cb.ActiveThreshold = 10;
                        cb.ResetInterval   = TimeSpan.FromMinutes(5);
                    });
                    e.ConfigureConsumer<UpdateBalanceConsumer>(context);
                });

                // ── Cola: TransactionCreated (Saga Coreografiada) ─────────────────
                cfg.ReceiveEndpoint(serviceBusOptions.TransactionCreatedQueueName, (IServiceBusReceiveEndpointConfigurator e) =>
                {
                    e.ConfigureConsumeTopology = false;
                    e.AutoDeleteOnIdle = TimeSpan.MaxValue;
                    e.DiscardSkippedMessages();
                    e.UseMessageRetry(r => r.Intervals(
                        TimeSpan.FromSeconds(1),
                        TimeSpan.FromSeconds(5),
                        TimeSpan.FromSeconds(15)));
                    e.UseCircuitBreaker(cb =>
                    {
                        cb.TrackingPeriod  = TimeSpan.FromMinutes(1);
                        cb.TripThreshold   = 15;
                        cb.ActiveThreshold = 10;
                        cb.ResetInterval   = TimeSpan.FromMinutes(5);
                    });
                    e.ConfigureConsumer<TransactionCreatedConsumer>(context);
                });

                // ── Cola: RechargeCreated (Saga Coreografiada Recharge) ────────────
                cfg.ReceiveEndpoint(serviceBusOptions.RechargeCreatedQueueName, (IServiceBusReceiveEndpointConfigurator e) =>
                {
                    e.ConfigureConsumeTopology = false;
                    e.AutoDeleteOnIdle = TimeSpan.MaxValue;
                    e.DiscardSkippedMessages();
                    e.UseMessageRetry(r => r.Intervals(
                        TimeSpan.FromSeconds(1),
                        TimeSpan.FromSeconds(5),
                        TimeSpan.FromSeconds(15)));
                    e.UseCircuitBreaker(cb =>
                    {
                        cb.TrackingPeriod  = TimeSpan.FromMinutes(1);
                        cb.TripThreshold   = 15;
                        cb.ActiveThreshold = 10;
                        cb.ResetInterval   = TimeSpan.FromMinutes(5);
                    });
                    e.ConfigureConsumer<RechargeCreatedConsumer>(context);
                });
            });
        });


        return services;
    }
}