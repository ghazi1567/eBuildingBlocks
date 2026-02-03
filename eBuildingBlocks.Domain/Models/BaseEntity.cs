using System.ComponentModel.DataAnnotations;

namespace eBuildingBlocks.Domain.Models
{
    public interface IEntity
    {
    }
    /// <summary>
    /// Minimal base for all entities.
    /// Provides identity, equality, optional concurrency token, and domain events plumbing.
    /// </summary>
    public abstract class BaseEntity<TKey> : IEquatable<BaseEntity<TKey>>, IEntity
        where TKey : IEquatable<TKey>
    {
        /// <summary>Primary key.</summary>
        public TKey Id { get; set; } = default!;

        /// <summary>
        /// Optional optimistic concurrency token. EF Core will use this if configured.
        /// Remove if you don't want row-versioning.
        /// </summary>
        [Timestamp]
        public byte[]? RowVersion { get; protected set; }

        #region Domain Events (optional, but handy)
        private readonly List<IDomainEvent> _domainEvents = new();
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        /// <summary>Add a domain event to be dispatched after save.</summary>
        protected void AddDomainEvent(IDomainEvent @event)
        {
            if (@event is null) return;
            _domainEvents.Add(@event);
        }

        /// <summary>Remove a specific domain event.</summary>
        protected void RemoveDomainEvent(IDomainEvent @event)
        {
            _domainEvents.Remove(@event);
        }

        /// <summary>Clear all domain events (usually after dispatch).</summary>
        public void ClearDomainEvents() => _domainEvents.Clear();
        #endregion

        #region Equality
        public override bool Equals(object? obj) => Equals(obj as BaseEntity<TKey>);

        public bool Equals(BaseEntity<TKey>? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            // If either Id is default (not persisted), fall back to reference equality
            var thisHasId = !EqualityComparer<TKey>.Default.Equals(Id, default!);
            var otherHasId = !EqualityComparer<TKey>.Default.Equals(other.Id, default!);
            if (!(thisHasId && otherHasId)) return false;

            return EqualityComparer<TKey>.Default.Equals(Id, other.Id)
                   && GetType() == other.GetType();
        }

        public static bool operator ==(BaseEntity<TKey>? left, BaseEntity<TKey>? right)
            => Equals(left, right);

        public static bool operator !=(BaseEntity<TKey>? left, BaseEntity<TKey>? right)
            => !Equals(left, right);

        public override int GetHashCode()
        {
            // When Id is default, use base hash (unstable before persistence)
            var hasId = !EqualityComparer<TKey>.Default.Equals(Id, default!);
            return hasId ? HashCode.Combine(GetType(), Id) : base.GetHashCode();
        }
        #endregion
    }

    /// <summary>
    /// Marker interface for domain events with standard properties.
    /// All domain events must implement this interface.
    /// </summary>
    public interface IDomainEvent
    {
        /// <summary>
        /// Unique identifier for the event instance.
        /// </summary>
        Guid EventId { get; }
        
        /// <summary>
        /// Timestamp when the event occurred.
        /// </summary>
        DateTime OccurredAt { get; }
        
        /// <summary>
        /// Tenant identifier (for multi-tenant systems).
        /// </summary>
        Guid? TenantId { get; }
    }
}
