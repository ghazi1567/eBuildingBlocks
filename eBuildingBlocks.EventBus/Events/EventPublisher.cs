using BuildingBlocks.EventBus.Contracts;
using eBuildingBlocks.Common.Features;
using MassTransit;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.EventBus.Events;

public sealed class EventPublisher(
    IPublishEndpoint publishEndpoint,
    IOptionsMonitor<MultiTenancyOptions> multiTenancyOptions) : IEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint = publishEndpoint;
    private readonly IOptionsMonitor<MultiTenancyOptions> _multiTenancyOptions = multiTenancyOptions;

    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
    {
        await _publishEndpoint.Publish(@event, cancellationToken).ConfigureAwait(false);
    }

    public async Task PublishAsync(IntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        var mt = _multiTenancyOptions.CurrentValue;
        if (mt.Enabled && @event.TenantId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "IntegrationEvent.TenantId must be set when multi-tenancy is enabled.");
        }

        await _publishEndpoint.Publish(@event, cancellationToken).ConfigureAwait(false);
    }
}
