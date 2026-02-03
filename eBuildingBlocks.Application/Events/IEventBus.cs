using eBuildingBlocks.Domain.Models;

namespace eBuildingBlocks.Application.Events
{
    /// <summary>
    /// Event bus interface for publishing domain events.
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// Publishes a domain event to all registered handlers.
        /// </summary>
        /// <typeparam name="TEvent">The type of domain event.</typeparam>
        /// <param name="event">The domain event to publish.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Task representing the async operation.</returns>
        Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) 
            where TEvent : IDomainEvent;
    }
}
