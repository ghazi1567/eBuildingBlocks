using eBuildingBlocks.Application.Events;
using eBuildingBlocks.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;

namespace eBuildingBlocks.Infrastructure.Extensions;

/// <summary>
/// Extension methods for repository and <see cref="DbContext"/> operations.
/// </summary>
public static class RepositoryExtensions
{
    /// <summary>
    /// Persists changes; <see cref="DomainOutboxSaveChangesInterceptor"/> enqueues <see cref="T:eBuildingBlocks.Domain.Models.OutboxMessage"/> rows
    /// and clears aggregate domain events only after a successful commit. Use this when the outbox processor delivers events.
    /// Do not combine with <see cref="SaveChangesAndPublishDomainEventsInProcessAsync"/> for the same logical event handling.
    /// </summary>
    public static Task<int> SaveChangesWithTransactionalOutboxAsync<TContext>(
        this TContext context,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
        => context.SaveChangesAsync(cancellationToken);

    /// <summary>
    /// Snapshots domain events from the change tracker, saves, clears domain events on aggregates (before publishing),
    /// then publishes in-process from the snapshot. Clearing before the publish loop avoids leaving events on entities
    /// if <see cref="IEventBus.PublishAsync"/> fails partway through (retries would otherwise re-dispatch from the model).
    /// Suppresses transactional outbox enqueue for this call when <see cref="Outbox.DomainOutboxSaveChangesInterceptor"/> is registered.
    /// Prefer <see cref="SaveChangesWithTransactionalOutboxAsync{TContext}"/> when using the outbox processor instead.
    /// </summary>
    public static async Task<int> SaveChangesAndPublishDomainEventsInProcessAsync<TContext>(
        this TContext context,
        IEventBus eventBus,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        var previousSuppress = DomainOutboxExecutionContext.SuppressTransactionalEnqueue.Value;
        DomainOutboxExecutionContext.SuppressTransactionalEnqueue.Value = true;
        try
        {
            var entitiesWithEvents = DomainEventChangeTrackerHelper.CollectEntitiesWithDomainEvents(context);
            var allEvents = entitiesWithEvents.SelectMany(e => e.Events).ToList();

            var result = await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            // Clear aggregates before publishing so a partial publish failure cannot leave events on entities
            // (retry would otherwise re-dispatch already-handled work). Publishing uses the allEvents snapshot only.
            foreach (var (entity, _) in entitiesWithEvents)
                DomainEventChangeTrackerHelper.ClearDomainEventsOnEntity(entity);

            foreach (var @event in allEvents)
            {
                try
                {
                    await eventBus.PublishAsync(@event, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Error publishing domain event of type {@event.GetType().Name}. " +
                        "The database transaction was successful, but event publishing failed.", ex);
                }
            }

            return result;
        }
        finally
        {
            DomainOutboxExecutionContext.SuppressTransactionalEnqueue.Value = previousSuppress;
        }
    }

    /// <summary>
    /// Obsolete: use <see cref="SaveChangesWithTransactionalOutboxAsync{TContext}"/> with the outbox interceptor, or
    /// <see cref="SaveChangesAndPublishDomainEventsInProcessAsync{TContext}"/> for in-process-only dispatch.
    /// </summary>
    [Obsolete(
        "Use SaveChangesWithTransactionalOutboxAsync when using DomainOutboxSaveChangesInterceptor, or SaveChangesAndPublishDomainEventsInProcessAsync for in-process-only. This overload now matches in-process behavior (outbox enqueue suppressed for this call).")]
    public static Task<int> SaveChangesAndPublishEventsAsync<TContext>(
        this TContext context,
        IEventBus eventBus,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
        => SaveChangesAndPublishDomainEventsInProcessAsync(context, eventBus, cancellationToken);
}
