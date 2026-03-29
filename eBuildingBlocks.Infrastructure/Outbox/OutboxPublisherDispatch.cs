using System.Collections.Concurrent;
using System.Linq;
using BuildingBlocks.EventBus.Events;

namespace eBuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// Invokes <see cref="IEventPublisher.PublishAsync{T}"/> for a payload whose type is only known at runtime.
/// </summary>
internal static class OutboxPublisherDispatch
{
    private static readonly ConcurrentDictionary<Type, Func<IEventPublisher, object, CancellationToken, Task>> Cache = new();

    public static Task PublishAsync(IEventPublisher publisher, object payload, CancellationToken cancellationToken)
    {
        var type = payload.GetType();
        var invoker = Cache.GetOrAdd(type, BuildInvoker);
        return invoker(publisher, payload, cancellationToken);
    }

    private static Func<IEventPublisher, object, CancellationToken, Task> BuildInvoker(Type eventType)
    {
        var open = typeof(IEventPublisher).GetMethods()
            .Single(m => m is { Name: nameof(IEventPublisher.PublishAsync), IsGenericMethodDefinition: true }
                         && m.GetGenericArguments().Length == 1
                         && m.GetParameters().Length == 2);
        var closed = open.MakeGenericMethod(eventType);
        return (pub, obj, ct) => (Task)closed.Invoke(pub, [obj, ct])!;
    }
}
