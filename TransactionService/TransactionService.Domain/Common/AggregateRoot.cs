namespace TransactionService.Domain.Common
{
    public abstract class AggregateRoot<TId> : AuditableEntity<TId>, IHasDomainEvents
    {
        private readonly List<DomainEvent> _domainEvents = new();

        protected AggregateRoot() { }

        protected AggregateRoot(TId id) : base(id) { }

        public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        protected void AddDomainEvent(DomainEvent @event) => _domainEvents.Add(@event);

        protected void RemoveDomainEvent(DomainEvent @event) => _domainEvents.Remove(@event);

        public void ClearDomainEvents() => _domainEvents.Clear();

    }
}
