using eBuildingBlocks.Application.Events;
using eBuildingBlocks.Domain.Exceptions;
using eBuildingBlocks.Domain.Interfaces;
using eBuildingBlocks.ReferenceApp.Domain.Orders.Events;
using MassTransit;

namespace eBuildingBlocks.ReferenceApp.API.Messaging.Consumers;

/// <summary>
/// When the outbox processor publishes <see cref="OrderPlacedDomainEvent"/> through <see cref="BuildingBlocks.EventBus.Events.IEventPublisher"/>,
/// MassTransit delivers it here — mirroring how another microservice would react in a distributed B2B topology.
/// Wraps handling in <see cref="ITenantScope.Begin"/> when the event carries a tenant id so downstream <see cref="ICurrentUser"/> / EF filters resolve correctly.
/// </summary>
public sealed class OrderPlacedMassTransitConsumer(
    ITenantScope tenantScope,
    IEventHandler<OrderPlacedDomainEvent> handler) : IConsumer<OrderPlacedDomainEvent>
{
    public Task Consume(ConsumeContext<OrderPlacedDomainEvent> context)
    {
        var message = context.Message;
        if (message.TenantId == Guid.Empty)
        {
            throw new TenantResolutionException(
                $"{nameof(OrderPlacedDomainEvent)}.{nameof(OrderPlacedDomainEvent.TenantId)} must be non-empty for multi-tenant processing.");
        }

        using (tenantScope.Begin(message.TenantId))
            return handler.HandleAsync(message, context.CancellationToken);
    }
}
