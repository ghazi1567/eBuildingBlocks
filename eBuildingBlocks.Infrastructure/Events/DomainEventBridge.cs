using eBuildingBlocks.Application.Events;
using eBuildingBlocks.Domain.Models;
using BuildingBlocks.EventBus.Contracts;
using BuildingBlocks.EventBus.Events;

namespace eBuildingBlocks.Infrastructure.Events
{
    /// <summary>
    /// Bridge to publish domain events as integration events.
    /// Useful when evolving from modular monolith to microservices.
    /// </summary>
    public class DomainEventBridge : IEventHandler<IDomainEvent>
    {
        private readonly IEventPublisher _integrationEventPublisher;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="DomainEventBridge"/> class.
        /// </summary>
        /// <param name="integrationEventPublisher">The integration event publisher.</param>
        public DomainEventBridge(IEventPublisher integrationEventPublisher)
        {
            _integrationEventPublisher = integrationEventPublisher;
        }
        
        /// <summary>
        /// Handles the domain event by transforming it to an integration event.
        /// </summary>
        /// <param name="domainEvent">The domain event to handle.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Task representing the async operation.</returns>
        public async Task HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
        {
            if (domainEvent == null)
                throw new ArgumentNullException(nameof(domainEvent));
            
            // Transform domain event to integration event
            var integrationEvent = new IntegrationEvent
            {
                EventId = domainEvent.EventId,
                OccurredAt = domainEvent.OccurredAt,
                Payload = domainEvent
            };
            
            await _integrationEventPublisher.PublishAsync(integrationEvent);
        }
    }
}
