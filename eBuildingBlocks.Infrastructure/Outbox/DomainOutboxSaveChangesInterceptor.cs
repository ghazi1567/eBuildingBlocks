using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using eBuildingBlocks.Application.Eventing;
using eBuildingBlocks.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace eBuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// Before <see cref="DbContext.SaveChangesAsync"/>, copies all <see cref="IDomainEvent"/> instances from tracked
/// aggregates into <see cref="OutboxMessage"/> rows (same transaction). Domain events are cleared on those aggregates
/// only after a successful save (<see cref="SavedChangesAsync"/>), so a failed commit does not lose in-memory events for retry.
/// Uses <see cref="IEventTypeRegistry"/> stable keys in <see cref="OutboxMessage.EventName"/> (not assembly-qualified names).
/// </summary>
public sealed class DomainOutboxSaveChangesInterceptor : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly ConcurrentDictionary<DbContext, List<object>> _pendingPostCommitDomainEventClears = new();
    private readonly IEventTypeRegistry _eventTypeRegistry;
    private readonly ILogger<DomainOutboxSaveChangesInterceptor> _logger;

    public DomainOutboxSaveChangesInterceptor(
        IEventTypeRegistry eventTypeRegistry,
        ILogger<DomainOutboxSaveChangesInterceptor> logger)
    {
        _eventTypeRegistry = eventTypeRegistry;
        _logger = logger;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not { } context)
            return await base.SavingChangesAsync(eventData, result, cancellationToken).ConfigureAwait(false);

        if (DomainOutboxExecutionContext.SuppressTransactionalEnqueue.Value)
            return await base.SavingChangesAsync(eventData, result, cancellationToken).ConfigureAwait(false);

        var toEnqueue = new List<(object Entity, IReadOnlyList<IDomainEvent> Events)>();
        foreach (var (entity, events) in DomainEventChangeTrackerHelper.CollectEntitiesWithDomainEvents(context))
            toEnqueue.Add((entity, events.ToList()));

        if (toEnqueue.Count == 0)
            return await base.SavingChangesAsync(eventData, result, cancellationToken).ConfigureAwait(false);

        var now = DateTime.UtcNow;
        var outboxRows = new List<OutboxMessage>();

        foreach (var (_, events) in toEnqueue)
        {
            foreach (var @event in events)
            {
                var type = @event.GetType();
                if (!_eventTypeRegistry.TryGetEventName(type, out var eventName))
                {
                    throw new InvalidOperationException(
                        $"Domain event type '{type.FullName}' is not registered in {nameof(IEventTypeRegistry)}. " +
                        $"Call {nameof(IEventTypeRegistry.Register)}<{type.Name}>(\"your.stable.name.v1\") at startup.");
                }

                outboxRows.Add(new OutboxMessage
                {
                    Id = Guid.CreateVersion7(),
                    DomainEventId = @event.EventId,
                    EventName = eventName,
                    PayloadJson = JsonSerializer.Serialize(@event, type, JsonOptions),
                    CreatedAtUtc = now,
                    TenantId = @event.TenantId
                });
            }
        }

        context.Set<OutboxMessage>().AddRange(outboxRows);

        var toClearAfterCommit = toEnqueue.Select(t => t.Entity).Distinct().ToList();
        _pendingPostCommitDomainEventClears[context] = toClearAfterCommit;

        _logger.LogDebug("Enqueued {Count} outbox message(s); domain events will clear after successful save", outboxRows.Count);

        return await base.SavingChangesAsync(eventData, result, cancellationToken).ConfigureAwait(false);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is DbContext ctx &&
            _pendingPostCommitDomainEventClears.TryRemove(ctx, out var entities))
        {
            foreach (var entity in entities)
                DomainEventChangeTrackerHelper.ClearDomainEventsOnEntity(entity);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken).ConfigureAwait(false);
    }

    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is DbContext ctx)
            _pendingPostCommitDomainEventClears.TryRemove(ctx, out _);

        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }
}
