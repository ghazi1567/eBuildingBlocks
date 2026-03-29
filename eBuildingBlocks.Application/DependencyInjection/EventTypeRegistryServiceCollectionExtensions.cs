using eBuildingBlocks.Application.Eventing;
using Microsoft.Extensions.DependencyInjection;

namespace eBuildingBlocks.Application.DependencyInjection;

/// <summary>
/// Registers <see cref="IEventTypeRegistry"/> for outbox and integration event typing.
/// </summary>
public static class EventTypeRegistryServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="EventTypeRegistry"/> as a singleton. Call <see cref="IEventTypeRegistry.Register{TEvent}"/>
    /// for each domain event type at startup (e.g. from <c>Program.cs</c> or a static module initializer).
    /// </summary>
    public static IServiceCollection AddEventTypeRegistry(this IServiceCollection services)
    {
        services.AddSingleton<IEventTypeRegistry, EventTypeRegistry>();
        return services;
    }
}
