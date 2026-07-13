namespace TransactionService.Domain.Common;

/// <summary>
/// Interfaz no genérica para que el DbContext pueda identificar cualquier
/// AggregateRoot&lt;TId&gt; y despachar sus eventos de dominio, sin importar
/// el tipo del identificador.
/// </summary>
public interface IHasDomainEvents
{
    IReadOnlyCollection<DomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
