using eBuildingBlocks.Application.Events;
using eBuildingBlocks.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace eBuildingBlocks.Infrastructure.Extensions
{
    /// <summary>
    /// Extension methods for repository and DbContext operations.
    /// </summary>
    public static class RepositoryExtensions
    {
        /// <summary>
        /// Saves changes and publishes domain events from all entities.
        /// </summary>
        /// <typeparam name="TContext">The type of DbContext.</typeparam>
        /// <param name="context">The database context.</param>
        /// <param name="eventBus">The event bus for publishing events.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of state entries written to the database.</returns>
        public static async Task<int> SaveChangesAndPublishEventsAsync<TContext>(
            this TContext context,
            IEventBus eventBus,
            CancellationToken cancellationToken = default)
            where TContext : DbContext
        {
            // Collect events from all entities that have domain events
            // Use reflection to access DomainEvents property since BaseEntity<TKey> is generic
            var entitiesWithEvents = new List<(object Entity, IReadOnlyCollection<IDomainEvent> Events)>();
            
            foreach (var entry in context.ChangeTracker.Entries())
            {
                var entity = entry.Entity;
                var entityType = entity.GetType();
                
                // Check if entity has DomainEvents property (from BaseEntity<TKey>)
                var domainEventsProperty = entityType.GetProperty("DomainEvents");
                if (domainEventsProperty != null)
                {
                    var domainEvents = domainEventsProperty.GetValue(entity) as IReadOnlyCollection<IDomainEvent>;
                    if (domainEvents != null && domainEvents.Any())
                    {
                        entitiesWithEvents.Add((entity, domainEvents));
                    }
                }
            }
            
            var allEvents = entitiesWithEvents
                .SelectMany(e => e.Events)
                .ToList();
            
            // Save changes first (transaction)
            var result = await context.SaveChangesAsync(cancellationToken);
            
            // Publish events after successful save
            foreach (var @event in allEvents)
            {
                try
                {
                    // Use reflection to call PublishAsync with correct generic type
                    var eventType = @event.GetType();
                    var method = typeof(IEventBus).GetMethod("PublishAsync")!
                        .MakeGenericMethod(eventType);
                    await (Task)method.Invoke(eventBus, new object[] { @event, cancellationToken })!;
                }
                catch (Exception ex)
                {
                    // Log error but don't fail the transaction
                    // Consider implementing dead letter queue or retry mechanism
                    throw new InvalidOperationException(
                        $"Error publishing domain event of type {@event.GetType().Name}. " +
                        "The database transaction was successful, but event publishing failed.", ex);
                }
            }
            
            // Clear events after publishing
            foreach (var (entity, _) in entitiesWithEvents)
            {
                var clearMethod = entity.GetType().GetMethod("ClearDomainEvents");
                clearMethod?.Invoke(entity, null);
            }
            
            return result;
        }
    }
}
