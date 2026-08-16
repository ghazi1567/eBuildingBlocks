using System.Collections.Concurrent;
using System.Linq;
using eBuildingBlocks.Common.Outbox;

namespace eBuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// Invokes <see cref="IOutboxIntegrationPublisher.PublishAsync{T}"/> for a payload whose type is only known at runtime.
/// </summary>
internal static class OutboxPublisherDispatch
{
    private static readonly ConcurrentDictionary<Type, Func<IOutboxIntegrationPublisher, object, CancellationToken, Task>> Cache = new();

    public static Task PublishAsync(IOutboxIntegrationPublisher publisher, object payload, CancellationToken cancellationToken)
    {
        var type = payload.GetType();
        var invoker = Cache.GetOrAdd(type, BuildInvoker);
        return invoker(publisher, payload, cancellationToken);
    }

    private static Func<IOutboxIntegrationPublisher, object, CancellationToken, Task> BuildInvoker(Type eventType)
    {
        var open = typeof(IOutboxIntegrationPublisher).GetMethods()
            .Single(m => m is { Name: nameof(IOutboxIntegrationPublisher.PublishAsync), IsGenericMethodDefinition: true }
                         && m.GetGenericArguments().Length == 1
                         && m.GetParameters().Length == 2);
        var closed = open.MakeGenericMethod(eventType);
        return (pub, obj, ct) => (Task)closed.Invoke(pub, [obj, ct])!;
    }
}
