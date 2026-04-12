# Architecture Analysis — eBuildingBlocks Core Library

> **Scope:** Core library projects only — `eBuildingBlocks.Domain`, `eBuildingBlocks.Application`,
> `eBuildingBlocks.Infrastructure`, `eBuildingBlocks.API`, `eBuildingBlocks.EventBus`, `eBuildingBlocks.Common`.
> The ReferenceApp is excluded from this analysis.
>
> **Reviewed against:** .NET 10, commit branch `latest-dotnet-10`.

---

## Table of Contents

1. [Project Dependency Graph](#1-project-dependency-graph)
2. [DDD Core — Entity Hierarchy & Domain Events](#2-ddd-core--entity-hierarchy--domain-events)
3. [Persistence — Repository & Unit of Work](#3-persistence--repository--unit-of-work)
4. [Messaging — Event Bus & Integration Events](#4-messaging--event-bus--integration-events)
5. [Transactional Outbox](#5-transactional-outbox)
6. [Cross-Cutting Concerns](#6-cross-cutting-concerns)
7. [API Layer](#7-api-layer)
8. [Gaps & Improvements](#8-gaps--improvements)

---

## 1. Project Dependency Graph

```
eBuildingBlocks.Common
    └── eBuildingBlocks.Domain
            ├── eBuildingBlocks.Application
            │       └── eBuildingBlocks.Infrastructure
            │               └── (also refs EventBus)
            └── eBuildingBlocks.EventBus
                    └── eBuildingBlocks.Common

eBuildingBlocks.API  →  all of the above
```

All projects target `net10.0` with `Nullable enable` and `ImplicitUsings enable`. Each project is independently NuGet-publishable (`GeneratePackageOnBuild=True` on Release builds).

---

## 2. DDD Core — Entity Hierarchy & Domain Events

### 2.1 Entity Hierarchy

| Class | Key Additions |
|---|---|
| `IEntity` | Marker interface only |
| `BaseEntity<TKey>` | Generic PK; `[Timestamp] RowVersion` (optimistic concurrency); structural equality by `Id + GetType()`; `DomainEvents` collection with `Add/Remove/ClearDomainEvents` |
| `AuditableEntity<TKey>` | `CreatedOn/By`, `ModifiedOn/By` (`DateTimeOffset` — correct); `SetCreated()/SetModified()` helpers |
| `TenantEntity<TKey>` | `TenantId : Guid` for multi-tenant isolation |
| `IAggregateRoot` | Marker interface — advisory only, **not enforced** by `IRepository` |

**Missing:** There is no `ValueObject` base class. Structural equality for value types must be implemented ad-hoc in each consuming project.

### 2.2 Domain Events

| Type | Role |
|---|---|
| `IDomainEvent` | Contract: `EventId : Guid`, `OccurredAt : DateTime`, `TenantId : Guid` |
| `BaseDomainEvent` | Abstract implementation; auto-assigns `EventId` and `OccurredAt`; supports parameterless and tenant-aware constructors |
| `IHasDomainEvents` | Optional marker so infrastructure can collect events without reflection |

**Known defects in `BaseDomainEvent`:**
- `EventId = Guid.NewGuid()` — uses random UUID instead of time-ordered UUID v7 (`Guid.CreateVersion7()`), which is already used in `OutboxMessage.Id` and `IntegrationEvent.EventId`.
- `OccurredAt : DateTime` — should be `DateTimeOffset` (timezone-unambiguous). `AuditableEntity` already uses `DateTimeOffset` correctly.

---

## 3. Persistence — Repository & Unit of Work

### 3.1 Interfaces

```
IReadRepository<TEntity, TKey>
    GetByIdAsync, FirstOrDefaultAsync, SingleOrDefaultAsync,
    ListAsync, ListAllAsync, CountAsync, AnyAsync

IRepository<TEntity, TKey> : IReadRepository<TEntity, TKey>
    AddAsync, UpdateAsync, DeleteAsync

IUnitOfWork
    SaveChangesAsync(CancellationToken)
```

`IRepository` carries **no `SaveChangesAsync`** — by design. Callers must inject `IUnitOfWork` separately. This is the correct CQRS boundary.

### 3.2 `Repository<TEntity, TKey, TDbContext>` — Concrete Implementation

```csharp
public class Repository<TEntity, TKey, TDbContext>(TDbContext dbContext)
    : UnitOfWork<TDbContext>(dbContext),          // ← IS-A UnitOfWork (structural smell)
      IRepository<TEntity, TKey>,
      IEfQueryableRepository<TEntity, TKey>
```

**Tenant-safety in `GetByIdAsync`:**  
For `ITenantEntity` types, `FindAsync` (which bypasses global query filters) is deliberately avoided; a LINQ `FirstOrDefaultAsync` filtered query is used instead. This is a correct and intentional security control.

**`IEfQueryableRepository<TEntity, TKey>`** (infrastructure-only port) exposes raw `IQueryable<T>`. Application code should use this only through the explicit cast helper `AsEfQueryable()`.

**`UnitOfWork<TDbContext>` concrete class** exposes `BeginTransactionAsync` and `ExecuteSqlInterpolatedAsync` — these are **not** on `IUnitOfWork`, so they are inaccessible through the domain interface but are accessible on the concrete repository.

### 3.3 Specification Pattern

| Type | Layer | Purpose |
|---|---|---|
| `ISpecification<T>` | Domain | Criteria, string includes, ordering, paging, `AsNoTracking`, `IgnoreQueryFilters` |
| `SpecificationBase<T>` | Domain | Concrete base; `AsNoTracking = true` by default; dynamic `CompositeFilter` via `DynamicPredicate.Build` |
| `IEfSpecification<T>` | Infrastructure | Adds EF-specific typed `Include` expressions and `IncludeChains` |
| `SpecificationEvaluator` | Infrastructure | Applies all spec parts; gates `IgnoreQueryFilters` behind `IQueryFilterBypassEvaluator` |

**`IgnoreQueryFilters` security control:** `SpecificationEvaluator` checks `IQueryFilterBypassEvaluator.CanIgnoreGlobalQueryFilters` before calling `IgnoreQueryFilters()`. The default implementation `DenyQueryFilterBypassEvaluator` returns `false`, causing a hard exception. Only an explicitly registered elevated evaluator (e.g., for migration tooling or super-admin scenarios) can bypass tenant filters.

### 3.4 Unit of Work — Two Implementations

| Class | Use Case |
|---|---|
| `DbContextUnitOfWork<TDbContext>` | Standalone UoW; registered via `AddDbContextUnitOfWork<T>()` |
| `UnitOfWork<TDbContext>` (base of `Repository`) | Repository's internal save path |

---

## 4. Messaging — Event Bus & Integration Events

### 4.1 Domain Event Bus (In-Process)

```
IEventBus
    PublishAsync<TEvent>(TEvent, CancellationToken)  ← generic
    PublishAsync(IDomainEvent, CancellationToken)     ← default interface method via DomainEventBusDispatch

IEventHandler<TEvent>
    HandleAsync(TEvent, CancellationToken)
```

**`InProcessEventBus`** resolves all `IEventHandler<TEvent>` from DI per event type and invokes them sequentially. Execution order is **non-deterministic**; handlers must be independent and idempotent (documented).

**Failure modes (configurable via `EventBusOptions`):**

| Mode | Behaviour |
|---|---|
| `FailFast` (default) | First handler exception propagates immediately |
| `Continue` | All handlers run; failures are logged but not re-thrown |

**Dispatch mechanism:**  
The default `IEventBus.PublishAsync(IDomainEvent)` method uses `DomainEventBusDispatch`, which builds and caches compiled reflection delegates in a `ConcurrentDictionary<Type, Func<...>>`.  
However, `InProcessEventBus.PublishAsync<TEvent>` itself invokes handlers via `GetMethod("HandleAsync").Invoke(...)` — uncached raw reflection per dispatch call.

### 4.2 Integration Event Bus (Cross-Process — MassTransit)

```
IEventPublisher
    PublishAsync<T>(T, CancellationToken)
    PublishAsync(IntegrationEvent, CancellationToken)

IEventSubscriber
    (consumer registration via MassTransit assembly scan)
```

**`EventPublisher`** wraps MassTransit `IPublishEndpoint`. When multi-tenancy is enabled, it validates that `IntegrationEvent.TenantId != Guid.Empty` before publishing.

**Transport:** RabbitMQ only (`MassTransit.RabbitMQ 8.2.5`). In-memory transport available for local development via `MassTransit:UseInMemory = true`.

**`IntegrationEvent` base:**  
Carries `EventId` (UUID v7), `TenantId`, `EventType` (enum), `CorrelationId`, `OccurredAt` (`DateTime`), and **`Payload : object`** (untyped).

**Registration:** `AddIntegrationMassTransit(services, config, consumerAssemblies)` auto-discovers MassTransit consumers from provided assemblies.

---

## 5. Transactional Outbox

The outbox is the framework's most complete subsystem. It prevents the dual-write problem between the database and the message broker.

### 5.1 Write Path — `DomainOutboxSaveChangesInterceptor`

1. In `SavingChangesAsync`: scans EF change tracker for entities implementing `IHasDomainEvents`.
2. For each domain event, looks up the stable event name from `IEventTypeRegistry` (throws if unregistered — fail-fast at startup).
3. Serializes events to `OutboxMessage` rows and adds them to the same `DbContext` transaction.
4. Stores entity references in a `ConcurrentDictionary<DbContext, List<object>>` pending post-commit clear.
5. In `SavedChangesAsync`: clears domain event collections on aggregates **only after a successful commit**.
6. In `SaveChangesFailedAsync`: discards pending clears — events remain on aggregates for retry.

`DomainOutboxExecutionContext.SuppressTransactionalEnqueue` (`AsyncLocal<bool>`) allows in-process dispatch to suppress outbox enqueue, wrapped in `try/finally` for safety.

### 5.2 Read/Publish Path — `OutboxProcessorBackgroundService<TDbContext>`

- Polls every `PollInterval` (default 5s).
- Claims rows with SQL Server `UPDLOCK, READPAST, ROWLOCK` hints via Dapper — safe under multi-pod deployment (no duplicate processing).
- Sets `LockedUntil` lease before processing so competing pods skip claimed rows.
- Deserializes payload using `IEventTypeRegistry.Resolve(eventName)`.
- Publishes via `IEventPublisher` (MassTransit).
- On failure: increments `AttemptCount`, sets exponential `LockedUntil` backoff.
- On `AttemptCount >= MaxAttempts`: marks row as poison (no more retries), logs critical error.
- Unknown `EventName`: poisons immediately without retrying.

### 5.3 `IEventTypeRegistry` / `EventTypeRegistry`

Thread-safe bidirectional string↔Type map. Populated at startup with stable human-readable keys (e.g., `"order.placed.v1"`). Uses `lock(object)` for both read and write.

### 5.4 Outbox Table Schema

| Column | Notes |
|---|---|
| `Id` | UUID v7 |
| `DomainEventId` | Indexed; correlates to `IDomainEvent.EventId` |
| `EventName` | Max 512 chars; indexed |
| `PayloadJson` | Full JSON of the concrete event type |
| `TenantId` | Required |
| `AttemptCount` | Default 0 |
| `LockedUntil` | Indexed; lease for multi-pod concurrency |
| `ProcessedAtUtc` | Indexed; null = pending |
| `LastError` | Max 4000 chars |

---

## 6. Cross-Cutting Concerns

### 6.1 Audit Logging — `AuditSaveChangesInterceptor`

Fires in `SavingChangesAsync` (before save, same transaction).

**`ApplyAudit`** — Sets `CreatedOn/By` or `ModifiedOn/By` on changed entities.  
**Defect:** Hardcoded to `ChangeTracker.Entries<AuditableEntity<Guid>>()`. Entities with non-`Guid` primary keys (`int`, `long`, `string`) receive no audit stamp.

**`PrepareAuditLogs`** — Creates `AuditLog` rows for all Add/Modify/Delete entries:
- Modified: old and new values for changed properties only (efficient diff).
- Added: full new value snapshot.
- Deleted: full old value snapshot.
- Excludes `AuditLog` itself (no infinite recursion).

**Defect:** `AuditLog` model has no `TenantId` column. All tenants' audit rows share the same table without isolation.

### 6.2 Global Exception Handling — `GlobalExceptionHandlerMiddleware`

Classic ASP.NET Core middleware (not `IExceptionHandler`). Maps known exception types to HTTP status codes via a `switch` expression:

| Exception | Status |
|---|---|
| `BadRequestException` | 400 |
| `TenantResolutionException` | 400 |
| `UnauthorizedException` | 401 |
| `ForbiddenException` | 403 |
| `NotFoundException` | 404 |
| `MethodNotAllowedException` | 405 |
| `ConflictException` | 409 |
| `TooManyRequestException` | 429 |
| `NotImplementedException` | 501 |
| `OperationCanceledException` | 408 |
| *(anything else)* | 500 |

**Gaps:** `FluentValidation.ValidationException` is not mapped (falls to 500). `DbUpdateConcurrencyException` is not mapped (falls to 500).

Writes a `ResponseModel` JSON body. Guards against `ctx.Response.HasStarted`. Re-throws `OperationCanceledException` when the request is aborted.

### 6.3 FluentValidation Integration

Validators registered via `AddValidatorsFromAssembly(assembly)` in `BaseRegister`. **No pipeline behavior** (no MediatR, no automatic validation middleware). Validators must be explicitly injected and invoked in command/query handlers.

### 6.4 Multi-Tenancy

**Resolution priority in `TenantResolver`:**
1. `ITenantScope.OverrideTenantId` — `AsyncLocal<Guid>` for background jobs / tests
2. JWT claim (`MultiTenancyOptions.ClaimType`, default `tenant_id`)
3. HTTP header (`MultiTenancyOptions.HeaderName`, default `x-tenant-id`)
4. Default tenant fallback (if `AllowDefaultTenantFallback = true` — documented dev-only)
5. Throws `TenantResolutionException` if nothing resolves

**`TenantHeaderValidationMiddleware`** — For authenticated requests, rejects (403) if the HTTP header disagrees with the JWT claim. Unauthenticated requests bypass this check.

**`TenantAwareDbContext`** — Abstract DbContext base that calls `ApplyRuntimeTenantQueryFilters(_currentUser)` in `OnModelCreating` when `MultiTenancyOptions.Enabled = true`. Also applies `TenantId` indexes via `ApplyTenantEntityIndexes()`.

**`TenantMemoryCache`** — Wraps `IMemoryCache`; prefixes all keys with `{TenantId:N}:` when multi-tenancy is enabled, preventing cross-tenant cache reads.

---

## 7. API Layer

### 7.1 `BaseRegister` — Feature-Gated DI Registration

All registrations are controlled by `appsettings.json` `Features:*` flags. Enabled features:

| Feature Key | Registers |
|---|---|
| `Features:Controllers` | MVC controllers with slugified routes + `JsonStringEnumConverter` |
| `Features:ApiVersioning` | Asp.Versioning with URL segment + `X-API-Version` header readers |
| `Features:OpenTelemetry` | OTLP trace exporter (tracing only) |
| `Features:MemoryCache` | `IMemoryCache` + `ITenantMemoryCache` |
| `Features:Redis` | `IConnectionMultiplexer` |
| `Features:Swagger` | Scalar/OpenAPI with JWT Bearer + API key security schemes |
| `Features:Cors` | Single policy — **always `AllowAnyOrigin()`** |
| `Features:FeatureManagement` | Microsoft.FeatureManagement |
| `Features:Hangfire` | Hangfire with memory or SQL Server storage |
| `Features:FluentValidation` | `AddValidatorsFromAssembly` (if assembly passed) |

### 7.2 `BaseAppUse` — Middleware Pipeline Order

```
CORS → Scalar → Hangfire Dashboard → Prometheus Metrics → Routing →
Authentication/TenantValidation/ApiKey/Authorization →
GlobalExceptionHandler + HttpResponse middlewares → Endpoints (controllers + health + metrics)
```

### 7.3 OpenTelemetry Coverage

- **Tracing:** ASP.NET Core instrumentation + HTTP client instrumentation + optional OTLP exporter. ✓
- **Metrics:** None registered by the framework.
- **Logging:** No structured scope enrichment (tenant, user, correlation) applied automatically.

---

## 8. Gaps & Improvements

See [`TODO.md`](../TODO.md) for the full actionable task list derived from these findings.

### Performance Gaps

| # | Finding |
|---|---|
| P-1 | `InProcessEventBus` dispatches via uncached raw reflection (`GetMethod.Invoke`) — should use compiled delegates |
| P-2 | `AuditSaveChangesInterceptor` hardcoded to `AuditableEntity<Guid>` — silently ignores non-Guid keyed entities |
| P-3 | `Repository<>` IS-A `UnitOfWork<>` — should be has-a (composition), leaks transaction methods onto the repo |
| P-4 | Five `IQueryable` properties (`GetQueryable`, `ListQueryable`, `BulkUpdateQueryable`, etc.) all return the same query — dead surface area |
| P-5 | No combined `(items, total) PagedListAsync` — every paged grid needs two DB round-trips |
| P-6 | Outbox uses `Task.Delay` polling (5s default) — no signal-based wakeup when a new message is saved |
| P-7 | `EventTypeRegistry` uses `lock(object)` for all reads — should use read-optimised locking post-startup |

### Resiliency Gaps

| # | Finding |
|---|---|
| R-1 | No retry/circuit-breaker on `InProcessEventBus` — `Continue` mode silently loses events on failure |
| R-2 | No EF Core `EnableRetryOnFailure` configured in library DI helpers — transient SQL failures are unhandled |
| R-3 | No Polly retry on `EventPublisher.PublishAsync` — direct MassTransit publish failures propagate immediately |
| R-4 | `AllowDefaultTenantFallback` has no environment guard — misconfigured production apps silently route to a default tenant |
| R-5 | No health check for outbox message backlog depth or RabbitMQ/MassTransit liveness |
| R-6 | Poison outbox messages accumulate silently — no alerting hook or DLQ forwarding |

### Observability Gaps

| # | Finding |
|---|---|
| O-1 | OpenTelemetry: tracing only — no `Meter`/`Counter`/`Histogram` for repository, event, or outbox operations |
| O-2 | No `X-Correlation-Id` / `X-Request-Id` response header propagation |
| O-3 | No automatic structured log scope enrichment for `TenantId`, `UserId`, or `CorrelationId` |
| O-4 | `BaseDomainEvent.EventId` uses `Guid.NewGuid()` — should use `Guid.CreateVersion7()` for time-ordered tracing |
| O-5 | `BaseDomainEvent.OccurredAt` and `IDomainEvent.OccurredAt` are `DateTime` — should be `DateTimeOffset` |
| O-6 | No outbox metrics — no counters for messages processed/failed/poisoned per poll cycle |

### Security Gaps

| # | Finding |
|---|---|
| S-1 | CORS hardcoded to `AllowAnyOrigin()` — no configurable `AllowedOrigins` whitelist |
| S-2 | Unauthenticated requests can supply an arbitrary `x-tenant-id` header which becomes the EF query filter — tenant boundary bypass for public endpoints |
| S-3 | `AuditLog` has no `TenantId` column — cross-tenant audit data co-exists without isolation |
| S-4 | `IAggregateRoot` not enforced on `IRepository<TEntity,TKey>` — repositories can be created for non-root child entities |
| S-5 | `IntegrationEvent.Payload` is `object` (untyped) — unsafe cast on consumer side; no compile-time safety or versioning support |
| S-6 | Hangfire dashboard defaults to `admin`/`admin` when config keys are absent |
| S-7 | `RegisterOpenApi` hardcodes personal contact info (name, email, LinkedIn) — not configurable for other NuGet consumers |
| S-8 | No .NET 7+ `RateLimiter` middleware integration despite `TooManyRequestException` existing in the exception hierarchy |
