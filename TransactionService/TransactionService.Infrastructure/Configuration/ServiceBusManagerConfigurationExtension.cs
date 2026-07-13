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
            // ── Consumers (mensajes entrantes de WalletService) ───────────────
            busConfig.AddConsumer<TransactionCompletedConsumer>();
            busConfig.AddConsumer<TransactionFailedConsumer>();
            busConfig.AddConsumer<RechargeCompletedConsumer>();
            busConfig.AddConsumer<RechargeFailedConsumer>();

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

                // ── Cola: transaction-completed → TransactionCompletedConsumer ──
                // Azure Service Bus Basic tier: solo colas, sin topics/subscriptions
                cfg.ReceiveEndpoint(serviceBusOptions.TransactionCompletedQueueName,
                    (IServiceBusReceiveEndpointConfigurator e) =>
                    {
                        e.ConfigureConsumeTopology = false;
                        e.AutoDeleteOnIdle = TimeSpan.MaxValue;
                        e.DiscardSkippedMessages();
                        // Reintento: 3 intentos con backoff incremental antes de dead-letter
                        e.UseMessageRetry(r => r.Intervals(
                            TimeSpan.FromSeconds(1),
                            TimeSpan.FromSeconds(5),
                            TimeSpan.FromSeconds(15)));
                        // Circuit breaker: corta el circuito si más del 15% de mensajes fallan
                        e.UseCircuitBreaker(cb =>
                        {
                            cb.TrackingPeriod  = TimeSpan.FromMinutes(1);
                            cb.TripThreshold   = 15;
                            cb.ActiveThreshold = 10;
                            cb.ResetInterval   = TimeSpan.FromMinutes(5);
                        });
                        e.ConfigureConsumer<TransactionCompletedConsumer>(context);
                    });

                // ── Cola: transaction-failed → TransactionFailedConsumer ─────────
                cfg.ReceiveEndpoint(serviceBusOptions.TransactionFailedQueueName,
                    (IServiceBusReceiveEndpointConfigurator e) =>
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
                        e.ConfigureConsumer<TransactionFailedConsumer>(context);
                    });

                // ── Cola: recharge-completed → RechargeCompletedConsumer ──────────
                cfg.ReceiveEndpoint(serviceBusOptions.RechargeCompletedQueueName,
                    (IServiceBusReceiveEndpointConfigurator e) =>
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
                        e.ConfigureConsumer<RechargeCompletedConsumer>(context);
                    });

                // ── Cola: recharge-failed → RechargeFailedConsumer ────────────────
                cfg.ReceiveEndpoint(serviceBusOptions.RechargeFailedQueueName,
                    (IServiceBusReceiveEndpointConfigurator e) =>
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
                        e.ConfigureConsumer<RechargeFailedConsumer>(context);
                    });
            });
        });

        // IEventBus: envía TransactionCreated a la cola de WalletService
        services.AddScoped<IEventBus, EventBus>();

        // IProducer: mantiene la funcionalidad de envío a colas para otros flujos
        services.AddScoped<IProducer, Producer>();

        return services;
    }
}

