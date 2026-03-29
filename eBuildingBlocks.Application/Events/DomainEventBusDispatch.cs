using System.Collections.Concurrent;
using System.Linq;
using eBuildingBlocks.Domain.Models;

namespace eBuildingBlocks.Application.Events;

/// <summary>
/// Cached dispatch from <see cref="IDomainEvent"/> (runtime type) to
/// <see cref="IEventBus.PublishAsync{TEvent}(TEvent, System.Threading.CancellationToken)"/>.
/// </summary>
internal static class DomainEventBusDispatch
{
    private static readonly ConcurrentDictionary<Type, Func<IEventBus, IDomainEvent, CancellationToken, Task>> Cache = new();

    public static Task PublishAsync(IEventBus bus, IDomainEvent @event, CancellationToken cancellationToken)
    {
        var eventType = @event.GetType();
        var invoker = Cache.GetOrAdd(eventType, BuildInvoker);
        return invoker(bus, @event, cancellationToken);
    }

    private static Func<IEventBus, IDomainEvent, CancellationToken, Task> BuildInvoker(Type eventType)
    {
        var open = typeof(IEventBus).GetMethods()
            .Single(m => m is { Name: nameof(IEventBus.PublishAsync), IsGenericMethodDefinition: true }
                         && m.GetGenericArguments().Length == 1);
        var closed = open.MakeGenericMethod(eventType);
        return (bus, ev, ct) => (Task)closed.Invoke(bus, [ev, ct])!;
    }
}
