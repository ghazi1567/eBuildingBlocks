using eBuildingBlocks.Application.Events;
using eBuildingBlocks.Infrastructure.Events;
using Microsoft.Extensions.DependencyInjection;

namespace eBuildingBlocks.Infrastructure.Extensions
{
    /// <summary>
    /// Extension methods for registering event bus services.
    /// </summary>
    public static class EventBusExtensions
    {
        /// <summary>
        /// Adds the in-process event bus to the service collection.
        /// </summary>
        /// <remarks>
        /// Multiple <see cref="IEventHandler{TEvent}"/> implementations for the same event type run in
        /// non-deterministic order; they must be independent and idempotent.
        /// </remarks>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional action to configure event bus options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddInProcessEventBus(
            this IServiceCollection services, 
            Action<EventBusOptions>? configure = null)
        {
            if (configure != null)
            {
                services.Configure(configure);
            }
            else
            {
                // Register default options
                services.Configure<EventBusOptions>(options => 
                {
                    options.FailureMode = EventHandlerFailureMode.FailFast;
                });
            }
            
            services.AddScoped<IEventBus, InProcessEventBus>();
            return services;
        }
    }
}
