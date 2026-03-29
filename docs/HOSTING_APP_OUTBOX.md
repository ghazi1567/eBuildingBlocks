# Hosting app: transactional outbox & event type registry

This guide describes how to wire **eBuildingBlocks** in an ASP.NET Core (or generic host) application so that:

- Domain events are written to **`OutboxMessages`** in the **same database transaction** as your aggregates (via EF Core interceptor).
- A background worker **claims** rows safely under **multiple pods** (SQL Server locking hints) and publishes via **`IEventPublisher`** (e.g. MassTransit).
- Event payloads are keyed by **stable string names** (`user.created.v1`), not assembly-qualified type names.

---

## Prerequisites

| Requirement | Notes |
|-------------|--------|
| **SQL Server** | Outbox **claim** SQL uses `UPDLOCK`, `READPAST` (SQL Server–specific). |
| **EF Core** | Your app `DbContext` must include `OutboxMessage` in the model (see below). |
| **Packages** | Reference `eBuildingBlocks.Application`, `eBuildingBlocks.Infrastructure`, and (for publishing) `eBuildingBlocks.EventBus` or your own `IEventPublisher` implementation. |

---

## 1. Register every outbox event type (stable names)

Implementations of **`IDomainEvent`** that you persist to the outbox must be registered once with a **versioned, stable** key (do not rename casually; add a new key when the contract changes).

```csharp
using eBuildingBlocks.Application.DependencyInjection;
using eBuildingBlocks.Application.Eventing;

// In Program.cs — after builder.Services is available:

builder.Services.AddEventTypeRegistry();

// Option A: register in a dedicated hosted service that runs once at startup (recommended for many events)
// Option B: register after app.Build() using a scope (see section below)
```

Example registration:

```csharp
void RegisterOutboxEvents(IEventTypeRegistry registry)
{
    registry.Register<UserCreatedEvent>("identity.user.created.v1");
    registry.Register<OrderPlacedEvent>("sales.order.placed.v1");
}
```

**Rules:**

- One **unique** `eventName` per CLR type; one type per `eventName`.
- The **interceptor** calls `TryGetEventName` when saving; if a raised event type is **not** registered, **`SaveChanges` fails** with a clear exception (fail-fast).

---

## 2. Configure `DbContext`

### 2.1 Model

In **`OnModelCreating`**:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.ConfigureDomainOutbox(); // eBuildingBlocks.Infrastructure.Outbox
}
```

### 2.2 Interceptor + connection

Register the interceptor singleton and attach it to the same `DbContext` options your app uses:

```csharp
using eBuildingBlocks.Infrastructure.Outbox;

builder.Services.AddDomainOutboxInterceptor();

builder.Services.AddDbContext<YourDbContext>((sp, options) =>
{
    options.UseSqlServer(connectionString);
    options.AddInterceptors(sp.GetRequiredService<DomainOutboxSaveChangesInterceptor>());
});
```

**Registration order:** call **`AddEventTypeRegistry()`** before **`AddDomainOutboxInterceptor()`** is not strictly required by the container, but you must register **event types** before any `SaveChanges` that emits unregistered events.

**Committing work:** call **`SaveChangesAsync`**, **`IUnitOfWork.SaveChangesAsync`**, or **`SaveChangesWithTransactionalOutboxAsync`** on your `DbContext`. The interceptor enqueues outbox rows during `SaveChanges` and clears aggregate domain events only **after** a successful commit (`SavedChanges`), so a failed save leaves events on entities for retry.

---

## 3. Database migration

Add/update an EF Core migration so the **`OutboxMessages`** table matches **`OutboxMessage`** (including `EventName`, `PayloadJson`, `AttemptCount`, `LastError`, `LockedUntil`, `ProcessedAtUtc`, etc.).

If you are upgrading from an older schema that used **`EventType`** (assembly-qualified name), rename/migrate the column to **`EventName`** and backfill or re-publish as needed.

---

## 4. Integration publishing (`IEventPublisher`)

The outbox processor resolves **`IEventPublisher`** from a **scope** each batch. Register your broker integration **before** or alongside the processor.

Example (library-provided MassTransit wiring):

```csharp
using BuildingBlocks.EventBus.Events;

builder.Services.AddEventBus(builder.Configuration); // registers IEventPublisher → EventPublisher
```

Ensure RabbitMQ (or your transport) settings are valid for your environment.

---

## 5. Outbox background processor (hosted service)

```csharp
builder.Services.AddOutboxProcessor<YourDbContext>(builder.Configuration);
```

Optional **`appsettings.json`** (section name: **`OutboxProcessor`**):

```json
{
  "OutboxProcessor": {
    "PollInterval": "00:00:05",
    "BatchSize": 20,
    "MaxAttempts": 10,
    "LeaseDuration": "00:05:00",
    "BaseBackoffSeconds": 15,
    "MaxBackoffSeconds": 3600,
    "LastErrorMaxLength": 4000
  }
}
```

**Behavior (summary):**

- Polls on an interval; **claims** a batch with **SQL Server** `UPDLOCK` / `READPAST` and sets **`LockedUntil`**.
- Deserializes JSON using **`IEventTypeRegistry.Resolve(EventName)`**.
- Unknown **`EventName`**: logs **critical**, marks row as **poison** (`AttemptCount = MaxAttempts`), does **not** crash the host.
- Publish is **at-least-once**; downstream consumers must be **idempotent**.

---

## 6. In-process `IEventBus` vs transactional outbox (choose one path)

Do **not** mix duplicate delivery for the same logical event:

| Goal | API | Outbox interceptor |
|------|-----|-------------------|
| **Transactional outbox** (processor publishes later) | `SaveChangesAsync`, `IUnitOfWork.SaveChangesAsync`, or **`SaveChangesWithTransactionalOutboxAsync`** | **Registered** — enqueues rows; clears domain events **after** successful save |
| **In-process only** (no outbox rows for this flow) | **`SaveChangesAndPublishDomainEventsInProcessAsync`** | **Registered** — enqueue is **suppressed** for that call via `AsyncLocal`; handlers run from a pre-save snapshot after commit |

**`SaveChangesAndPublishEventsAsync`** is **obsolete**; it behaves like **`SaveChangesAndPublishDomainEventsInProcessAsync`** (outbox suppressed for that call). Apps that used the old API **with** the outbox registered were at risk of **double handlers** (in-process + processor); migrate explicitly to one of the two rows above.

---


## 7. Suggested startup pattern (register event names cleanly)

Avoid building a second root provider only to register names. Prefer a small **`IHostedService`** that runs once:

```csharp
public sealed class OutboxEventCatalogStartup(IEventTypeRegistry registry) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        registry.Register<UserCreatedEvent>("identity.user.created.v1");
        // …all outbox events
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

// builder.Services.AddSingleton<IHostedService, OutboxEventCatalogStartup>();
// Register before or with other hosted services; runs once at startup.
```

Alternatively, register from a **composition root** module that runs synchronously during startup **after** `services.AddEventTypeRegistry()`.

---

## 8. Checklist

- [ ] `AddEventTypeRegistry()`
- [ ] Every emitted **`IDomainEvent`** type used with the outbox has **`Register<T>(eventName)`**
- [ ] `modelBuilder.ConfigureDomainOutbox()`
- [ ] `AddDomainOutboxInterceptor()` + `AddInterceptors(...)` on `DbContext`
- [ ] SQL Server connection for that `DbContext`
- [ ] `AddOutboxProcessor<YourDbContext>(configuration)` (and config section if used)
- [ ] `IEventPublisher` registered (e.g. `AddEventBus`)
- [ ] EF migration applied in each environment
- [ ] Monitoring/alerts for poison rows (`AttemptCount` at max, `LastError` populated)

---

## 9. Troubleshooting

| Symptom | What to check |
|---------|----------------|
| `SaveChanges` throws: type not registered | Call `Register<YourEvent>("name")` for that CLR type. |
| Outbox rows never processed | `IEventPublisher`, SQL connectivity, processor hosted service running, `MaxAttempts` not already exceeded. |
| Unknown `EventName` / poison | Wrong or missing registry entry; legacy rows with wrong column; fix data or add mapping. |
| Duplicate processing across pods | Claim SQL uses SQL Server; ensure **one** logical database and correct provider. |

For library types and namespaces, see XML docs on **`DomainOutboxInfrastructureExtensions`**, **`OutboxProcessorBackgroundService<TDbContext>`**, and **`IEventTypeRegistry`**.
