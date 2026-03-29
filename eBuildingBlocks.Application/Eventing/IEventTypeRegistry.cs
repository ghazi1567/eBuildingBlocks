using System.Diagnostics.CodeAnalysis;
using eBuildingBlocks.Domain.Models;

namespace eBuildingBlocks.Application.Eventing;

/// <summary>
/// Maps stable string keys (e.g. <c>user.created.v1</c>) to CLR types for domain events.
/// Used when writing and reading the transactional outbox so payloads stay decoupled from assembly-qualified names.
/// </summary>
public interface IEventTypeRegistry
{
    /// <summary>Registers a stable name for <typeparamref name="TEvent"/>.</summary>
    /// <exception cref="InvalidOperationException">The name or type is already registered differently.</exception>
    void Register<TEvent>(string eventName) where TEvent : IDomainEvent;

    /// <summary>Resolves the CLR type for an outbox key, or <c>null</c> if unknown.</summary>
    Type? Resolve(string eventName);

    /// <summary>Resolves the stable outbox key for a concrete domain event type (used when persisting outbox rows).</summary>
    bool TryGetEventName(Type eventType, [NotNullWhen(true)] out string? eventName);
}
