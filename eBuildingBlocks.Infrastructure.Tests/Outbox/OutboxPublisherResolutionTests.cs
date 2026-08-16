using BuildingBlocks.EventBus.Events;
using eBuildingBlocks.Infrastructure.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace eBuildingBlocks.Infrastructure.Tests.Outbox;

public class OutboxPublisherResolutionTests
{
    [Fact]
    public void ResolvePublisher_PrefersModernPublisher_AndNeverTouchesLegacy_WhenBothRegistered()
    {
        var services = new ServiceCollection();
        var modern = new FakeOutboxIntegrationPublisher();
        services.AddSingleton<eBuildingBlocks.Common.Outbox.IOutboxIntegrationPublisher>(modern);

        var legacyFactoryInvoked = false;
        services.AddSingleton<IEventPublisher>(_ =>
        {
            legacyFactoryInvoked = true;
            return new FakeEventPublisher();
        });

        using var sp = services.BuildServiceProvider();
        var logger = new RecordingLogger();

        var resolved = OutboxProcessorBackgroundService<FakeDbContext1>.ResolvePublisher(sp, logger);

        Assert.Same(modern, resolved);
        Assert.False(legacyFactoryInvoked, "IEventPublisher should never be resolved when IOutboxIntegrationPublisher is registered");
        Assert.Equal(0, logger.WarningCount);
    }

    [Fact]
    public async Task ResolvePublisher_FallsBackToLegacyAdapter_WhenOnlyEventPublisherRegistered()
    {
        var services = new ServiceCollection();
        var legacy = new FakeEventPublisher();
        services.AddSingleton<IEventPublisher>(legacy);

        using var sp = services.BuildServiceProvider();
        var logger = new RecordingLogger();

        var resolved = OutboxProcessorBackgroundService<FakeDbContext2>.ResolvePublisher(sp, logger);

        Assert.IsType<LegacyEventPublisherAdapter>(resolved);

        await resolved.PublishAsync(new object(), CancellationToken.None);

        Assert.Equal(1, legacy.CallCount);
        Assert.Equal(1, logger.WarningCount);
    }

    [Fact]
    public void ResolvePublisher_Throws_WhenNeitherPublisherRegistered()
    {
        var services = new ServiceCollection();
        using var sp = services.BuildServiceProvider();
        var logger = new RecordingLogger();

        var ex = Assert.Throws<InvalidOperationException>(
            () => OutboxProcessorBackgroundService<FakeDbContext3>.ResolvePublisher(sp, logger));

        Assert.Contains("IOutboxIntegrationPublisher", ex.Message);
    }

    [Fact]
    public void ResolvePublisher_LogsLegacyWarningExactlyOnce_AcrossMultipleResolutions()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IEventPublisher>(new FakeEventPublisher());

        using var sp = services.BuildServiceProvider();
        var logger = new RecordingLogger();

        // Simulates ResolvePublisher being called once per ProcessOnceAsync iteration.
        OutboxProcessorBackgroundService<FakeDbContext4>.ResolvePublisher(sp, logger);
        OutboxProcessorBackgroundService<FakeDbContext4>.ResolvePublisher(sp, logger);
        OutboxProcessorBackgroundService<FakeDbContext4>.ResolvePublisher(sp, logger);

        Assert.Equal(1, logger.WarningCount);
    }
}
