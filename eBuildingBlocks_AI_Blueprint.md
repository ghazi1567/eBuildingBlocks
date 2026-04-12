# eBuildingBlocks AI Blueprint

> **Purpose:** A self-contained reference for any AI instance writing code against this framework.
> Every type name, namespace, method signature, and constraint in this document was verified
> directly against the source code. Do not invent types or signatures not listed here.
>
> **Framework version:** .NET 10 · Branch `latest-dotnet-10`
> **Full analysis:** [`docs/ARCHITECTURE_ANALYSIS.md`](docs/ARCHITECTURE_ANALYSIS.md)
> **Open tasks:** [`TODO.md`](TODO.md)

---

## Table of Contents

1. [Core Philosophy](#1-core-philosophy)
2. [Namespace & Dependency Map](#2-namespace--dependency-map)
3. [Critical Namespace Warning](#3-critical-namespace-warning)
4. [The Golden Thread — Domain Layer](#4-the-golden-thread--domain-layer)
5. [The Golden Thread — Application Layer](#5-the-golden-thread--application-layer)
6. [The Golden Thread — Infrastructure Layer](#6-the-golden-thread--infrastructure-layer)
7. [The Golden Thread — EventBus & Messaging](#7-the-golden-thread--eventbus--messaging)
8. [Service Registration Cheat Sheet](#8-service-registration-cheat-sheet)
9. [Configuration Reference](#9-configuration-reference)
10. [Cross-Cutting Patterns](#10-cross-cutting-patterns)
11. [Specification Pattern Cookbook](#11-specification-pattern-cookbook)
12. [Strict Constraints — Never Do](#12-strict-constraints--never-do)
13. [Known Gaps & Workarounds](#13-known-gaps--workarounds)

---

## 1. Core Philosophy

| Principle | How this framework applies it |
|---|---|
| **Clean Architecture** | Dependency direction: Domain ← Application ← Infrastructure ← API. Application and Domain have **zero** framework dependencies. |
| **DDD** | Aggregates own domain events. Repositories exist only at the aggregate root boundary (`IAggregateRoot` marker — not yet enforced by constraint, see §13). |
| **CQRS (lightweight)** | Read paths use `IReadRepository` + `ISpecification`. Write paths use `IRepository` + `IUnitOfWork`. No MediatR is used. |
| **Event-Driven** | Two distinct buses: `IEventBus` (in-process domain events, same transaction) and `IEventPublisher` (cross-service integration events via MassTransit/RabbitMQ). |
| **Transactional Outbox** | Domain events are persisted in the same DB transaction as aggregate changes. A background service publishes them to the broker. Prevents dual-write. |
| **Multi-Tenancy** | Global EF query filters on all `ITenantEntity` types. Tenant resolved from JWT claim → HTTP header → ambient scope. |
| **Feature Flags** | All framework middleware and services are gated by `Features:*` keys in `appsettings.json`. Unused features have zero cost. |

> **No MediatR.** This framework does not use MediatR. Command/query handlers are plain classes
> injected with `IRepository` and `IUnitOfWork`. There are no pipeline behaviours.

---

## 2. Namespace & Dependency Map

```
eBuildingBlocks.Common          Shared utilities: GuidGenerator, CustomClaimTypes,
                                MultiTenancyOptions, FeatureGate

eBuildingBlocks.Domain          DDD core: base entities, domain events, IRepository/IReadRepository
                                interfaces, IUnitOfWork, ISpecification, ICurrentUser, IAggregateRoot

eBuildingBlocks.Application     Use-case contracts: exception types, ResponseModel<T>,
                                IEventBus, IEventHandler<T>, IEventTypeRegistry, middlewares

eBuildingBlocks.Infrastructure  EF Core implementations: Repository, UnitOfWork,
                                AuditSaveChangesInterceptor, InProcessEventBus,
                                TenantAwareDbContext, transactional outbox, TenantResolver

eBuildingBlocks.EventBus        MassTransit/RabbitMQ integration: IEventPublisher,
                                IntegrationEvent base class, AddIntegrationMassTransit()

eBuildingBlocks.API             ASP.NET Core host: BaseRegister(), BaseAppUse(),
                                BaseController, feature-gated middleware pipeline

eBuildingBlocks.SMPP            SMS/SMPP protocol support (independent, no cross-deps)
```

**Dependency graph (arrows = "depends on"):**
```
Common ──► (nobody)
Domain ──► Common
Application ──► Domain, Common
EventBus ──► Common
Infrastructure ──► Application, Domain, Common, EventBus
API ──► Infrastructure, Application, Common
```

---

## 3. Critical Namespace Warning

**The `EventBus` and `API` projects use a different namespace prefix than all other projects.**

| Project | Root namespace |
|---|---|
| `eBuildingBlocks.Domain` | `eBuildingBlocks.Domain.*` |
| `eBuildingBlocks.Application` | `eBuildingBlocks.Application.*` |
| `eBuildingBlocks.Infrastructure` | `eBuildingBlocks.Infrastructure.*` |
| `eBuildingBlocks.Common` | `eBuildingBlocks.Common.*` |
| `eBuildingBlocks.EventBus` | **`BuildingBlocks.EventBus.*`** ← no 'e' |
| `eBuildingBlocks.API` (Startup) | **`BuildingBlocks.API.Startup`** ← no 'e' |
| `eBuildingBlocks.API` (Controllers) | `eBuildingBlocks.API.Controllers` |

Always verify `using` statements. When adding a `BaseRegister` or `BaseAppUse` call, the correct
using is `BuildingBlocks.API.Startup`, not `eBuildingBlocks.API.Startup`.

---

## 4. The Golden Thread — Domain Layer

### 4.1 Entity Hierarchy

Inherit in this order depending on what your entity needs:

```csharp
// ── Minimal entity (no audit, no tenant) ─────────────────────────────────
// Namespace: eBuildingBlocks.Domain.Models
public abstract class BaseEntity<TKey> : IEquatable<BaseEntity<TKey>>, IEntity, IHasDomainEvents
    where TKey : IEquatable<TKey>
{
    public TKey Id { get; set; } = default!;

    [Timestamp]                              // EF Core optimistic concurrency
    public byte[]? RowVersion { get; protected set; }

    // Domain events – raise from aggregate methods, never from outside
    public IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    protected void AddDomainEvent(IDomainEvent @event);
    protected void RemoveDomainEvent(IDomainEvent @event);
    public void ClearDomainEvents();

    // Structural equality: Id + GetType(). Falls back to reference equality for un-persisted entities.
}

// ── Entity with audit fields ──────────────────────────────────────────────
// Namespace: eBuildingBlocks.Domain.Models
public abstract class AuditableEntity<TKey> : BaseEntity<TKey>
    where TKey : IEquatable<TKey>
{
    public DateTimeOffset CreatedOn { get; protected set; }
    public string? CreatedBy { get; protected set; }
    public DateTimeOffset? ModifiedOn { get; protected set; }
    public string? ModifiedBy { get; protected set; }
    public void SetCreated(string? userId, DateTimeOffset? when = null);
    public void SetModified(string? userId, DateTimeOffset? when = null);
}

// ── Tenant-scoped entity with audit ──────────────────────────────────────
// Namespace: eBuildingBlocks.Domain.Models
public abstract class TenantEntity<TKey> : AuditableEntity<TKey>, ITenantEntity
    where TKey : IEquatable<TKey>
{
    public Guid TenantId { get; set; } = default!;
}
```

**Which base to choose:**

| Scenario | Base class |
|---|---|
| Simple lookup / reference data, no audit, single tenant | `BaseEntity<TKey>` |
| Entity needs audit trail | `AuditableEntity<TKey>` |
| Entity is tenant-scoped (most aggregates) | `TenantEntity<TKey>` |

### 4.2 Aggregate Root Marker

```csharp
// Namespace: eBuildingBlocks.Domain.Interfaces
public interface IAggregateRoot { }
```

Tag every aggregate root with this interface. Repositories must only be created for aggregate roots.
*(Note: the constraint is not yet enforced by `IRepository<>` — see §13. Apply it by convention.)*

**Typical aggregate root pattern:**

```csharp
// Your project
public class Order : TenantEntity<Guid>, IAggregateRoot
{
    public string OrderNumber { get; private set; } = string.Empty;
    public decimal TotalAmount { get; private set; }

    private Order() { }  // EF Core constructor

    public static Order Create(string orderNumber, decimal total, Guid tenantId)
    {
        var order = new Order
        {
            Id = Guid.CreateVersion7(),
            OrderNumber = orderNumber,
            TotalAmount = total,
            TenantId = tenantId
        };
        order.AddDomainEvent(new OrderPlacedDomainEvent(order.Id, tenantId));
        return order;
    }
}
```

### 4.3 Domain Events

```csharp
// Interface (Namespace: eBuildingBlocks.Domain.Models)
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredAt { get; }   // ⚠ DateTime (not DateTimeOffset) — known gap, see TODO O-5
    Guid TenantId { get; }
}

// Base class (Namespace: eBuildingBlocks.Domain.Models)
public abstract class BaseDomainEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();      // ⚠ not UUID v7 — known gap, see TODO O-4
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public Guid TenantId { get; set; }

    protected BaseDomainEvent() { }
    protected BaseDomainEvent(Guid tenantId) { TenantId = tenantId; }
}
```

**Defining a domain event (your project):**

```csharp
// Prefer record for immutability and value semantics
public sealed record OrderPlacedDomainEvent : BaseDomainEvent
{
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;

    // Parameterless ctor required for JSON deserialization in the outbox
    public OrderPlacedDomainEvent() { }

    public OrderPlacedDomainEvent(Guid orderId, Guid tenantId) : base(tenantId)
    {
        OrderId = orderId;
    }
}
```

### 4.4 Repository & Unit of Work Interfaces

```csharp
// Namespace: eBuildingBlocks.Domain.Interfaces

public interface IReadRepository<TEntity, TKey> where TEntity : class
{
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default);
    Task<TEntity?> FirstOrDefaultAsync(ISpecification<TEntity> spec, CancellationToken ct = default);
    Task<TEntity?> SingleOrDefaultAsync(ISpecification<TEntity> spec, CancellationToken ct = default);
    Task<IReadOnlyList<TEntity>> ListAsync(ISpecification<TEntity> spec, CancellationToken ct = default);
    Task<IReadOnlyList<TEntity>> ListAllAsync(CancellationToken ct = default);
    Task<int> CountAsync(ISpecification<TEntity> spec, CancellationToken ct = default);
    Task<bool> AnyAsync(ISpecification<TEntity> spec, CancellationToken ct = default);
}

public interface IRepository<TEntity, TKey> : IReadRepository<TEntity, TKey>
    where TEntity : class
{
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
    // ⚠ SaveChangesAsync is NOT here. Use IUnitOfWork.
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

### 4.5 Specifications Interface

```csharp
// Namespace: eBuildingBlocks.Domain.Interfaces
public interface ISpecification<T> where T : class
{
    Expression<Func<T, bool>>? Criteria { get; }
    IReadOnlyList<string> IncludeStrings { get; }   // dot-separated paths: "Category" or "Order.Lines"
    Expression<Func<T, object>>? OrderBy { get; }
    Expression<Func<T, object>>? OrderByDescending { get; }
    int? Take { get; }
    int? Skip { get; }
    bool AsNoTracking { get; }        // default: true in SpecificationBase<T>
    bool IgnoreQueryFilters { get; }  // gated by IQueryFilterBypassEvaluator — always false by default
}
```

### 4.6 Current User & Tenant

```csharp
// Namespace: eBuildingBlocks.Domain.Interfaces
public interface ICurrentUser
{
    string? UserId { get; }
    string? UserEmail { get; }
    string? IPAddress { get; }
    string? UserName { get; }
    Guid TenantId { get; }
    string UserAgent { get; }
}

public interface ITenantScope
{
    IDisposable Begin(Guid tenantId);    // use in background jobs / tests
    Guid OverrideTenantId { get; }
}
```

---

## 5. The Golden Thread — Application Layer

### 5.1 No MediatR — Handler Pattern

This framework does **not** use MediatR. Handlers are plain classes:

```csharp
// Pattern: inject IRepository + IUnitOfWork into your handler/service
public class PlaceOrderCommandHandler
{
    private readonly IRepository<Order, Guid> _orders;
    private readonly IUnitOfWork _uow;

    public PlaceOrderCommandHandler(
        IRepository<Order, Guid> orders,
        IUnitOfWork uow)
    {
        _orders = orders;
        _uow = uow;
    }

    public async Task<ResponseModel<Guid>> HandleAsync(
        PlaceOrderCommand command,
        CancellationToken ct = default)
    {
        var order = Order.Create(command.OrderNumber, command.Total, command.TenantId);
        await _orders.AddAsync(order, ct);
        await _uow.SaveChangesAsync(ct);   // ← always via IUnitOfWork, never via repository
        return ResponseModel<Guid>.Created(order.Id, "Order placed successfully.");
    }
}
```

### 5.2 Domain Event Handler Pattern

```csharp
// Namespace: eBuildingBlocks.Application.Events
public interface IEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}

// Implementation (your project)
public class OrderPlacedHandler : IEventHandler<OrderPlacedDomainEvent>
{
    public async Task HandleAsync(OrderPlacedDomainEvent @event, CancellationToken ct)
    {
        // Side-effects: send email, update read-model, etc.
        // Must be idempotent. Execution order across multiple handlers is non-deterministic.
    }
}
```

Register handlers:
```csharp
services.AddScoped<IEventHandler<OrderPlacedDomainEvent>, OrderPlacedHandler>();
```

### 5.3 Exception Types

All in namespace `eBuildingBlocks.Application.Exceptions`:

| Class | HTTP mapping | Constructor |
|---|---|---|
| `BadRequestException` | 400 | `(string error, Dictionary<string, string[]> errors)` |
| `UnauthorizedException` | 401 | `(string? error)` |
| `ForbiddenException` | 403 | `(string? error)` |
| `NotFoundException` | 404 | `(string? error)` |
| `MethodNotAllowedException` | 405 | `(string? error)` |
| `ConflictException` | 409 | `(string? error)` |
| `TooManyRequestException` | 429 | `(string? error)` |
| `NotImplementedException` | 501 | `(string? error)` |

Domain-specific: `TenantResolutionException` (namespace: `eBuildingBlocks.Domain.Exceptions`) → maps to 400.

Throw them anywhere in Application or Domain layers. `GlobalExceptionHandlerMiddleware` catches and converts them automatically.

### 5.4 Response Model

```csharp
// Namespace: eBuildingBlocks.Application.Features

// Non-generic (operations with no data payload)
ResponseModel.Ok(string? message = null)
ResponseModel.Fail(string message, HttpStatusCode status = HttpStatusCode.BadRequest)
ResponseModel.Fail(IEnumerable<string> messages, HttpStatusCode status)
ResponseModel.ValidationFail(Dictionary<string, string[]> errors, string? message = null)

// Generic (operations returning data)
ResponseModel<T>.Ok(T data, string? message = null)
ResponseModel<T>.Created(T data, string? message = null)
ResponseModel<T>.Fail(string message, HttpStatusCode status = HttpStatusCode.BadRequest)
ResponseModel<T>.ValidationFail(Dictionary<string, string[]> errors, string? message = null)

// Fluent (chaining)
model.WithData(value)
model.WithSuccess(message)
model.WithError(message, status?)
```

Properties on every `ResponseModel`:
```
bool Success
HttpStatusCode StatusCode
string? Message          // first error or first success, auto-resolved
List<string> Successes
List<string> Errors
Dictionary<string, string[]> ValidationErrors
T? Data                  // ResponseModel<T> only
```

### 5.5 IEventTypeRegistry

Required for the transactional outbox. Register every domain event type with a stable string name at startup.

```csharp
// Namespace: eBuildingBlocks.Application.Eventing
public interface IEventTypeRegistry
{
    void Register<TEvent>(string eventName) where TEvent : IDomainEvent;
    Type? Resolve(string eventName);
    bool TryGetEventName(Type eventType, out string? eventName);
}

// Registration at startup (Program.cs):
var registry = app.Services.GetRequiredService<IEventTypeRegistry>();
registry.Register<OrderPlacedDomainEvent>("order.placed.v1");
// Use stable, human-readable keys. Never use assembly-qualified names.
```

### 5.6 Domain Event Bus Abstraction

```csharp
// Namespace: eBuildingBlocks.Application.Events
public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
        where TEvent : IDomainEvent;

    // Default interface method — dispatches by runtime type (cached reflection):
    Task PublishAsync(IDomainEvent @event, CancellationToken ct = default);
}
```

---

## 6. The Golden Thread — Infrastructure Layer

### 6.1 `Repository<TEntity, TKey, TDbContext>`

```csharp
// Namespace: eBuildingBlocks.Infrastructure.Implementations
// Full signature:
public class Repository<TEntity, TKey, TDbContext>(TDbContext dbContext)
    : UnitOfWork<TDbContext>(dbContext),
      IRepository<TEntity, TKey>,
      IEfQueryableRepository<TEntity, TKey>
    where TEntity : class, IEntity
    where TDbContext : DbContext
```

Register a repository for your aggregate root:
```csharp
// In Program.cs / extension method:
services.AddScoped<IRepository<Order, Guid>>(sp =>
    new Repository<Order, Guid, YourDbContext>(
        sp.GetRequiredService<YourDbContext>()));

// Or define a typed repository class:
public class OrderRepository(YourDbContext db)
    : Repository<Order, Guid, YourDbContext>(db), IOrderRepository { }

services.AddScoped<IOrderRepository, OrderRepository>();
```

`GetByIdAsync` is tenant-safe: for `ITenantEntity` types it uses a filtered LINQ query rather than `FindAsync` (which bypasses global query filters).

### 6.2 Unit of Work

```csharp
// Namespace: eBuildingBlocks.Infrastructure.Implementations
// Thin adapter — wraps DbContext.SaveChangesAsync
public sealed class DbContextUnitOfWork<TDbContext>(TDbContext context) : IUnitOfWork
    where TDbContext : DbContext
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

Register:
```csharp
services.AddDbContextUnitOfWork<YourDbContext>();
// Extension from eBuildingBlocks.Infrastructure.Extensions.UnitOfWorkServiceCollectionExtensions
```

**Always call `IUnitOfWork.SaveChangesAsync` to commit. Never call it on a repository.**

### 6.3 DbContext — `TenantAwareDbContext`

```csharp
// Namespace: eBuildingBlocks.Infrastructure.Data
public abstract class TenantAwareDbContext : DbContext
{
    protected TenantAwareDbContext(
        DbContextOptions options,
        ICurrentUser currentUser,
        IOptions<MultiTenancyOptions> multiTenancy) : base(options) { }

    // Automatically calls ApplyRuntimeTenantQueryFilters(_currentUser)
    // and ApplyTenantEntityIndexes() in OnModelCreating when MT is enabled.
    protected override void OnModelCreating(ModelBuilder modelBuilder) { ... }
}
```

Your DbContext should inherit from this when multi-tenancy is enabled:
```csharp
public class YourDbContext(
    DbContextOptions<YourDbContext> options,
    ICurrentUser currentUser,
    IOptions<MultiTenancyOptions> multiTenancy)
    : TenantAwareDbContext(options, currentUser, multiTenancy)
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);    // ← must call base — sets tenant filters
        modelBuilder.ConfigureDomainOutbox();  // ← if using outbox
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(YourDbContext).Assembly);
    }
}
```

### 6.4 `AuditSaveChangesInterceptor`

Wires automatically when registered. Stamps `CreatedOn/By` and `ModifiedOn/By` on `AuditableEntity<Guid>` instances and writes `AuditLog` rows in the same transaction.

```csharp
// Register:
services.AddScoped<AuditSaveChangesInterceptor>();

// Add to DbContext options:
services.AddDbContext<YourDbContext>((sp, options) =>
{
    options.UseSqlServer(connectionString)
           .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
});
```

⚠️ **Known gap:** Only stamps entities with `Guid` primary keys. Non-Guid keyed entities are silently skipped. See TODO P-2.

### 6.5 Transactional Outbox — Registration

```csharp
// Step 1: Register IEventTypeRegistry
services.AddEventTypeRegistry();
// from eBuildingBlocks.Application.DependencyInjection

// Step 2: Register the save-changes interceptor (singleton)
services.AddDomainOutboxInterceptor();
// from eBuildingBlocks.Infrastructure.Outbox.DomainOutboxInfrastructureExtensions

// Step 3: Add the interceptor to DbContext options
services.AddDbContext<YourDbContext>((sp, options) =>
{
    options.UseSqlServer(connectionString)
           .AddInterceptors(sp.GetRequiredService<DomainOutboxSaveChangesInterceptor>());
});

// Step 4: Register the background processor
services.AddOutboxProcessor<YourDbContext>(configuration);
// Reads config section "OutboxProcessor"

// Step 5: Map OutboxMessages table in OnModelCreating
modelBuilder.ConfigureDomainOutbox();

// Step 6: Register each domain event type at startup
var registry = app.Services.GetRequiredService<IEventTypeRegistry>();
registry.Register<OrderPlacedDomainEvent>("order.placed.v1");
```

### 6.6 Transactional Outbox — Save Patterns

```csharp
// ── Option A: Outbox path (recommended for reliability) ──────────────────
// DomainOutboxSaveChangesInterceptor handles enqueuing automatically.
await _uow.SaveChangesAsync(ct);
// or equivalently:
await dbContext.SaveChangesWithTransactionalOutboxAsync(ct);

// ── Option B: In-process path (no outbox, same-process only) ─────────────
// Collects domain events → saves → clears → publishes in-process.
// Suppresses outbox interceptor for this call automatically.
await dbContext.SaveChangesAndPublishDomainEventsInProcessAsync(eventBus, ct);
// from eBuildingBlocks.Infrastructure.Extensions.RepositoryExtensions

// ⚠ Never mix both for the same aggregate in the same request.
```

### 6.7 EF-Specific Queryable Repository (read-side escape hatch)

```csharp
// Namespace: eBuildingBlocks.Infrastructure.Data
public interface IEfQueryableRepository<TEntity, TKey> where TEntity : class
{
    IQueryable<TEntity> Query();   // AsNoTracking by default
}

// Cast from domain interface:
var efRepo = _orders.AsEfQueryable();
// from eBuildingBlocks.Infrastructure.Extensions.EfRepositoryQueryableExtensions
// Returns null if the implementation does not support IQueryable (safe to null-check)

// Use only in Application handlers when ISpecification is insufficient.
// Never use IQueryable<T> across layer boundaries.
```

---

## 7. The Golden Thread — EventBus & Messaging

### 7.1 Integration Event Base Class

```csharp
// Namespace: BuildingBlocks.EventBus.Contracts  ← note: no 'e' prefix
public class IntegrationEvent
{
    public Guid EventId { get; set; } = Guid.CreateVersion7();
    public Guid TenantId { get; set; }          // required when MT is enabled
    public EventType EventType { get; set; }    // enum: BuildingBlocks.EventBus.Enums
    public Guid CorrelationId { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public object Payload { get; set; }         // ⚠ untyped — known gap, see TODO S-5
}
```

Define concrete integration events:
```csharp
// Your project
public class OrderPlacedIntegrationEvent : IntegrationEvent
{
    // EventType identifies the event for routing / consumers
    public OrderPlacedIntegrationEvent(Guid orderId, Guid tenantId)
    {
        EventType = EventType.OrderPlaced;
        TenantId = tenantId;
        Payload = new { OrderId = orderId };
    }
}
```

### 7.2 Event Publisher (cross-service)

```csharp
// Namespace: BuildingBlocks.EventBus.Events  ← no 'e' prefix
public interface IEventPublisher
{
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class;
    Task PublishAsync(IntegrationEvent @event, CancellationToken cancellationToken = default);
}
```

### 7.3 MassTransit Consumer Pattern

```csharp
// Your project
public class OrderPlacedMassTransitConsumer : IConsumer<OrderPlacedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<OrderPlacedIntegrationEvent> context)
    {
        var @event = context.Message;
        // Handle cross-service integration logic here
    }
}
```

### 7.4 Publishing from an Outbox Handler

The outbox processor calls `IEventPublisher.PublishAsync<T>` internally for each processed outbox row. You do not call `IEventPublisher` directly in normal command handlers — the outbox does it for you.

For direct (non-outbox) publish:
```csharp
// Inject IEventPublisher only in infrastructure-layer Hangfire jobs or consumers
await _publisher.PublishAsync(new OrderPlacedIntegrationEvent(orderId, tenantId), ct);
```

---

## 8. Service Registration Cheat Sheet

### 8.1 Full API Host `Program.cs`

```csharp
using BuildingBlocks.API.Startup;    // ← no 'e' prefix
using BuildingBlocks.EventBus.Events; // ← no 'e' prefix
using eBuildingBlocks.Infrastructure.Extensions;
using eBuildingBlocks.Infrastructure.Outbox;
using eBuildingBlocks.Application.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// ── 1. Framework core (controllers, versioning, OpenTelemetry, Hangfire, etc.) ──
builder.Services.BaseRegister(
    builder.Configuration,
    builder.Host,
    typeof(YourValidatorsAssemblyMarker).Assembly  // null if no FluentValidation
);

// ── 2. Your DbContext (include interceptors) ──────────────────────────────
builder.Services.AddSingleton<AuditSaveChangesInterceptor>();
builder.Services.AddSingleton<DomainOutboxSaveChangesInterceptor>();

builder.Services.AddDbContext<YourDbContext>((sp, opts) =>
    opts.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
        .AddInterceptors(
            sp.GetRequiredService<AuditSaveChangesInterceptor>(),
            sp.GetRequiredService<DomainOutboxSaveChangesInterceptor>()));

// ── 3. Unit of Work ───────────────────────────────────────────────────────
builder.Services.AddDbContextUnitOfWork<YourDbContext>();

// ── 4. Your repositories ──────────────────────────────────────────────────
builder.Services.AddScoped<IRepository<Order, Guid>>(sp =>
    new Repository<Order, Guid, YourDbContext>(sp.GetRequiredService<YourDbContext>()));

// ── 5. Domain event bus (in-process) ─────────────────────────────────────
builder.Services.AddInProcessEventBus(opts =>
    opts.FailureMode = EventHandlerFailureMode.FailFast);

// Register your handlers:
builder.Services.AddScoped<IEventHandler<OrderPlacedDomainEvent>, OrderPlacedHandler>();

// ── 6. Outbox ─────────────────────────────────────────────────────────────
builder.Services.AddEventTypeRegistry();
builder.Services.AddDomainOutboxInterceptor();       // already registered above via AddSingleton
builder.Services.AddOutboxProcessor<YourDbContext>(builder.Configuration);

// ── 7. Integration events (MassTransit / RabbitMQ) ───────────────────────
builder.Services.AddIntegrationMassTransit(
    builder.Configuration,
    typeof(YourConsumersAssemblyMarker).Assembly);

// ── 8. Your application handlers ─────────────────────────────────────────
builder.Services.AddScoped<PlaceOrderCommandHandler>();
builder.Services.AddScoped<GetOrderByIdQueryHandler>();

var app = builder.Build();

// ── 9. Middleware pipeline ────────────────────────────────────────────────
app.BaseAppUse(builder.Configuration);
app.MapControllers();

// ── 10. Register outbox event types ──────────────────────────────────────
var registry = app.Services.GetRequiredService<IEventTypeRegistry>();
registry.Register<OrderPlacedDomainEvent>("order.placed.v1");

app.Run();
```

### 8.2 Worker / Background Service Host

```csharp
// Worker hosts (no HTTP) use AddTenantContextForWorkers instead of BaseRegister
builder.Services.AddTenantContextForWorkers(builder.Configuration);
builder.Services.AddDbContextUnitOfWork<YourDbContext>();
builder.Services.AddEventTypeRegistry();
builder.Services.AddOutboxProcessor<YourDbContext>(builder.Configuration);
builder.Services.AddIntegrationMassTransit(builder.Configuration, typeof(YourConsumers).Assembly);
```

### 8.3 Extension Method Signatures Reference

| Method | Namespace | Notes |
|---|---|---|
| `BaseRegister(services, config, hostBuilder, assembly?)` | `BuildingBlocks.API.Startup` | All API infrastructure, feature-gated |
| `BaseAppUse(app, config)` | `BuildingBlocks.API.Startup` | Full middleware pipeline |
| `AddDbContextUnitOfWork<TDbContext>()` | `eBuildingBlocks.Infrastructure.Extensions` | Registers `IUnitOfWork` |
| `AddInProcessEventBus(configure?)` | `eBuildingBlocks.Infrastructure.Extensions` | Registers `IEventBus → InProcessEventBus` |
| `AddEventTypeRegistry()` | `eBuildingBlocks.Application.DependencyInjection` | Registers `IEventTypeRegistry` as singleton |
| `AddDomainOutboxInterceptor()` | `eBuildingBlocks.Infrastructure.Outbox` | Registers interceptor as singleton |
| `AddOutboxProcessor<TDbContext>(services, config?)` | `eBuildingBlocks.Infrastructure.Outbox` | Registers `BackgroundService` processor |
| `AddIntegrationMassTransit(services, config, assemblies)` | `BuildingBlocks.EventBus.Events` | MassTransit + RabbitMQ/in-memory |
| `AddTenantContextCore(services, config)` | `eBuildingBlocks.Infrastructure.Tenancy` | Called internally by `BaseRegister` |
| `AddTenantContextForWorkers(services, config)` | `eBuildingBlocks.Infrastructure.Tenancy` | Worker host alias for above |
| `AddTenantMemoryCache()` | `eBuildingBlocks.Infrastructure.Tenancy` | Requires `IMemoryCache` already registered |
| `ConfigureDomainOutbox(modelBuilder)` | `eBuildingBlocks.Infrastructure.Outbox` | Call in `OnModelCreating` |
| `ApplyRuntimeTenantQueryFilters(modelBuilder, currentUser)` | `eBuildingBlocks.Infrastructure.Extensions` | Called by `TenantAwareDbContext` automatically |
| `ApplyTenantEntityIndexes(modelBuilder)` | `eBuildingBlocks.Infrastructure.Extensions` | Called by `TenantAwareDbContext` automatically |

---

## 9. Configuration Reference

All features are gated by `appsettings.json` keys. Missing keys = feature disabled (safe default).

```jsonc
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=...;",
    "RedisConnection": "localhost:6379"           // only if Features:Redis enabled
  },

  "Features": {
    "Controllers": true,
    "ApiVersioning": {
      "Enabled": true,
      "DefaultVersion": "1.0"
    },
    "OpenTelemetry": {
      "Enabled": true,
      "Name": "YourServiceName",
      "OtlpUrl": "http://otel-collector:4317"
    },
    "MemoryCache": true,
    "Redis": false,
    "Swagger": {
      "Enabled": true,
      "Title": "Your API",
      "Version": "v1",
      "RoutePrefix": "scalar"
    },
    "Cors": {
      "Enabled": true,
      "Policy": "allowall"                        // ⚠ AllowAnyOrigin() — see TODO S-1
    },
    "Hangfire": {
      "Enabled": true,
      "UseMemoryStorage": true,                   // false = SQL Server
      "ConnectionStringName": "HangfireConnection",
      "DashboardPath": "/hangfire",
      "User": "your-admin-user",                  // ⚠ must set — default is insecure
      "Password": "your-secure-password"          // ⚠ must set — default is insecure
    },
    "Authorization": true,
    "Endpoints": {
      "MapControllers": true,
      "HealthChecks": {
        "Enabled": true,
        "Path": "/healthz"
      },
      "Metrics": {
        "Enabled": true,
        "Path": "/metrics"
      }
    },
    "Middlewares": {
      "Enabled": true,
      "GlobalException": true,
      "HttpResponse": true
    },
    "MultiTenancy": {
      "Enabled": true,
      "HeaderName": "x-tenant-id",
      "ClaimType": "tenant_id",                   // matches CustomClaimTypes.TenantId
      "ValidateHeaderAgainstClaims": true,
      "AllowDefaultTenantFallback": false,         // ⚠ dev only — never true in production
      "DefaultTenantId": ""
    },
    "FluentValidation": true
  },

  "RabbitMQSettings": {
    "HostName": "localhost",
    "Username": "guest",
    "Password": "guest"
  },

  "MassTransit": {
    "UseInMemory": true    // true = in-memory (dev), false = RabbitMQ (prod)
  },

  "OutboxProcessor": {
    "PollInterval": "00:00:05",      // TimeSpan — how often to poll
    "BatchSize": 20,
    "MaxAttempts": 10,
    "LeaseDuration": "00:05:00",
    "BaseBackoffSeconds": 15,
    "MaxBackoffSeconds": 3600,
    "LastErrorMaxLength": 4000
  }
}
```

---

## 10. Cross-Cutting Patterns

### 10.1 Controllers — `BaseController`

```csharp
// Namespace: eBuildingBlocks.API.Controllers
// Always inherit BaseController in API controllers
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class OrdersController(PlaceOrderCommandHandler handler) : BaseController
{
    [HttpPost]
    public async Task<IActionResult> Place([FromBody] PlaceOrderCommand command, CancellationToken ct)
    {
        var result = await handler.HandleAsync(command, ct);
        return ApiResult(result);    // maps ResponseModel.StatusCode → correct IActionResult
    }
}
```

`ApiResult(ResponseModel)` and `ApiResult<T>(ResponseModel<T>)` handle the full HTTP status mapping.
Never return raw `Ok(...)` / `BadRequest(...)` from controllers — always go through `ApiResult`.

### 10.2 Audit Logging

Automatic when `AuditSaveChangesInterceptor` is registered and wired to the DbContext.

- **What is logged:** All `Add`, `Modify`, `Delete` operations on any tracked entity.
- **Where:** `AuditLog` table in the same database, same transaction.
- **Data captured:** `TableName`, `Action`, `KeyValues`, `OldValues` (modified/deleted), `NewValues` (added/modified), `PerformedBy` (from `ICurrentUser.UserName`), `IPAddress`, `PerformedAt`.
- **Limitation:** Only stamps audit fields on `AuditableEntity<Guid>` (Guid PK only). Non-Guid keyed entities do not get `CreatedOn/By` populated automatically.

```csharp
// AuditLog model (eBuildingBlocks.Domain.Models) — stored by the interceptor, read by your code
public class AuditLog
{
    public string TableName { get; set; }
    public string Action { get; set; }       // "Added" | "Modified" | "Deleted"
    public string KeyValues { get; set; }    // JSON of primary key columns
    public string? OldValues { get; set; }   // JSON — only changed columns for Modified
    public string? NewValues { get; set; }   // JSON — only changed columns for Modified
    public string PerformedBy { get; set; }  // defaults to "System"
    public string IPAddress { get; set; }
    public DateTime PerformedAt { get; set; }
    // ⚠ No TenantId — known gap, see TODO S-3
}
```

### 10.3 Global Exception Handling

`GlobalExceptionHandlerMiddleware` is activated by `Features:Middlewares:GlobalException = true` (default on via `BaseAppUse`).

Throw any of the framework exceptions from any layer — they are automatically caught and converted:

```csharp
// In a handler:
var order = await _orders.GetByIdAsync(id, ct)
    ?? throw new NotFoundException($"Order {id} was not found.");

if (!order.CanBeCancelled())
    throw new BadRequestException("Order cannot be cancelled.", errors: []);

if (currentUser.TenantId != order.TenantId)
    throw new ForbiddenException("Access denied to this order.");
```

The middleware writes a JSON `ResponseModel` body. The `HttpResponseMiddleware` (also wired by default) additionally normalises 401/403/405 responses produced by the ASP.NET Core pipeline itself.

### 10.4 Multi-Tenancy in Background Jobs

```csharp
// Inject ITenantScope to override tenant for a job
public class MyHangfireJob(ITenantScope tenantScope, IRepository<Order, Guid> orders)
{
    public async Task ExecuteAsync(Guid tenantId, CancellationToken ct)
    {
        using (tenantScope.Begin(tenantId))    // AsyncLocal — restores on Dispose
        {
            // EF query filters and ICurrentUser.TenantId now return tenantId
            var orders = await _orders.ListAsync(new ActiveOrdersSpec(), ct);
        }
    }
}
```

### 10.5 Tenant-Scoped Memory Cache

```csharp
// Inject ITenantMemoryCache (not IMemoryCache) for automatic tenant key isolation
public class OrderSummaryService(ITenantMemoryCache cache)
{
    public async Task<OrderSummaryDto> GetAsync(Guid orderId)
    {
        var key = $"order-summary:{orderId}";
        return cache.Get<OrderSummaryDto>(key)
               ?? await RefreshCacheAsync(key);
    }
    // Keys are automatically prefixed with TenantId when MT is enabled
}
```

---

## 11. Specification Pattern Cookbook

### 11.1 Built-in Specifications

```csharp
// Namespace: eBuildingBlocks.Domain.Specifications

// By ID (with or without tracking)
var spec = new ByIdSpec<Order>(orderId);

// Paged list (page is 1-based, size defaults to 20 if <= 0)
var spec = new PagedSpec<Order>(page: 1, size: 20);
```

### 11.2 Custom Domain Specification (no EF types)

```csharp
// Use when: no typed Include expressions needed; plain LINQ criteria only
// Namespace: eBuildingBlocks.Domain.Specifications
public class ActiveOrdersByTenantSpec : SpecificationBase<Order>
{
    public ActiveOrdersByTenantSpec(Guid tenantId)
    {
        Criteria = o => o.TenantId == tenantId && o.Status == OrderStatus.Active;
        ApplyOrderByDescending(o => o.CreatedOn);
        ApplyPaging(skip: 0, take: 50);
        AddInclude("Lines");               // string dot-path include
        // AsNoTracking = true by default
    }
}
```

### 11.3 EF Specification (typed `Include` / `ThenInclude`)

```csharp
// Use when: you need typed Include expressions or ThenInclude chains
// Namespace: eBuildingBlocks.Infrastructure.Specifications
public class OrderWithLinesSpec : EfSpecification<Order>
{
    public OrderWithLinesSpec(Guid orderId)
    {
        Criteria = o => o.Id == orderId;
        AddInclude(o => o.Lines);                          // typed Include
        AddIncludeChain(q => q                             // ThenInclude chain
            .Include(o => o.Lines)
            .ThenInclude(l => l.Product));
        WithTracking();   // override default AsNoTracking for update scenarios
    }
}
```

### 11.4 Dynamic / Composite Filter

```csharp
// SpecificationBase<T>.CompositeFilter builds a LINQ predicate from runtime filter criteria
var filters = new List<FilterCriterion>
{
    new("Status", ComparisonOperator.Equal, "Active"),
    new("TotalAmount", ComparisonOperator.GreaterThan, 100)
};

var spec = new MyEntitySpec();
spec.CompositeFilter(filters, Logical.And);
```

### 11.5 Using Specifications with Repository

```csharp
// All repository read methods accept ISpecification<T>
var order = await _orders.FirstOrDefaultAsync(new ByIdSpec<Order>(orderId), ct);
var list   = await _orders.ListAsync(new ActiveOrdersByTenantSpec(tenantId), ct);
var count  = await _orders.CountAsync(new ActiveOrdersByTenantSpec(tenantId), ct);
var exists = await _orders.AnyAsync(new ActiveOrdersByTenantSpec(tenantId), ct);

// For paged grids — currently two round-trips (combined paged query is a TODO):
var items = await _orders.ListAsync(pagedSpec, ct);
var total = await _orders.CountAsync(unpaginatedSpec, ct);
```

---

## 12. Strict Constraints — Never Do

These rules are derived directly from the framework's architecture. Violating them will produce silent data bugs, security vulnerabilities, or broken multi-tenancy.

---

**[NEVER-01] Never call `SaveChangesAsync` on a repository.**

```csharp
// ❌ Wrong — repository does not own SaveChanges
await _orders.SaveChangesAsync();

// ✅ Correct — always use IUnitOfWork
await _uow.SaveChangesAsync(ct);
```

---

**[NEVER-02] Never inject `DbContext` directly into Application layer handlers.**

```csharp
// ❌ Wrong — Application has no dependency on EF Core
public class GetOrderQueryHandler(YourDbContext db) { }

// ✅ Correct — use IRepository or IReadRepository
public class GetOrderQueryHandler(IRepository<Order, Guid> orders) { }
```

---

**[NEVER-03] Never leak `IQueryable<T>` across layer boundaries.**

```csharp
// ❌ Wrong — IQueryable deferred to a caller outside Infrastructure
public IQueryable<Order> GetAllOrders() => _repo.Queryable;

// ✅ Correct — materialise inside the handler using specification or IEfQueryableRepository
var orders = await _repo.ListAsync(spec, ct);
```

---

**[NEVER-04] Never use `FindAsync` on `ITenantEntity` types.**

`FindAsync` bypasses EF Core global query filters. The framework's `GetByIdAsync` implementation already accounts for this. Do not call `dbContext.Set<T>().FindAsync(id)` for tenant-scoped entities.

---

**[NEVER-05] Never set `IgnoreQueryFilters = true` in a specification without registering an elevated `IQueryFilterBypassEvaluator`.**

The default `DenyQueryFilterBypassEvaluator` returns `false`. Setting `IgnoreQueryFilters` without an elevated evaluator throws at runtime. This is intentional — it prevents accidental cross-tenant data access.

---

**[NEVER-06] Never use `AllowDefaultTenantFallback = true` in production.**

This option routes unauthenticated requests to a default tenant, bypassing all tenant isolation. It exists for local development only. An environment guard is not yet enforced by the framework (TODO R-4); enforce it yourself via environment checks.

---

**[NEVER-07] Never create a repository for a non-aggregate-root entity.**

Only aggregate roots (`IAggregateRoot`) should have repositories. Access child entities (e.g., `OrderLine`) through the aggregate root (`Order`).

```csharp
// ❌ Wrong
services.AddScoped<IRepository<OrderLine, Guid>, ...>();

// ✅ Correct — access OrderLines through Order's repository
var order = await _orders.FirstOrDefaultAsync(new OrderWithLinesSpec(orderId), ct);
var line = order.Lines.First(l => l.Id == lineId);
```

---

**[NEVER-08] Never register a domain event in the outbox without an `IEventTypeRegistry` entry.**

The outbox interceptor throws `InvalidOperationException` at save time if an event type is unregistered. Register all domain event types at startup before any request is processed.

```csharp
// ❌ Wrong — triggers exception on first SaveChanges
// (forgot to call registry.Register<OrderPlacedDomainEvent>(...))

// ✅ Correct — register immediately after app.Build()
registry.Register<OrderPlacedDomainEvent>("order.placed.v1");
```

---

**[NEVER-09] Never use assembly-qualified names as outbox event keys.**

```csharp
// ❌ Wrong — breaks on rename/refactor/NuGet version change
registry.Register<OrderPlacedDomainEvent>(typeof(OrderPlacedDomainEvent).AssemblyQualifiedName!);

// ✅ Correct — use stable, human-readable versioned keys
registry.Register<OrderPlacedDomainEvent>("order.placed.v1");
```

---

**[NEVER-10] Never mix in-process dispatch and outbox for the same event in the same save call.**

```csharp
// ❌ Wrong — double-dispatch
await dbContext.SaveChangesAndPublishDomainEventsInProcessAsync(eventBus, ct);
await dbContext.SaveChangesWithTransactionalOutboxAsync(ct);  // already committed

// ✅ Correct — pick one pattern per aggregate and be consistent
```

---

**[NEVER-11] Never throw from a domain event handler without understanding the failure mode.**

In `FailFast` mode (default), an exception in any handler propagates immediately, rolling back the unit of work if the save has not yet committed. This can break the atomicity guarantee if domain events are dispatched after save. Design handlers to be idempotent and catch their own transient errors.

---

**[NEVER-12] Never pass `Guid.Empty` as `TenantId` in an `IntegrationEvent` when multi-tenancy is enabled.**

`EventPublisher.PublishAsync(IntegrationEvent)` validates this and throws `InvalidOperationException`. Populate `TenantId` before publishing.

---

**[NEVER-13] Never call `IEventPublisher` inside a domain entity or Application-layer handler.**

`IEventPublisher` is an infrastructure concern (MassTransit). Domain entities raise `IDomainEvent` via `AddDomainEvent`. Application handlers use `IEventBus` (in-process). `IEventPublisher` is called by the outbox processor or infrastructure-layer Hangfire jobs only.

---

**[NEVER-14] Never return `ResponseModel` with `Success = true` and a non-2xx status code, or vice versa.**

Use the provided factory methods (`ResponseModel.Ok`, `ResponseModel.Fail`, `ResponseModel.Created`, etc.) to ensure consistency. The `BaseController.ApiResult` routing depends on `StatusCode` being correctly set.

---

## 13. Known Gaps & Workarounds

These are **intentional omissions** as of the current version. They are tracked in [`TODO.md`](TODO.md). Do not attempt to work around them by reaching into lower layers — note them as tech debt.

| ID | Gap | Workaround until fixed |
|---|---|---|
| P-2 | `AuditSaveChangesInterceptor` only stamps `AuditableEntity<Guid>` | Entities with non-Guid keys: call `SetCreated`/`SetModified` manually in handlers |
| P-3 | `Repository<>` inherits `UnitOfWork<>` (IS-A instead of has-a) | Always inject and call `IUnitOfWork` separately — do not use the repo's `SaveChangesAsync` |
| P-5 | No combined `PagedListAsync(spec)` — two round-trips for paged grids | Issue `ListAsync` + `CountAsync` separately |
| O-4 | `BaseDomainEvent.EventId` uses `Guid.NewGuid()` (not UUID v7) | Override `EventId` in your domain event record: `public Guid EventId { get; } = Guid.CreateVersion7()` |
| O-5 | `IDomainEvent.OccurredAt` is `DateTime` (not `DateTimeOffset`) | Store UTC explicitly; do not assume timezone from this field |
| S-1 | CORS is `AllowAnyOrigin()` | Add CORS policy in consuming app `Program.cs` after `BaseRegister` to restrict origins |
| S-2 | Unauthenticated requests accept any `x-tenant-id` header | Ensure all tenant-sensitive endpoints require authentication via `[Authorize]` |
| S-3 | `AuditLog` has no `TenantId` | Add a tenant discriminator at the DB level or query filter manually |
| S-4 | `IAggregateRoot` not enforced by `IRepository<>` constraint | Enforce by team convention; only create repos for aggregate roots |
| S-5 | `IntegrationEvent.Payload` is `object` (untyped) | Define a concrete typed subclass with a strongly-typed payload property |
| S-6 | Hangfire dashboard defaults to `admin`/`admin` | Always set `Features:Hangfire:User` and `Features:Hangfire:Password` in all environments |
| D-1 | No `ValueObject` base class | Implement structural equality manually per value type |
| D-2 | `FluentValidation.ValidationException` maps to 500 | Catch `ValidationException` in your handler and convert to `BadRequestException` or `ResponseModel.ValidationFail` |
| D-3 | `DbUpdateConcurrencyException` maps to 500 | Catch in handler and throw `ConflictException` |

---

*Generated from source code analysis · [`docs/ARCHITECTURE_ANALYSIS.md`](docs/ARCHITECTURE_ANALYSIS.md) · [`TODO.md`](TODO.md)*
