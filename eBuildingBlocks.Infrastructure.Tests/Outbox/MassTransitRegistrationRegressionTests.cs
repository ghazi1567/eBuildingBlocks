using BuildingBlocks.EventBus.Events;
using eBuildingBlocks.Common.Outbox;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace eBuildingBlocks.Infrastructure.Tests.Outbox;

public sealed record ProbeIntegrationEvent(Guid CorrelationId);

public sealed class ProbeConsumer(TaskCompletionSource<ProbeIntegrationEvent> received) : MassTransit.IConsumer<ProbeIntegrationEvent>
{
    public Task Consume(MassTransit.ConsumeContext<ProbeIntegrationEvent> context)
    {
        received.TrySetResult(context.Message);
        return Task.CompletedTask;
    }
}

/// <summary>
/// No eBuildingBlocks.ReferenceApp.* test harness exists in this repo (see CLAUDE.md: "There are
/// no automated test projects"), so this stands in for the plan's optional end-to-end regression
/// check: it proves AddIntegrationMassTransit (eBuildingBlocks.EventBus 3.1.0+) registers
/// IOutboxIntegrationPublisher as the exact same instance as IEventPublisher, and that publishing
/// through the new interface still round-trips through a real MassTransit bus, unchanged.
/// </summary>
public class MassTransitRegistrationRegressionTests
{
    [Fact]
    public async Task AddIntegrationMassTransit_RegistersOutboxIntegrationPublisher_AsSameInstance_AndStillPublishesViaMassTransit()
    {
        var received = new TaskCompletionSource<ProbeIntegrationEvent>(TaskCreationOptions.RunContinuationsAsynchronously);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["MassTransit:UseInMemory"] = "true" })
            .Build();

        using var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(received);
                services.AddIntegrationMassTransit(configuration, typeof(ProbeConsumer).Assembly);
            })
            .Build();

        await host.StartAsync();
        try
        {
            using var scope = host.Services.CreateScope();
            var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
            var outboxPublisher = scope.ServiceProvider.GetRequiredService<IOutboxIntegrationPublisher>();

            Assert.Same(eventPublisher, outboxPublisher);

            var probe = new ProbeIntegrationEvent(Guid.NewGuid());
            await outboxPublisher.PublishAsync(probe);

            var consumed = await received.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal(probe.CorrelationId, consumed.CorrelationId);
        }
        finally
        {
            await host.StopAsync();
        }
    }
}
