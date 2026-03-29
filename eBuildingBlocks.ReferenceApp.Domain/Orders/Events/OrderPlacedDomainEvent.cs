using eBuildingBlocks.Domain.Models;

namespace eBuildingBlocks.ReferenceApp.Domain.Orders.Events;

/// <summary>
/// Contract published to the transactional outbox (stable name in IEventTypeRegistry)
/// and optionally to MassTransit after the outbox processor claims the row.
/// </summary>
/// <remarks>
/// Init-only DTO shape keeps System.Text.Json deserialization predictable for outbox replays.
/// </remarks>
public sealed record OrderPlacedDomainEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();

    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    public Guid? TenantId { get; init; }

    public Guid OrderId { get; init; }

    public string OrderNumber { get; init; } = string.Empty;

    public decimal TotalAmount { get; init; }

    public OrderPlacedDomainEvent()
    {
    }

    public OrderPlacedDomainEvent(Guid orderId, string orderNumber, decimal totalAmount, Guid tenantId)
    {
        OrderId = orderId;
        OrderNumber = orderNumber;
        TotalAmount = totalAmount;
        TenantId = tenantId;
    }
}
