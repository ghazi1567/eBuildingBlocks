# TODO — eBuildingBlocks Core Library

> Tasks derived from the full architecture analysis in [`docs/ARCHITECTURE_ANALYSIS.md`](docs/ARCHITECTURE_ANALYSIS.md).
> **No code changes have been made yet.** Each task links back to the finding that produced it.

---

## Legend

- **Priority:** `High` = correctness / security risk | `Medium` = quality / resilience | `Low` = nice-to-have
- **Layer:** The project(s) that need to change
- **Status:** `[ ]` open · `[x]` done · `[-]` won't fix / deferred

---

## Performance

### P-1 — Replace raw reflection in `InProcessEventBus` with compiled delegates
**Priority:** Medium  
**Layer:** `eBuildingBlocks.Infrastructure`  
**File:** `Infrastructure/Events/InProcessEventBus.cs`

`PublishAsync<TEvent>` resolves and invokes handlers via `handlerType.GetMethod("HandleAsync").Invoke(...)` on every call. This is uncached raw reflection.

**Fix:** Build and cache a `Func<object, TEvent, CancellationToken, Task>` delegate per handler type using `Expression.Lambda` compilation, stored in a `ConcurrentDictionary<Type, ...>`. The same pattern is already used in `DomainEventBusDispatch` and `OutboxPublisherDispatch`.

- [ ] Add a static `ConcurrentDictionary<Type, Func<object, IDomainEvent, CancellationToken, Task>>` cache to `InProcessEventBus`
- [ ] Replace `GetMethod("HandleAsync").Invoke(...)` with a compiled expression delegate
- [ ] Add a unit test verifying dispatch executes without reflection overhead across multiple invocations

---

### P-2 — Fix `AuditSaveChangesInterceptor` hardcoded to `AuditableEntity<Guid>` — done
**Priority:** High  
**Layer:** `eBuildingBlocks.Infrastructure`  
**File:** `Infrastructure/Implementations/AuditSaveChangesInterceptor.cs`

`ApplyAudit` calls `context.ChangeTracker.Entries<AuditableEntity<Guid>>()`. Any entity with a non-`Guid` primary key (`int`, `long`, `string`) is silently skipped — audit fields are never stamped.

**Fix:** Change the query to `Entries()` filtered by a non-generic base check (e.g., `e.Entity is AuditableEntity<Guid>` runtime check, or introduce a non-generic `IAuditableEntity` marker interface and use `Entries<IAuditableEntity>()`).

- [x] Introduce `IAuditableEntity` non-generic marker interface with `SetCreated` / `SetModified` methods
- [x] Implement `IAuditableEntity` on `AuditableEntity<TKey>`
- [x] Change `ApplyAudit` to query `Entries<IAuditableEntity>()` (or equivalent)
- [x] Verify audit stamping works for `int`, `long`, and `string` keyed entities — see `eBuildingBlocks.Infrastructure.Tests/Auditing/AuditSaveChangesInterceptorTests.cs` (also covers `Guid`, and modify-vs-add)

Shipped in `eBuildingBlocks.Domain` 3.2.0 (additive) and `eBuildingBlocks.Infrastructure` 5.0.0. See `CHANGELOG.md`.

---

### P-3 — Break `Repository<>` IS-A `UnitOfWork<>` inheritance — done
**Priority:** High  
**Layer:** `eBuildingBlocks.Infrastructure`  
**File:** `Infrastructure/Implementations/Repository.cs`, `Infrastructure/Implementations/UnitOfWork.cs`

`Repository<TEntity, TKey, TDbContext>` inherits `UnitOfWork<TDbContext>`. This IS-A relationship is architecturally wrong — a repository is not a unit of work. The consequence is that `SaveChangesAsync`, `BeginTransactionAsync`, and `ExecuteSqlAsync` are accessible on the concrete repository class.

**Fix:** Change to composition. Inject `TDbContext` directly into `Repository` (it already receives it) and remove the base class. Expose only `DbSet<TEntity>` access via a private helper.

- [x] Remove `: UnitOfWork<TDbContext>` from `Repository<TEntity, TKey, TDbContext>`
- [x] Store `TDbContext` as a private field in `Repository` (primary-constructor parameter, captured)
- [x] Move `Entities<TEntity>()` helper to a private method in `Repository`
- [x] Ensure `IUnitOfWork` is still separately registered (it is, via `DbContextUnitOfWork`)
- [x] Verify no public API surface is broken for existing consumers — audited: no code in this repo
      (reference apps, docs, templates) called `SaveChangesAsync`/`BeginTransactionAsync`/
      `ExecuteSqlAsync`/`Entities<T>()` on a repository instance. External consumers doing so are
      breaking — this is documented as a breaking change in `CHANGELOG.md` (Infrastructure 5.0.0).
      Regression coverage: `eBuildingBlocks.Infrastructure.Tests/Repositories/RepositoryCompositionTests.cs`.

Shipped in `eBuildingBlocks.Infrastructure` 5.0.0 (breaking — see `CHANGELOG.md` and `docs/VERSIONING.md`).

---

### P-4 — Remove or differentiate the five identical `IQueryable` properties on `Repository`
**Priority:** Low  
**Layer:** `eBuildingBlocks.Infrastructure`  
**File:** `Infrastructure/Implementations/Repository.cs`

`Queryable`, `GetQueryable`, `GetTrackedQueryable`, `ListQueryable`, `BulkUpdateQueryable`, `BulkDeleteQueryable` all return `Entities<TEntity>().AsQueryable()`. None are differentiated.

**Fix:** Either remove unused properties or provide meaningful overrides (e.g., `BulkUpdateQueryable` returns tracked, `ListQueryable` returns `AsNoTracking`).

- [ ] Audit each property to decide: remove or provide a real distinction
- [ ] Keep only `Queryable` (untracked base) and override in concrete repos where a distinct query is needed
- [ ] Remove `ProjectionExpression`, `GetProjectionExpression`, `ListProjectionExpression` if unused in all known consumers

---

### P-5 — Add a combined paged query method `(items, total) PagedListAsync`
**Priority:** Medium  
**Layer:** `eBuildingBlocks.Domain`, `eBuildingBlocks.Infrastructure`  
**File:** `Domain/Interfaces/IReadRepository.cs`, `Infrastructure/Implementations/Repository.cs`

Every paginated grid issues two separate DB round-trips: `ListAsync` + `CountAsync`. A combined method can run both queries in a single DB scope.

**Fix:** Add `Task<(IReadOnlyList<TEntity> Items, int Total)> PagedListAsync(ISpecification<TEntity> spec, CancellationToken ct)` to `IReadRepository` and implement it in `Repository` using two awaited queries within the same `IQueryable` chain.

- [ ] Add `PagedListAsync` to `IReadRepository<TEntity, TKey>`
- [ ] Implement in `Repository<TEntity, TKey, TDbContext>` — build the count query from the filtered (but non-paged) spec, and the data query from the fully paged spec
- [ ] Add corresponding `PagedResponseModel<T>` factory method if needed

---

### P-6 — Replace `Task.Delay` polling in outbox processor with a signal-based wakeup
**Priority:** Medium  
**Layer:** `eBuildingBlocks.Infrastructure`  
**File:** `Infrastructure/Outbox/OutboxProcessorBackgroundService.cs`, `Infrastructure/Outbox/DomainOutboxSaveChangesInterceptor.cs`

The outbox processor polls every 5 seconds regardless of activity, adding up to 2.5s average delivery latency.

**Fix:** Introduce a `Channel<bool>` or `SemaphoreSlim` wakeup signal. `DomainOutboxSaveChangesInterceptor.SavedChangesAsync` writes a signal after enqueuing messages. The processor awaits the signal with a timeout fallback equal to the current `PollInterval`.

- [ ] Add `IOutboxWakeupSignal` interface with `Signal()` and `WaitAsync(TimeSpan, CancellationToken)` methods
- [ ] Implement with `Channel<bool>` (bounded capacity 1, drop if full)
- [ ] Have `DomainOutboxSaveChangesInterceptor.SavedChangesAsync` call `Signal()` after successful enqueue
- [ ] Replace `Task.Delay(PollInterval)` in `OutboxProcessorBackgroundService.ExecuteAsync` with `WaitAsync`
- [ ] Register `IOutboxWakeupSignal` as singleton in `DomainOutboxInfrastructureExtensions`

---

### P-7 — Optimise `EventTypeRegistry` locking for read-heavy post-startup workloads
**Priority:** Low  
**Layer:** `eBuildingBlocks.Application`  
**File:** `Application/Eventing/EventTypeRegistry.cs`

`EventTypeRegistry` uses `lock(_sync)` for every read (`Resolve`, `TryGetEventName`). Post-startup the registry is effectively immutable but all reads acquire an exclusive lock.

**Fix:** Replace the `lock` with `ImmutableDictionary` + `Interlocked.CompareExchange` swap on write, or use `ReaderWriterLockSlim`. Since the registry is written only at startup, the immutable swap approach is simplest.

- [ ] Replace `Dictionary` fields with `ImmutableDictionary<string, Type>` and `ImmutableDictionary<Type, string>`
- [ ] Use `ImmutableInterlocked.TryAdd` or a `lock`-on-write / lock-free-on-read pattern
- [ ] Verify thread-safety under concurrent startup registration

---

## Resiliency

### R-1 — Add retry/resilience pipeline to `InProcessEventBus`
**Priority:** Medium  
**Layer:** `eBuildingBlocks.Infrastructure`, `eBuildingBlocks.Application`  
**File:** `Infrastructure/Events/InProcessEventBus.cs`, `Application/Events/EventBusOptions.cs`

In `Continue` mode, a failed handler is logged and silently discarded. There is no retry, backoff, or dead-letter mechanism for in-process domain event handlers.

**Fix:** Integrate `Microsoft.Extensions.Resilience` (Polly v8). Add optional `RetryPolicy` configuration to `EventBusOptions`. For each handler, wrap invocation in a resilience pipeline configured at startup.

- [ ] Add `RetryOptions` (max attempts, backoff) to `EventBusOptions`
- [ ] Add `Microsoft.Extensions.Resilience` package reference to `eBuildingBlocks.Infrastructure`
- [ ] In `InProcessEventBus`, build a `ResiliencePipeline` per handler type using registered options
- [ ] In `Continue` mode, failed handlers after exhausting retries should be collectable for a dead-letter callback

---

### R-2 — Add default EF Core `EnableRetryOnFailure` in library DI helpers
**Priority:** Medium  
**Layer:** `eBuildingBlocks.Infrastructure`  
**File:** `Infrastructure/Extensions/UnitOfWorkServiceCollectionExtensions.cs`

The library registers `IUnitOfWork` but does not configure EF Core execution strategy retries. Consumers must add `EnableRetryOnFailure` themselves or transient SQL errors propagate directly.

**Fix:** Document the requirement clearly in `AddDbContextUnitOfWork<T>` XML doc, and optionally provide an overload `AddDbContextUnitOfWork<T>(Action<SqlServerRetryingExecutionStrategy> configureRetry)`.

- [ ] Add XML doc note to `AddDbContextUnitOfWork` that the caller must configure `EnableRetryOnFailure`
- [ ] Optionally expose an overload that accepts a retry configuration delegate
- [ ] Add a warning log if the registered `TDbContext` does not have an execution strategy configured

---

### R-3 — Add Polly resilience to `EventPublisher.PublishAsync`
**Priority:** Medium  
**Layer:** `eBuildingBlocks.EventBus`  
**File:** `EventBus/Events/EventPublisher.cs`

Direct calls to `_publishEndpoint.Publish(...)` (MassTransit) are not retried. A transient broker unavailability propagates immediately to the caller.

**Fix:** Wrap the publish call in a `ResiliencePipeline` with configurable retry + exponential backoff. Since the transactional outbox is the recommended path, this applies primarily to consumers that use `IEventPublisher` directly outside the outbox flow.

- [ ] Add `EventPublisherOptions` with retry configuration
- [ ] Add `Microsoft.Extensions.Resilience` to `eBuildingBlocks.EventBus`
- [ ] Wrap `Publish` in `EventPublisher` with the resilience pipeline
- [ ] Register options via `IOptions<EventPublisherOptions>` in `IntegrationMassTransitServiceCollectionExtensions`

---

### R-4 — Add environment guard for `AllowDefaultTenantFallback`
**Priority:** High  
**Layer:** `eBuildingBlocks.Infrastructure`  
**File:** `Infrastructure/Tenancy/TenantResolver.cs`

`MultiTenancyOptions.AllowDefaultTenantFallback = true` is documented as development-only but there is no runtime environment assertion. A misconfigured production app silently routes all unauthenticated traffic under `DefaultTenantId`.

**Fix:** In `TenantResolver.TenantId`, if `AllowDefaultTenantFallback` is true and the current environment is `Production`, log a critical warning (or throw). Inject `IHostEnvironment` to check.

- [ ] Inject `IHostEnvironment` into `TenantResolver`
- [ ] In the `DefaultTenantId` fallback path, add: if `IsProduction && AllowDefaultTenantFallback` → `ILogger.LogCritical` + throw `TenantResolutionException`
- [ ] Update `MultiTenancyOptions` XML doc to reflect this behaviour

---

### R-5 — Add outbox depth and broker health checks
**Priority:** Medium  
**Layer:** `eBuildingBlocks.Infrastructure`, `eBuildingBlocks.API`  
**File:** `Infrastructure/Outbox/DomainOutboxInfrastructureExtensions.cs`, `API/Startup/HealthChecksServiceCollectionExtensions.cs`

The framework registers health check infrastructure but does not add checks for outbox message backlog depth or MassTransit/RabbitMQ broker liveness.

**Fix:**
- Add `AddOutboxHealthCheck<TDbContext>(threshold)` that queries `COUNT(*) WHERE ProcessedAtUtc IS NULL AND AttemptCount < MaxAttempts` and fails if above threshold.
- Add MassTransit health check (MassTransit provides `IHealthCheck` via `MassTransit.AspNetCore` already referenced).

- [ ] Implement `OutboxDepthHealthCheck<TDbContext>` using `IDbContextFactory` or `IServiceScopeFactory`
- [ ] Register via `AddOutboxHealthCheck<TDbContext>(services, options)` in `DomainOutboxInfrastructureExtensions`
- [ ] Enable MassTransit health check in `IntegrationMassTransitServiceCollectionExtensions`
- [ ] Document health check tags (`ready`, `live`) in `HealthChecksServiceCollectionExtensions`

---

### R-6 — Add alerting hook / DLQ forwarding for poison outbox messages
**Priority:** Medium  
**Layer:** `eBuildingBlocks.Infrastructure`  
**File:** `Infrastructure/Outbox/OutboxProcessorBackgroundService.cs`

When `AttemptCount >= MaxAttempts`, the row is poisoned and processing stops. The only signal is a `LogError` entry. There is no callback, webhook, or forwarding mechanism.

**Fix:** Introduce an `IOutboxPoisonHandler` interface. When a message is poisoned, call the registered handler (default: log-only). Consumers can register a custom handler to forward to a DLQ, send an alert, or emit a metric.

- [ ] Define `IOutboxPoisonHandler` with `HandleAsync(OutboxMessage, CancellationToken)` in Infrastructure
- [ ] Create `LogOnlyOutboxPoisonHandler` as the default implementation
- [ ] Call `IOutboxPoisonHandler.HandleAsync` in `MarkUnknownEventNamePoisonAsync` and after `AttemptCount >= MaxAttempts` in `RecordFailureAsync`
- [ ] Register `LogOnlyOutboxPoisonHandler` as default in `AddOutboxProcessor` (replaceable by consumer)

---

## Observability

### O-1 — Add OpenTelemetry metrics instrumentation
**Priority:** Medium  
**Layer:** `eBuildingBlocks.Infrastructure`, `eBuildingBlocks.API`  
**File:** `API/Startup/ServiceCollectionExtensions.cs`, various

Only distributed tracing is registered. No `Meter`, `Counter`, or `Histogram` instruments exist for repository queries, event dispatch, outbox operations, or handler latency.

**Fix:** Add `WithMetrics()` to the OpenTelemetry builder in `RegisterLog`. Instrument key operations:

- [ ] Add `WithMetrics(mp => mp.AddAspNetCoreInstrumentation().AddRuntimeInstrumentation())` to `RegisterLog`
- [ ] Create `eBuildingBlocksMeter` (static `Meter` in a shared `Telemetry` class)
- [ ] Add `outbox.messages.processed` counter in `OutboxProcessorBackgroundService`
- [ ] Add `outbox.messages.failed` counter in `RecordFailureAsync`
- [ ] Add `outbox.messages.poisoned` counter in `MarkUnknownEventNamePoisonAsync`
- [ ] Add `eventbus.handler.duration` histogram in `InProcessEventBus`
- [ ] Export via `AddOtlpExporter` in the metrics pipeline

---

### O-2 — Add `X-Correlation-Id` / `X-Request-Id` response header propagation
**Priority:** Medium  
**Layer:** `eBuildingBlocks.Application`  
**File:** `Application/Middlewares/` (new file)

`GlobalExceptionHandlerMiddleware` uses `ctx.TraceIdentifier` internally but never writes it back to the response. Clients cannot correlate their request to a backend log entry.

**Fix:** Add `CorrelationIdMiddleware` that reads `X-Correlation-Id` from the request (or generates one), writes it back in the response, and adds it to `ILogger` scope.

- [ ] Create `CorrelationIdMiddleware` in `Application/Middlewares`
- [ ] Read `X-Correlation-Id` header; fall back to `HttpContext.TraceIdentifier`
- [ ] Set `ctx.Response.Headers["X-Correlation-Id"]` before calling `_next`
- [ ] Use `ILogger.BeginScope(new { CorrelationId })` for the request scope
- [ ] Register in `BaseAppUse` before other middlewares (gated by `Features:Middlewares:CorrelationId`)

---

### O-3 — Add automatic structured log scope enrichment (TenantId, UserId, CorrelationId)
**Priority:** Medium  
**Layer:** `eBuildingBlocks.Application`  
**File:** `Application/Middlewares/` (new file or extend existing)

No middleware enriches `ILogger` scope with `TenantId` or `UserId`. Downstream handler and repository logs lack these dimensions, making per-tenant log queries impossible without manual instrumentation.

**Fix:** Create `RequestContextLoggingMiddleware` that calls `ILogger.BeginScope(new { TenantId, UserId, CorrelationId })` at the start of each request.

- [ ] Create `RequestContextLoggingMiddleware`
- [ ] Inject `ICurrentUser` and resolve `TenantId`, `UserId`
- [ ] Call `_logger.BeginScope(...)` with a structured dictionary before `_next`
- [ ] Register in `BaseAppUse` after authentication middleware (so claims are available)
- [ ] Gate with `Features:Middlewares:RequestContextLogging`

---

### O-4 — Change `BaseDomainEvent.EventId` to use `Guid.CreateVersion7()`
**Priority:** Low  
**Layer:** `eBuildingBlocks.Domain`  
**File:** `Domain/Models/BaseDomainEvent.cs`

`EventId = Guid.NewGuid()` generates a random UUID. `Guid.CreateVersion7()` (time-ordered) is already used in `OutboxMessage.Id` and `IntegrationEvent.EventId`. Using UUID v7 throughout enables time-ordered log correlation and index-friendly storage.

**Fix:** Replace `Guid.NewGuid()` with `Guid.CreateVersion7()` in `BaseDomainEvent`.

- [ ] Change `public Guid EventId { get; } = Guid.NewGuid()` → `= Guid.CreateVersion7()` in `BaseDomainEvent`
- [ ] Verify `IDomainEvent` contract does not need updating (it does not specify the generation strategy)

---

### O-5 — Change `IDomainEvent.OccurredAt` and `BaseDomainEvent.OccurredAt` to `DateTimeOffset`
**Priority:** Medium  
**Layer:** `eBuildingBlocks.Domain`  
**File:** `Domain/Models/BaseEntity.cs` (IDomainEvent), `Domain/Models/BaseDomainEvent.cs`

`IDomainEvent.OccurredAt` is `DateTime` and `BaseDomainEvent.OccurredAt` is `DateTime`. `AuditableEntity` already correctly uses `DateTimeOffset`. Mixing the two types across the framework causes timezone ambiguity in distributed systems and log aggregation.

**Fix:** Change both to `DateTimeOffset`. This is a **breaking change** to the `IDomainEvent` interface.

- [ ] Change `DateTime OccurredAt` → `DateTimeOffset OccurredAt` in `IDomainEvent`
- [ ] Change `DateTime.UtcNow` → `DateTimeOffset.UtcNow` in `BaseDomainEvent`
- [ ] Update `OutboxMessage.CreatedAtUtc` from `DateTime` to `DateTimeOffset` for consistency
- [ ] Bump major version on `eBuildingBlocks.Domain` NuGet package
- [ ] Update all known concrete event implementations in the framework (none in core library; notify consumers)

---

### O-6 — Add outbox processing metrics (processed / failed / poisoned counters)
**Priority:** Medium  
**Layer:** `eBuildingBlocks.Infrastructure`  
**File:** `Infrastructure/Outbox/OutboxProcessorBackgroundService.cs`

There are no counters for messages processed, failed, or poisoned per poll cycle. Operators cannot build dashboards or alerts without forking the library.

**Fix:** Depends on O-1 (shared `Meter`). Emit named counters from `OutboxProcessorBackgroundService`.

- [ ] Create named `Counter<long>` instruments in `OutboxProcessorBackgroundService` constructor
- [ ] Increment `outbox.messages.processed` on successful `ProcessedAtUtc` stamp
- [ ] Increment `outbox.messages.failed` in `RecordFailureAsync`
- [ ] Increment `outbox.messages.poisoned` in `MarkUnknownEventNamePoisonAsync`
- [ ] Add `outbox.poll.batch_size` histogram to track batch sizes over time

---

## Security

### S-1 — Make CORS `AllowedOrigins` configurable; remove `AllowAnyOrigin()` default
**Priority:** High  
**Layer:** `eBuildingBlocks.API`  
**File:** `API/Startup/ServiceCollectionExtensions.cs`

`RegisterCors` unconditionally builds `AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()` regardless of environment. There is no `AllowedOrigins` configuration key.

**Fix:** Read `Features:Cors:AllowedOrigins` (array). If provided, use `WithOrigins(...)`. If not provided and environment is not Development, log a warning. Only fall back to `AllowAnyOrigin()` explicitly in Development.

- [ ] Add `Features:Cors:AllowedOrigins` array support in `RegisterCors`
- [ ] If `AllowedOrigins` is set, use `policy.WithOrigins(origins)`
- [ ] If not set and `IHostEnvironment.IsProduction()`, throw or log critical warning instead of opening wildcard
- [ ] Update `appsettings.Example.txt` with `AllowedOrigins` documentation
- [ ] Update `CLAUDE.md` / docs

---

### S-2 — Block unauthenticated requests from supplying an arbitrary tenant header
**Priority:** High  
**Layer:** `eBuildingBlocks.Infrastructure`, `eBuildingBlocks.Application`  
**File:** `Infrastructure/Tenancy/TenantResolver.cs`, `Application/Middlewares/TenantHeaderValidationMiddleware.cs`

`TenantResolver` accepts the `x-tenant-id` header for *any* request (authenticated or not) if no JWT claim is present. `TenantHeaderValidationMiddleware` only validates header vs. claim for authenticated users. An unauthenticated caller can pass any `TenantId` and bypass the EF global query filter.

**Fix:** Add a `MultiTenancyOptions.AllowUnauthenticatedTenantHeader` flag (default `false`). In `TenantResolver`, skip the header resolution path if the user is not authenticated and this flag is false.

- [ ] Add `AllowUnauthenticatedTenantHeader : bool = false` to `MultiTenancyOptions`
- [ ] In `TenantResolver.TenantId`, check `httpContext.User.Identity?.IsAuthenticated` before using the header
- [ ] If unauthenticated and `AllowUnauthenticatedTenantHeader = false`, skip header → fall through to default/throw
- [ ] Update `TenantHeaderValidationMiddleware` XML docs to reflect the new option
- [ ] Add integration test: unauthenticated request with spoofed header should not resolve tenant

---

### S-3 — Add `TenantId` to `AuditLog` model
**Priority:** High  
**Layer:** `eBuildingBlocks.Domain`, `eBuildingBlocks.Infrastructure`  
**File:** `Domain/Models/AuditLog.cs`, `Infrastructure/Implementations/AuditSaveChangesInterceptor.cs`

`AuditLog` has no `TenantId` column. In a multi-tenant schema all tenants' audit rows share the same table without isolation, making per-tenant audit export impossible and risking cross-tenant audit data exposure.

**Fix:** Add `TenantId : Guid` to `AuditLog`. Populate it in `AuditSaveChangesInterceptor` from `ICurrentUser.TenantId`. Add a database index on `TenantId`.

- [ ] Add `public Guid TenantId { get; set; }` to `AuditLog`
- [ ] Inject `ICurrentUser` (already available) in `AuditSaveChangesInterceptor.PrepareAuditLogs`; set `audit.TenantId = _currentUser.TenantId`
- [ ] Add EF index configuration for `AuditLog.TenantId` in the model configuration helper (if any)
- [ ] Note: this is a **database migration breaking change** — bump package version and document

---

### S-4 — Enforce `IAggregateRoot` constraint on `IRepository<TEntity, TKey>`
**Priority:** Medium  
**Layer:** `eBuildingBlocks.Domain`  
**File:** `Domain/Interfaces/IRepository.cs`, `Domain/Interfaces/IReadRepository.cs`

Both `IRepository` and `IReadRepository` constrain `TEntity : class, IEntity`. The `IAggregateRoot` marker exists but is never enforced. Developers can create repositories for child entities or value objects, violating the DDD rule that repositories should only exist for aggregate roots.

**Fix:** Add `IAggregateRoot` to the `TEntity` constraint on both interfaces. This is a **breaking change**.

- [ ] Change `where TEntity : class, IEntity` → `where TEntity : class, IEntity, IAggregateRoot` in `IReadRepository`
- [ ] Same change in `IRepository`
- [ ] Update `Repository<TEntity, TKey, TDbContext>` constraint to match
- [ ] Bump major version on `eBuildingBlocks.Domain`
- [ ] Add `IAggregateRoot` to all aggregate root entities in the framework and document the requirement for consumers

---

### S-5 — Replace `IntegrationEvent.Payload : object` with typed generic payload
**Priority:** Medium  
**Layer:** `eBuildingBlocks.EventBus`  
**File:** `EventBus/Contracts/IntegrationEvent.cs`

`IntegrationEvent.Payload` is `object`. On the consumer side, the type must be known and cast manually. This is unsafe under message versioning and provides no compile-time safety. MassTransit may lose type discriminator information during serialization.

**Fix:** Replace with a typed generic `IntegrationEvent<TPayload>` where `TPayload : class`. Keep the non-generic `IntegrationEvent` as a base or remove it.

- [ ] Create `IntegrationEvent<TPayload> : IntegrationEvent where TPayload : class` with `TPayload Payload { get; init; }`
- [ ] Remove (or keep as abstract base) the non-generic `IntegrationEvent` with untyped `Payload`
- [ ] Update `EventPublisher.PublishAsync(IntegrationEvent)` overload if needed
- [ ] Bump major version on `eBuildingBlocks.EventBus` — this is a breaking change

---

### S-6 — Remove insecure Hangfire dashboard default credentials
**Priority:** High  
**Layer:** `eBuildingBlocks.API`  
**File:** `API/Startup/AppUseExtensions.cs`

`UsingHangfire` falls back to `admin`/`admin` when `Features:Hangfire:User` or `Features:Hangfire:Password` config keys are absent. Deploying without setting these keys leaves the Hangfire dashboard open with well-known credentials.

**Fix:** If credentials are not configured, either disable the dashboard or throw an `InvalidOperationException` in non-Development environments.

- [ ] In `UsingHangfire`, check if `user` or `pass` fall back to the default value
- [ ] If `IHostEnvironment.IsProduction()` and defaults are in use, throw `InvalidOperationException` with a clear message
- [ ] Optionally: allow disabling the dashboard entirely via `Features:Hangfire:EnableDashboard = false`
- [ ] Update `appsettings.Example.txt` with required credential keys

---

### S-7 — Make `RegisterOpenApi` contact info configurable
**Priority:** Low  
**Layer:** `eBuildingBlocks.API`  
**File:** `API/Startup/ServiceCollectionExtensions.cs`

Author name, email, and LinkedIn URL are hardcoded in `RegisterOpenApi`. Any team consuming this NuGet package cannot change the contact info without forking.

**Fix:** Add `OpenApiContactOptions` (name, email, url) to the feature configuration, defaulting to empty/null if not set.

- [ ] Create `OpenApiContactOptions` record (or use the existing `Features:Swagger:*` section)
- [ ] Read `Features:Swagger:Contact:Name`, `:Email`, `:Url` from config
- [ ] Only set `document.Info.Contact` if at least one field is configured
- [ ] Remove hardcoded personal information from the library source

---

### S-8 — Integrate .NET `RateLimiter` middleware
**Priority:** Low  
**Layer:** `eBuildingBlocks.API`  
**File:** `API/Startup/ServiceCollectionExtensions.cs`, `API/Startup/AppUseExtensions.cs`

`TooManyRequestException` exists in the exception hierarchy and maps to HTTP 429, but there is no rate limiting middleware integration. Consumers must add `AddRateLimiter` and `UseRateLimiter` manually.

**Fix:** Add optional `Features:RateLimiting` registration in `BaseRegister` and `BaseAppUse`.

- [ ] Add `RegisterRateLimiting(cfg)` to `ServiceCollectionExtensions` gated by `Features:RateLimiting`
- [ ] Support fixed-window policy from config (`Features:RateLimiting:PermitLimit`, `Window`, `QueueLimit`)
- [ ] Add `app.UseRateLimiter()` in `BaseAppUse` after routing
- [ ] Map `RateLimiterExceededException` to HTTP 429 in `GlobalExceptionHandlerMiddleware`

---

## Architectural / Design Debt

### D-1 — Add `ValueObject` base class
**Priority:** Low  
**Layer:** `eBuildingBlocks.Domain`  
**File:** `Domain/Models/` (new file)

No `ValueObject` base exists. Projects that need value types implement structural equality ad-hoc.

**Fix:** Add an abstract `ValueObject` base class with `Equals`, `GetHashCode`, and `==`/`!=` operators based on `GetAtomicValues()`.

- [ ] Create `Domain/Models/ValueObject.cs` with abstract `IEnumerable<object> GetAtomicValues()`
- [ ] Override `Equals` and `GetHashCode` using `GetAtomicValues()`
- [ ] Add `operator ==` and `operator !=`
- [ ] Add XML doc usage example

---

### D-2 — Add `FluentValidation.ValidationException` mapping to `GlobalExceptionHandlerMiddleware`
**Priority:** High  
**Layer:** `eBuildingBlocks.Application`  
**File:** `Application/GlobalExceptionHandler.cs`

`FluentValidation.ValidationException` is not handled — it falls through to the 500 catch-all. This produces confusing 500 errors for what should be 400 validation failures.

**Fix:** Add a case for `ValidationException` that maps to 400 with `ResponseModel.ValidationFail(...)`.

- [ ] Add `FluentValidation` package reference to `eBuildingBlocks.Application` (if not already present)
- [ ] Add `ValidationException ve => (HttpStatusCode.BadRequest, ResponseModel.ValidationFail(ve.Errors...))` to the `MapException` switch
- [ ] Convert `ve.Errors` (`IEnumerable<ValidationFailure>`) to `Dictionary<string, string[]>`

---

### D-3 — Add `DbUpdateConcurrencyException` mapping to `GlobalExceptionHandlerMiddleware`
**Priority:** Medium  
**Layer:** `eBuildingBlocks.Application`  
**File:** `Application/GlobalExceptionHandler.cs`

`DbUpdateConcurrencyException` (EF Core optimistic concurrency — `RowVersion` is already on `BaseEntity<TKey>`) falls through to 500. It should map to HTTP 409 Conflict.

**Fix:** Add a case for `DbUpdateConcurrencyException` → 409.

- [ ] Add `DbUpdateConcurrencyException => (HttpStatusCode.Conflict, ResponseModel.Fail("The resource was modified by another request.", HttpStatusCode.Conflict))` to `MapException`
- [ ] Note: this requires a reference to `Microsoft.EntityFrameworkCore` in `eBuildingBlocks.Application`, or the catch can be done in `eBuildingBlocks.Infrastructure` via a re-wrapping exception

---

## Summary Counts

| Category | Total Tasks | High | Medium | Low |
|---|---|---|---|---|
| Performance | 7 | 2 | 3 | 2 |
| Resiliency | 6 | 1 | 5 | 0 |
| Observability | 6 | 0 | 4 | 2 |
| Security | 8 | 4 | 2 | 2 |
| Design Debt | 3 | 1 | 1 | 1 |
| **Total** | **30** | **8** | **15** | **7** |
