using WalletService.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using WalletService.Infrastructure.Persistence.Entities;

namespace WalletService.Infrastructure.Persistence.Contexts
{
    public class ApplicationDbContext : DbContext
    {
        private readonly IPublisher _publisher;

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            IPublisher publisher) : base(options)
        {
            _publisher = publisher;
        }

        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<WalletLimit> WalletsLimit { get; set; }

        /// <summary>
        /// Tabla de idempotencia para la Saga Coreografiada.
        /// Almacena los TransactionId ya procesados para evitar procesar dos veces el mismo mensaje.
        /// </summary>
        public DbSet<ProcessedTransaction> ProcessedTransactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var assembly = typeof(ApplicationDbContext).Assembly;
            modelBuilder.ApplyConfigurationsFromAssembly(assembly);
            modelBuilder.Ignore<DomainEvent>();
            base.OnModelCreating(modelBuilder);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.EnableSensitiveDataLogging();
            base.OnConfiguring(optionsBuilder);
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Properties<string>()
                .HaveMaxLength(255)
                .AreUnicode(false)
                .HaveConversion<string>();

            configurationBuilder.Properties<DateTime>()
                .HaveColumnType("datetime");

            base.ConfigureConventions(configurationBuilder);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var result = await base.SaveChangesAsync(cancellationToken);

            // Después de guardar los cambios en la base de datos, se publican los eventos de dominio
            await DistpatchDomainEventsAsync(cancellationToken);

            return result;
        }

        private async Task DistpatchDomainEventsAsync(CancellationToken cancellationToken)
        {
            // Wallet : AggregateRoot<WalletId> es el único agregado raíz en WalletService.
            // Usando el tipo correcto para que ChangeTracker pueda encontrar las entidades.
            var domainEntities = ChangeTracker
                .Entries<AggregateRoot<WalletId>>()
                .Where(e => e.Entity.DomainEvents != null && e.Entity.DomainEvents.Any())
                .ToList();

            var domainEvents = domainEntities
                .SelectMany(e => e.Entity.DomainEvents)
                .ToList();

            domainEntities.ForEach(e => e.Entity.ClearDomainEvents());

            foreach (var domainEvent in domainEvents)
            {
                await _publisher.Publish(domainEvent, cancellationToken);
            }
        }
    }
}
