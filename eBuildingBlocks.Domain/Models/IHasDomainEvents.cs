namespace eBuildingBlocks.Domain.Models;

/// <summary>
/// Optional contract for aggregates that raise domain events.
/// Implementations align with <see cref="BaseEntity{TKey}"/> event plumbing so infrastructure
/// can collect events without reflection when this interface is implemented.
/// </summary>
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
