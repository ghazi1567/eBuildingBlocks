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

        /// <summary>
        /// Publishes when the instance is only known as <see cref="IDomainEvent"/> (e.g. after save, from a list of mixed events).
        /// Default implementation dispatches to the generic <see cref="PublishAsync{TEvent}"/> using the runtime type.
        /// Existing implementations of <see cref="IEventBus"/> do not need to add a member: this default applies.
        /// </summary>
        async Task PublishAsync(IDomainEvent @event, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(@event);
            await DomainEventBusDispatch.PublishAsync(this, @event, cancellationToken);
        }
    }
}
