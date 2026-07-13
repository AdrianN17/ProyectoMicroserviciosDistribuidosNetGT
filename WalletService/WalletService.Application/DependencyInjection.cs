using WalletService.Application.Behaviors.Common;
using FluentValidation;
using MediatR;
using MediatR.NotificationPublishers;
using Microsoft.Extensions.DependencyInjection;
using WalletService.Domain.Services;

namespace WalletService.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            var assembly = typeof(DependencyInjection).Assembly;

            services.AddMediatR(cfg => {
                cfg.RegisterServicesFromAssembly(assembly);
                cfg.NotificationPublisher = new TaskWhenAllPublisher();
            });
            
            services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationErrorOrBehavior<,>));
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehavior<,>));
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

            // Domain Services
            services.AddScoped<ITransferDomainService, TransferDomainService>();

            return services;
        }

    }
}