# Reliable event publishing with the transactional outbox in 10 minutes

The problem this solves: if you save a database change and then separately publish a message ("dual write"), a crash between the two leaves you either with an unpublished event or a phantom one. The transactional outbox writes the event to the same database transaction as your data, then a background processor publishes it — so it's exactly as reliable as your database's commit. For the full reference (schema, retry/backoff config, troubleshooting), see the [outbox deep dive](../HOSTING_APP_OUTBOX.md).

## 1. Add packages

```bash
dotnet add package eBuildingBlocks.Application
dotnet add package eBuildingBlocks.Infrastructure
dotnet add package eBuildingBlocks.EventBus   # if publishing via MassTransit
```

## 2. Define and raise a domain event

```csharp
public record OrderPlacedEvent(Guid OrderId, decimal Total, Guid TenantId) : BaseDomainEvent(TenantId);

public class Order : AuditableEntity<Guid>
{
    public decimal Total { get; set; }

    public void Place()
    {
        AddDomainEvent(new OrderPlacedEvent(Id, Total, TenantId: default));
    }
}
```

## 3. Wire up the outbox on your `DbContext`

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.ConfigureDomainOutbox(); // adds the OutboxMessages table to the model
}
```

```csharp
builder.Services.AddDomainOutboxInterceptor();
builder.Services.AddDbContext<YourDbContext>((sp, options) =>
{
    options.UseSqlServer(connectionString);
    options.AddInterceptors(sp.GetRequiredService<DomainOutboxSaveChangesInterceptor>());
});
```

Then add/apply an EF Core migration so the `OutboxMessages` table exists.

## 4. Register event type names and the publisher

Outbox rows are keyed by a stable string name, not the CLR type — this is what lets you rename/refactor the C# type later without breaking already-queued or historical rows.

```csharp
builder.Services.AddEventTypeRegistry();
builder.Services.AddSingleton<IHostedService, OutboxEventCatalogStartup>(); // registers every event name once at startup

builder.Services.AddEventBus(builder.Configuration); // registers IEventPublisher -> MassTransit
```

```csharp
public sealed class OutboxEventCatalogStartup(IEventTypeRegistry registry) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        registry.Register<OrderPlacedEvent>("sales.order.placed.v1");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
```

## 5. Start the background processor

```csharp
builder.Services.AddOutboxProcessor<YourDbContext>(builder.Configuration);
```

This polls for unprocessed rows, claims a batch safely across multiple instances (SQL Server `UPDLOCK`/`READPAST`), and publishes via `IEventPublisher`. Publishing is at-least-once, so downstream consumers must be idempotent.

## 6. Commit with the outbox path

```csharp
order.Place();
orders.Add(order);
await dbContext.SaveChangesWithTransactionalOutboxAsync(cancellationToken);
```

The event row is written in the same transaction as the `Order` insert. If `SaveChanges` fails, nothing is enqueued; if it succeeds, the processor will eventually publish it — even if the process crashes immediately after the commit.

> Don't mix this with `SaveChangesAndPublishDomainEventsInProcessAsync` for the same event flow — pick one path per event, or you risk double-handling. See the [deep dive's comparison table](../HOSTING_APP_OUTBOX.md#6-in-process-ieventbus-vs-transactional-outbox-choose-one-path).

## Next steps

- Multiple tenants publishing events? Combine this with the [multi-tenant SaaS quickstart](multi-tenant-saas-quickstart.md) — `IDomainEvent.TenantId` is required when multi-tenancy is enabled.
- Watch for poison rows (`AttemptCount` at max) in monitoring — see the [troubleshooting table](../HOSTING_APP_OUTBOX.md#9-troubleshooting).
