namespace eBuildingBlocks.Domain.Models;

/// <summary>
/// Persisted integration outbox row: domain events serialized in the same transaction as aggregate changes.
/// A background processor can read pending rows and publish to the message bus, then set <see cref="ProcessedAtUtc"/>.
/// </summary>
public class OutboxMessage : IEntity
{
    public Guid Id { get; set; }

    /// <summary>Matches <see cref="IDomainEvent.EventId"/> for correlation and idempotency.</summary>
    public Guid DomainEventId { get; set; }

    /// <summary>Stable key registered with <c>IEventTypeRegistry</c> (e.g. <c>user.created.v1</c>), not an assembly name.</summary>
    public string EventName { get; set; } = string.Empty;

    /// <summary>JSON payload of the domain event (concrete type).</summary>
    public string PayloadJson { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Use <see cref="Guid.Empty"/> only when multi-tenancy is disabled for the host.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Number of failed publish attempts (successful publish clears via <see cref="ProcessedAtUtc"/>).</summary>
    public int AttemptCount { get; set; }

    /// <summary>Last exception message or stack excerpt when publishing failed.</summary>
    public string? LastError { get; set; }

    /// <summary>
    /// Lease: while set in the future, other processor instances should skip this row (soft lock).
    /// Must be &lt;= UtcNow or null to be eligible for processing.
    /// </summary>
    public DateTime? LockedUntil { get; set; }

    /// <summary>Set when the message has been successfully published externally.</summary>
    public DateTime? ProcessedAtUtc { get; set; }
}
