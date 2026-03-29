using eBuildingBlocks.Application.Events;
using eBuildingBlocks.ReferenceApp.Domain.Orders.Events;
using MassTransit;

namespace eBuildingBlocks.ReferenceApp.API.Messaging.Consumers;

/// <summary>
/// When the outbox processor publishes <see cref="OrderPlacedDomainEvent"/> through <see cref="BuildingBlocks.EventBus.Events.IEventPublisher"/>,
/// MassTransit delivers it here — mirroring how another microservice would react in a distributed B2B topology.
/// </summary>
public sealed class OrderPlacedMassTransitConsumer(IEventHandler<OrderPlacedDomainEvent> handler) : IConsumer<OrderPlacedDomainEvent>
{
    public Task Consume(ConsumeContext<OrderPlacedDomainEvent> context) =>
        handler.HandleAsync(context.Message, context.CancellationToken);
}
