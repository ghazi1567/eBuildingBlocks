using eBuildingBlocks.Domain.Models;
using eBuildingBlocks.ReferenceApp.Domain.Orders.Events;

namespace eBuildingBlocks.ReferenceApp.Domain.Orders;

/// <summary>
/// Tenant-scoped aggregate root. Inherits <see cref="TenantEntity{TKey}"/> so the host can stamp
/// <see cref="TenantEntity{TKey}.TenantId"/> and audit columns via <c>ICurrentUser</c> + interceptors.
/// </summary>
public sealed class Order : TenantEntity<Guid>
{
    /// <summary>Human-readable identifier (e.g. PO number) shown to B2B buyers.</summary>
    public string OrderNumber { get; private set; } = string.Empty;

    /// <summary>Monetary total for the reference scenario (single line-item sum omitted for brevity).</summary>
    public decimal TotalAmount { get; private set; }

    public OrderStatus Status { get; private set; }

    /// <summary>ORM / serializer constructor.</summary>
    private Order()
    {
    }

    /// <summary>Factory for new drafts (Id assigned before persistence).</summary>
    public static Order CreateDraft(Guid tenantId)
    {
        return new Order
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            Status = OrderStatus.Draft
        };
    }

    /// <summary>
    /// Domain behavior: transitions to <see cref="OrderStatus.Placed"/> and records
    /// <see cref="OrderPlacedDomainEvent"/> via <see cref="BaseEntity{TKey}.AddDomainEvent"/> so infrastructure
    /// can persist the same logical fact to the transactional outbox on <c>SaveChanges</c>.
    /// </summary>
    public void PlaceOrder(string orderNumber, decimal totalAmount)
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException("Only draft orders can be placed.");

        if (string.IsNullOrWhiteSpace(orderNumber))
            throw new ArgumentException("Order number is required.", nameof(orderNumber));

        if (totalAmount <= 0)
            throw new ArgumentOutOfRangeException(nameof(totalAmount), "Total must be positive.");

        OrderNumber = orderNumber.Trim();
        TotalAmount = totalAmount;
        Status = OrderStatus.Placed;

        // Raised events are collected on the aggregate; DomainOutboxSaveChangesInterceptor copies them into OutboxMessages.
        AddDomainEvent(new OrderPlacedDomainEvent(Id, OrderNumber, TotalAmount, TenantId));
    }
}
