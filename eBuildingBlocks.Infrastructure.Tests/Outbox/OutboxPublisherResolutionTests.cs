using eBuildingBlocks.Common.Outbox;
using eBuildingBlocks.Infrastructure.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace eBuildingBlocks.Infrastructure.Tests.Outbox;

/// <summary>
/// Phase 2 removed the legacy-fallback ResolvePublisher helper entirely — the outbox
/// processor now resolves its publisher with a single, direct
/// sp.GetRequiredService&lt;IOutboxIntegrationPublisher&gt;() call inside ProcessOnceAsync.
/// </summary>
public class OutboxPublisherResolutionTests
{
    [Fact]
    public void GetRequiredService_ReturnsRegisteredPublisher_WhenRegistered()
    {
        var services = new ServiceCollection();
        var modern = new FakeOutboxIntegrationPublisher();
        services.AddSingleton<IOutboxIntegrationPublisher>(modern);

        using var sp = services.BuildServiceProvider();

        Assert.Same(modern, sp.GetRequiredService<IOutboxIntegrationPublisher>());
    }

    [Fact]
    public async Task ProcessOnceAsync_Throws_WhenIOutboxIntegrationPublisherNotRegistered()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new FakeDbContext());
        // Intentionally no IOutboxIntegrationPublisher registration.

        using var sp = services.BuildServiceProvider();
        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
        var options = Options.Create(new OutboxProcessorOptions());
        var logger = NullLogger<OutboxProcessorBackgroundService<FakeDbContext>>.Instance;

        var service = new OutboxProcessorBackgroundService<FakeDbContext>(scopeFactory, options, logger);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ProcessOnceAsync(CancellationToken.None));

        Assert.Contains("IOutboxIntegrationPublisher", ex.Message);
    }
}
