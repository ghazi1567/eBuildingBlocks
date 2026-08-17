using eBuildingBlocks.Infrastructure.Outbox;
using Xunit;

namespace eBuildingBlocks.Infrastructure.Tests.Outbox;

/// <summary>
/// Regression guard for Phase 2 of the outbox-publisher decoupling: fails if
/// eBuildingBlocks.Infrastructure ever re-introduces a ProjectReference to
/// eBuildingBlocks.EventBus.
///
/// Checks for "eBuildingBlocks.EventBus" itself, not just "MassTransit"/"RabbitMQ.Client":
/// verified empirically (by building the pre-Phase-2 code and inspecting the compiled
/// eBuildingBlocks.Infrastructure.dll) that Infrastructure's own IL only ever referenced
/// eBuildingBlocks.EventBus directly — never a MassTransit or RabbitMQ.Client type, since
/// those were only used inside eBuildingBlocks.EventBus itself. So
/// Assembly.GetReferencedAssemblies() never listed "MassTransit"/"RabbitMQ.Client" even in
/// the pre-Phase-2 state; checking only for those names would not have caught the
/// dependency this task removes. MassTransit/RabbitMQ.Client are still checked here for
/// defense-in-depth in case Infrastructure's code ever references them directly in future.
/// </summary>
public class InfrastructureAssemblyDependencyTests
{
    [Fact]
    public void InfrastructureAssembly_DoesNotReferenceEventBusOrMassTransitOrRabbitMq()
    {
        var referencedNames = typeof(OutboxProcessorBackgroundService<>).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name)
            .Where(name => name is not null)
            .Cast<string>()
            .ToList();

        Assert.DoesNotContain(referencedNames, name => name.Contains("EventBus", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(referencedNames, name => name.Contains("MassTransit", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(referencedNames, name => name.Contains("RabbitMQ", StringComparison.OrdinalIgnoreCase));
    }
}
