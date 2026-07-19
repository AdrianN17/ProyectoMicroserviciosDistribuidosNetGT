using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Options;
using TransactionService.Infrastructure.Configuration;

namespace TransactionService.Infrastructure.Persistence.Contexts
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            var connectionString = Environment.GetEnvironmentVariable("COSMOS_CONNECTION")
                                   ?? throw new InvalidOperationException("COSMOS_CONNECTION not configured");
            var databaseName     = Environment.GetEnvironmentVariable("COSMOS_DATABASE")
                                   ?? throw new InvalidOperationException("COSMOS_DATABASE not configured");

            optionsBuilder
                .UseCosmos(connectionString, databaseName)
                .EnableSensitiveDataLogging();

            var cosmosOptions = Options.Create(new CosmosOptions());

            return new ApplicationDbContext(optionsBuilder.Options, new NoOpPublisher(), cosmosOptions);
        }
    }

    public sealed class NoOpPublisher : IPublisher
    {
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification => Task.CompletedTask;

        public Task Publish(object notification, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
