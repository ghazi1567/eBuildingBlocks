using eBuildingBlocks.Domain.Models;

namespace eBuildingBlocks.Application.Events
{
    /// <summary>
    /// Handler interface for domain events.
    /// </summary>
    /// <typeparam name="TEvent">The type of domain event to handle.</typeparam>
    public interface IEventHandler<in TEvent> where TEvent : IDomainEvent
    {
        /// <summary>
        /// Handles the domain event.
        /// </summary>
        /// <param name="event">The domain event to handle.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Task representing the async operation.</returns>
        Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
    }
}
