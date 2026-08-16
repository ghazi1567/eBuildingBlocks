using BuildingBlocks.EventBus.Events;
using eBuildingBlocks.Common.Outbox;

namespace eBuildingBlocks.Infrastructure.Outbox;

[Obsolete("Only used to bridge legacy IEventPublisher consumers. Will be removed alongside the legacy fallback.")]
internal sealed class LegacyEventPublisherAdapter(IEventPublisher inner) : IOutboxIntegrationPublisher
{
    public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
        => inner.PublishAsync(@event, cancellationToken);
}
