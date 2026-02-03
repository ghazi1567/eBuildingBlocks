namespace eBuildingBlocks.Domain.Models
{
    /// <summary>
    /// Base class for domain events with common properties.
    /// Reduces boilerplate in event definitions.
    /// </summary>
    public abstract class BaseDomainEvent : IDomainEvent
    {
        /// <summary>
        /// Unique identifier for the event instance.
        /// </summary>
        public Guid EventId { get; } = Guid.NewGuid();
        
        /// <summary>
        /// Timestamp when the event occurred.
        /// </summary>
        public DateTime OccurredAt { get; } = DateTime.UtcNow;
        
        /// <summary>
        /// Tenant identifier (for multi-tenant systems).
        /// </summary>
        public Guid? TenantId { get; set; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseDomainEvent"/> class.
        /// </summary>
        protected BaseDomainEvent()
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseDomainEvent"/> class with tenant identifier.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        protected BaseDomainEvent(Guid? tenantId)
        {
            TenantId = tenantId;
        }
    }
}
