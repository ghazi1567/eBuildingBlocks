using eBuildingBlocks.Application.Events;
using eBuildingBlocks.ReferenceApp.Domain.Orders.Events;
using Microsoft.Extensions.Logging;

namespace eBuildingBlocks.ReferenceApp.Application.Orders;

/// <summary>
/// In-process handler contract from eBuildingBlocks. In this reference host the same type is also invoked from
/// MassTransit (<c>OrderPlacedMassTransitConsumer</c> in the API project) after the outbox processor publishes the message,
/// so side effects stay in one class without double-firing on SaveChanges.
/// </summary>
public sealed class OrderPlacedDomainEventHandler(ILogger<OrderPlacedDomainEventHandler> logger) : IEventHandler<OrderPlacedDomainEvent>
{
    public Task HandleAsync(OrderPlacedDomainEvent @event, CancellationToken cancellationToken = default)
    {
        // Simulate local notification (email, CRM tickler, etc.) — idempotent by EventId in real systems.
        logger.LogInformation(
            "Order placed notification: EventId={EventId} Order={OrderNumber} Amount={Amount} Tenant={Tenant}",
            @event.EventId,
            @event.OrderNumber,
            @event.TotalAmount,
            @event.TenantId);

        return Task.CompletedTask;
    }
}
