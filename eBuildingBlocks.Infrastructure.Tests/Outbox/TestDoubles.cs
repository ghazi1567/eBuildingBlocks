using eBuildingBlocks.Common.Outbox;
using Microsoft.EntityFrameworkCore;

namespace eBuildingBlocks.Infrastructure.Tests.Outbox;

/// <summary>
/// Never configured with a provider and never touches the database — used only to satisfy
/// OutboxProcessorBackgroundService's TDbContext resolution before it reaches the
/// publisher-resolution line under test.
/// </summary>
internal sealed class FakeDbContext : DbContext;

internal sealed class FakeOutboxIntegrationPublisher : IOutboxIntegrationPublisher
{
    public int CallCount { get; private set; }

    public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
    {
        CallCount++;
        return Task.CompletedTask;
    }
}
