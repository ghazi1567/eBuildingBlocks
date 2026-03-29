using eBuildingBlocks.Application.Eventing;
using eBuildingBlocks.ReferenceApp.Domain.Orders.Events;
using Microsoft.Extensions.Hosting;

namespace eBuildingBlocks.ReferenceApp.API.Hosting;

/// <summary>
/// Registers stable outbox keys before any <c>SaveChanges</c> that emits domain events; the interceptor fails fast
/// if a CLR event type is not mapped (see docs/HOSTING_APP_OUTBOX.md).
/// </summary>
public sealed class OutboxEventCatalogStartup(IEventTypeRegistry registry) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        registry.Register<OrderPlacedDomainEvent>("order.placed.v1");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
