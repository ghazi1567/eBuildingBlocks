using Microsoft.Extensions.Logging;

namespace eBuildingBlocks.ReferenceApp.API.Hosting;

/// <summary>
/// Hangfire recurring job placeholder. The eBuildingBlocks transactional outbox is drained by
/// <see cref="eBuildingBlocks.Infrastructure.Outbox.OutboxProcessorBackgroundService{TDbContext}"/> (registered via
/// <c>AddOutboxProcessor&lt;ReferenceDbContext&gt;</c>), which uses SQL Server locking and <see cref="BuildingBlocks.EventBus.Events.IEventPublisher"/>.
/// Use Hangfire here for operational tasks, or replace this job with your own adapter that calls shared outbox logic if you
/// standardize on Hangfire as the only scheduler in your organization.
/// </summary>
public sealed class ReferenceHangfireOutboxBridgeJob(ILogger<ReferenceHangfireOutboxBridgeJob> logger)
{
    public Task RunScheduledOperationalHeartbeatAsync()
    {
        logger.LogDebug("Hangfire heartbeat: outbox is processed by OutboxProcessorBackgroundService; use dashboard at /hangfire.");
        return Task.CompletedTask;
    }
}
