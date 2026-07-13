﻿using MediatR;
using Microsoft.EntityFrameworkCore;
using TransactionService.Application.Commmon.Interfaces;
using TransactionService.Domain.Common;

namespace TransactionService.Infrastructure.Persistence.Contexts
{
    public class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        private readonly IPublisher _publisher;

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            IPublisher publisher) : base(options)
        {
            _publisher = publisher;
        }

        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Recharge> Recharges { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Ignorar el tipo base abstracto ANTES de aplicar configuraciones
            // para que EF Core no intente mapear DomainEvents como owned collection
            modelBuilder.Ignore<DomainEvent>();

            var assembly = typeof(ApplicationDbContext).Assembly;
            modelBuilder.ApplyConfigurationsFromAssembly(assembly);

            base.OnModelCreating(modelBuilder);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.EnableSensitiveDataLogging();
            base.OnConfiguring(optionsBuilder);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var result = await base.SaveChangesAsync(cancellationToken);
            await DispatchDomainEventsAsync(cancellationToken);
            return result;
        }


        private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
        {
            // Usamos IHasDomainEvents (interfaz no genérica) para encontrar cualquier
            // AggregateRoot<TId>, independientemente del tipo del identificador.
            var aggregates = ChangeTracker
                .Entries()
                .Select(e => e.Entity)
                .OfType<IHasDomainEvents>()
                .Where(e => e.DomainEvents.Any())
                .ToList();

            var domainEvents = aggregates
                .SelectMany(e => e.DomainEvents)
                .ToList();

            aggregates.ForEach(e => e.ClearDomainEvents());

            foreach (var domainEvent in domainEvents)
                await _publisher.Publish(domainEvent, cancellationToken);
        }
    }
}
