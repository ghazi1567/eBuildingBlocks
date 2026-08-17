# Migration: Outbox publisher resolution

## Breaking in 4.0
If you're upgrading directly from `eBuildingBlocks.Infrastructure` 3.x or earlier and
you use the outbox processor, you must have `IOutboxIntegrationPublisher` registered
**before** calling `AddOutboxProcessor<TDbContext>()` — either via
`eBuildingBlocks.EventBus`'s `AddIntegrationMassTransit(...)` (>= 3.x, which registers
it automatically) or your own implementation. Without it, the app will fail to start
with a clear exception rather than degrade silently.

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
- **eBuildingBlocks.Infrastructure 3.x (superseded):** both paths worked. Legacy
  path logged a warning once per process.
- **eBuildingBlocks.Infrastructure 4.0 (current):** the `IEventPublisher` fallback
  and the `EventBus` project reference have been removed. Apps that have not
  registered `IOutboxIntegrationPublisher` will get a startup
  `InvalidOperationException` naming the missing service instead of a warning.
