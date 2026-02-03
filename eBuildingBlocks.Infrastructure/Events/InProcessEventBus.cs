using eBuildingBlocks.Application.Events;
using eBuildingBlocks.Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace eBuildingBlocks.Infrastructure.Events
{
    /// <summary>
    /// In-process event bus implementation for modular monoliths.
    /// Uses dependency injection to resolve event handlers.
    /// </summary>
    public class InProcessEventBus : IEventBus
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<InProcessEventBus> _logger;
        private readonly EventBusOptions _options;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="InProcessEventBus"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider for resolving event handlers.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="options">The event bus options. If null, default options are used.</param>
        public InProcessEventBus(
            IServiceProvider serviceProvider,
            ILogger<InProcessEventBus> logger,
            Microsoft.Extensions.Options.IOptions<EventBusOptions>? options = null)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _options = options?.Value ?? new EventBusOptions();
        }
        
        /// <summary>
        /// Publishes a domain event to all registered handlers.
        /// </summary>
        /// <typeparam name="TEvent">The type of domain event.</typeparam>
        /// <param name="event">The domain event to publish.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Task representing the async operation.</returns>
        public async Task PublishAsync<TEvent>(
            TEvent @event, 
            CancellationToken cancellationToken = default) 
            where TEvent : IDomainEvent
        {
            if (@event == null)
                throw new ArgumentNullException(nameof(@event));
            
            var handlerType = typeof(IEventHandler<>).MakeGenericType(typeof(TEvent));
            var handlers = _serviceProvider.GetServices(handlerType);
            
            if (!handlers.Any())
            {
                _logger.LogDebug("No handlers found for event type {EventType}", typeof(TEvent).Name);
                return;
            }
            
            var exceptions = new List<Exception>();
            
            foreach (var handler in handlers)
            {
                try
                {
                    var method = handlerType.GetMethod("HandleAsync");
                    var task = (Task)method!.Invoke(handler, new object[] { @event, cancellationToken })!;
                    await task;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, 
                        "Error handling event {EventType} in handler {HandlerType}", 
                        typeof(TEvent).Name, 
                        handler.GetType().Name);
                    
                    if (_options.FailureMode == EventHandlerFailureMode.FailFast)
                    {
                        throw;
                    }
                    
                    exceptions.Add(ex);
                }
            }
            
            if (exceptions.Any() && _options.FailureMode == EventHandlerFailureMode.Continue)
            {
                _logger.LogWarning(
                    "Some event handlers failed for event {EventType}. {Count} handler(s) failed.",
                    typeof(TEvent).Name,
                    exceptions.Count);
                // In Continue mode, we log but don't throw
            }
        }
    }
}
