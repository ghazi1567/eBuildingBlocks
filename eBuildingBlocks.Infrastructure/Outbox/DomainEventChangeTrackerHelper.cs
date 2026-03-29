using System.Reflection;
using eBuildingBlocks.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace eBuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// Shared change-tracker scan and clear for domain events (outbox interceptor + in-process publish extension).
/// </summary>
internal static class DomainEventChangeTrackerHelper
{
    internal static List<(object Entity, IReadOnlyCollection<IDomainEvent> Events)> CollectEntitiesWithDomainEvents(DbContext context)
    {
        context.ChangeTracker.DetectChanges();
        var result = new List<(object, IReadOnlyCollection<IDomainEvent>)>();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            var entity = entry.Entity;
            IReadOnlyCollection<IDomainEvent>? domainEvents = null;

            if (entity is IHasDomainEvents withEvents)
            {
                domainEvents = withEvents.DomainEvents;
            }
            else
            {
                var prop = entity.GetType().GetProperty(
                    nameof(IHasDomainEvents.DomainEvents),
                    BindingFlags.Public | BindingFlags.Instance);
                if (prop != null)
                    domainEvents = prop.GetValue(entity) as IReadOnlyCollection<IDomainEvent>;
            }

            if (domainEvents is { Count: > 0 })
                result.Add((entity, domainEvents));
        }

        return result;
    }

    internal static void ClearDomainEventsOnEntity(object entity)
    {
        if (entity is IHasDomainEvents has)
        {
            has.ClearDomainEvents();
            return;
        }

        entity.GetType()
            .GetMethod(
                nameof(IHasDomainEvents.ClearDomainEvents),
                BindingFlags.Public | BindingFlags.Instance)
            ?.Invoke(entity, null);
    }
}
