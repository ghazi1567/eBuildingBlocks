using System.Diagnostics.CodeAnalysis;
using eBuildingBlocks.Domain.Models;

namespace eBuildingBlocks.Application.Eventing;

/// <summary>
/// Thread-safe registry populated at application startup. Register all outbox event types before processing messages.
/// </summary>
public sealed class EventTypeRegistry : IEventTypeRegistry
{
    private readonly object _sync = new();
    private readonly Dictionary<string, Type> _nameToType = new(StringComparer.Ordinal);
    private readonly Dictionary<Type, string> _typeToName = new();

    /// <inheritdoc />
    public void Register<TEvent>(string eventName) where TEvent : IDomainEvent
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        var type = typeof(TEvent);

        lock (_sync)
        {
            if (_typeToName.TryGetValue(type, out var existingForType))
            {
                if (string.Equals(existingForType, eventName, StringComparison.Ordinal))
                    return;
                throw new InvalidOperationException(
                    $"Type '{type.FullName}' is already registered as '{existingForType}'.");
            }

            if (_nameToType.TryGetValue(eventName, out var existingForName) && existingForName != type)
                throw new InvalidOperationException(
                    $"Event name '{eventName}' is already bound to '{existingForName.FullName}'.");

            _nameToType[eventName] = type;
            _typeToName[type] = eventName;
        }
    }

    /// <inheritdoc />
    public Type? Resolve(string eventName)
    {
        if (string.IsNullOrWhiteSpace(eventName))
            return null;

        lock (_sync)
            return _nameToType.TryGetValue(eventName, out var t) ? t : null;
    }

    /// <inheritdoc />
    public bool TryGetEventName(Type eventType, [NotNullWhen(true)] out string? eventName)
    {
        ArgumentNullException.ThrowIfNull(eventType);

        lock (_sync)
            return _typeToName.TryGetValue(eventType, out eventName);
    }
}
