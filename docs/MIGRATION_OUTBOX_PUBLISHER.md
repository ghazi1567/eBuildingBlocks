# Migration: Outbox publisher resolution

## What changed
`eBuildingBlocks.Infrastructure`'s outbox processor now prefers
`eBuildingBlocks.Common.Outbox.IOutboxIntegrationPublisher` over the MassTransit-specific
`IEventPublisher` from `eBuildingBlocks.EventBus`. This lets `Infrastructure` avoid a
hard dependency on MassTransit/RabbitMQ for consumers who don't use the outbox +
message broker combination.

## Do I need to do anything right now?
**No.** If you currently call `services.AddIntegrationMassTransit(...)` from
`eBuildingBlocks.EventBus`, the new interface is registered automatically as of
`eBuildingBlocks.EventBus` 3.x. Your app keeps working with zero code changes.

If you're on an older `eBuildingBlocks.EventBus` version that predates this change,
the outbox processor still falls back to `IEventPublisher` automatically — you'll see
a one-time warning in your logs, nothing breaks.

## What should I do to remove the warning / prepare for the next major version?
1. Upgrade `eBuildingBlocks.EventBus` to >= 3.x (if not already).
2. No further action needed — `AddIntegrationMassTransit` registers
   `IOutboxIntegrationPublisher` for you.

## If you're using a broker other than MassTransit, or a custom `IEventPublisher`
Implement `IOutboxIntegrationPublisher` directly and register it:
```csharp
services.AddScoped<IOutboxIntegrationPublisher, MyCustomOutboxPublisher>();
```

## Timeline
- **Now (eBuildingBlocks.Infrastructure 3.x):** both paths work. Legacy path logs a
  warning once per process.
- **Next major version (eBuildingBlocks.Infrastructure 4.0, separately scheduled):**
  the `IEventPublisher` fallback and the `EventBus` project reference are removed.
  Apps that have not registered `IOutboxIntegrationPublisher` by then will get a
  clear startup exception with the same guidance instead of a warning.
