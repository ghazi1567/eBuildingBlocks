# Multi-tenancy: developer guide

This guide explains how **eBuildingBlocks** resolves the current tenant, enforces row isolation in EF Core, and how to use the same model in **HTTP APIs**, **background jobs**, and **message consumers**.

---

## What you get

| Capability | Behavior |
|------------|----------|
| **Tenant resolution** | `ICurrentUser.TenantId` throws **`TenantResolutionException`** (→ HTTP 400 via global handler) if multi-tenancy is enabled and no tenant is resolved; use **`ITenantScope.Begin`** for overrides |
| **EF Core isolation** | Global query filters on `ITenantEntity` when multi-tenancy is **enabled** |
| **Safe `GetById`** | Repository avoids `FindAsync` for tenant entities so filters still apply |
| **Caching** | `ITenantMemoryCache` prefixes keys with the current tenant when enabled |
| **Integration bus** | `IntegrationEvent.TenantId` and **`IDomainEvent.TenantId`** are non-nullable **`Guid`**; must be non-empty when multi-tenancy is enabled |
| **Header vs JWT** | Optional middleware rejects mismatched header + claim for authenticated users |
| **Bypassing filters** | `IgnoreQueryFilters` on specifications is denied unless you register an elevated `IQueryFilterBypassEvaluator` |

---

## Prerequisites

| Requirement | Notes |
|-------------|--------|
| **Configuration** | Section `Features:MultiTenancy` in configuration (see below). |
| **API hosts** | `BaseRegister` (eBuildingBlocks.API) already calls `AddTenantContextCore` via `RegisterCurrentUser`. |
| **Worker / console hosts** | Call `AddTenantContextForWorkers` (same as `AddTenantContextCore`) and still register `IMemoryCache` if you use `ITenantMemoryCache`. |
| **EF Core** | Derive your `DbContext` from `TenantAwareDbContext` (or equivalent pattern) and inject `ICurrentUser` + `IOptions<MultiTenancyOptions>`. |
| **Application service provider** | For `IQueryFilterBypassEvaluator` resolution inside the specification pipeline, use `options.UseApplicationServiceProvider(sp)` in your `AddDbContext` factory. |

### Defaults and strictness

- **`MultiTenancyOptions.Enabled`** defaults to **`true`** in code. Set **`"Enabled": false`** under `Features:MultiTenancy` only for single-tenant deployments.
- When **`Enabled`** is **`true`**, **`ICurrentUser.TenantId`** never returns **`Guid.Empty`**: it either returns a resolved tenant or throws **`TenantResolutionException`**.
- **`ITenantScope.Begin(Guid)`** throws if **`tenantId`** is **`Guid.Empty`**.
- **`OutboxMessage.TenantId`** is a non-nullable **`Guid`** (use **`Guid.Empty`** only when the host has multi-tenancy disabled). Apply the **`OutboxTenantIdRequired`** (or equivalent) migration for existing databases.

---

## 1. Configuration (`Features:MultiTenancy`)

Bind options under **`Features:MultiTenancy`** (same path `IOptions<MultiTenancyOptions>` uses).

```json
"Features": {
  "MultiTenancy": {
    "Enabled": true,
    "HeaderName": "X-Tenant-Id",
    "ClaimType": "tenant_id",
    "AllowDefaultTenantFallback": false,
    "ValidateHeaderAgainstClaims": true,
    "DefaultTenantId": "00000000-0000-0000-0000-000000000000"
  }
}
```

| Property | Purpose |
|----------|---------|
| `Enabled` | **Default `true` in code.** When `false`, `ICurrentUser.TenantId` returns `Guid.Empty` and **no** tenant query filters are applied by `TenantAwareDbContext`. |
| `HeaderName` | HTTP header for tenant id (case-insensitive for typical servers). |
| `ClaimType` | JWT / `ClaimsPrincipal` claim type for tenant id (defaults to `tenant_id` via `CustomClaimTypes.TenantId`). **Do not** reuse the header name here. |
| `AllowDefaultTenantFallback` | If `true` and nothing else resolves a tenant, use `DefaultTenantId` (still non-empty). Use for **local dev only**; in production prefer explicit claim/header/scope. |
| `DefaultTenantId` | Used only when `AllowDefaultTenantFallback` is `true`; must be non-empty to act as fallback. |
| `ValidateHeaderAgainstClaims` | When the user is **authenticated** and both claim and header carry tenant ids, they must match or the request gets **403**. |

**JWT + header:** Resolution order is **ambient scope → claim → header → optional default**. Claims are preferred over the header to reduce reliance on a spoofable header alone; the middleware adds an extra check when both are present.

---

## 2. Registration (DI)

### ASP.NET Core (full API stack)

If you use **`BaseRegister`** from eBuildingBlocks.API, tenant services are already registered (`ICurrentUser`, `ITenantScope`, `IQueryFilterBypassEvaluator`, options binding).

If you assemble DI manually:

```csharp
using eBuildingBlocks.Infrastructure.Tenancy;

builder.Services.AddTenantContextCore(builder.Configuration);
// After AddMemoryCache():
builder.Services.AddTenantMemoryCache();
```

### Workers, Hangfire hosts, or minimal APIs without `BaseRegister`

```csharp
services.AddTenantContextForWorkers(configuration); // same as AddTenantContextCore
```

You still need **`AddHttpContextAccessor()`** if any code path uses `IHttpContextAccessor` (included in `AddTenantContextCore`).

### Authentication middleware (header vs claim validation)

When `ValidateHeaderAgainstClaims` is `true` and multi-tenancy is `Enabled`, the pipeline may call **`UseAuthentication()`** even if `Features:Authorization` is off, so that claims exist. Ensure **`AddAuthentication()`** is registered (empty default is enough for the middleware to run safely). The reference app calls `builder.Services.AddAuthentication()` for this reason.

---

## 3. Modeling tenant-scoped data

### Entity

- Implement **`ITenantEntity`** (`TenantId`), or inherit **`TenantEntity<TKey>`** from the Domain package.
- Set **`TenantId`** when creating aggregates (factory / command handler), typically from `ICurrentUser.TenantId` when `Enabled` is true.

### DbContext

- Inherit **`TenantAwareDbContext`** (Infrastructure) **or** call `ApplyRuntimeTenantQueryFilters(ICurrentUser)` in `OnModelCreating` only when `MultiTenancyOptions.Enabled` is true (the base class does this for you).
- Constructor must receive **`ICurrentUser`** and **`IOptions<MultiTenancyOptions>`** and pass them to `TenantAwareDbContext`.

### EF Core options (important)

In `AddDbContext`, use:

```csharp
options.UseApplicationServiceProvider(serviceProvider);
```

so **`DbContext.GetService<IQueryFilterBypassEvaluator>()`** works for the specification pipeline.

### Design-time / migrations

Use **`IDesignTimeDbContextFactory<T>`** with **`MultiTenancyOptions { Enabled = false }`** and a stub `ICurrentUser` so migrations do not apply tenant filters to the model snapshot. See `ReferenceDbContextFactory` in the reference solution.

---

## 4. Repository and specifications

### `GetByIdAsync`

For types implementing **`ITenantEntity`**, `GetByIdAsync` uses a **filtered LINQ query** (not `FindAsync`), so global query filters apply.

### `IgnoreQueryFilters`

Specifications with **`IgnoreQueryFilters == true`** throw unless **`IQueryFilterBypassEvaluator.CanIgnoreGlobalQueryFilters`** is true.

- Default registration: **`DenyQueryFilterBypassEvaluator`** (always false).
- For admin-only or migration tooling, register your own **`IQueryFilterBypassEvaluator`** (singleton) that returns `true` only in controlled environments.

### Raw `IQueryable`

Code that uses **`IEfQueryableRepository.Queryable`** (or direct `DbSet`) still runs through EF’s query pipeline, so **global filters apply**. Any use of **`IgnoreQueryFilters()`** in hand-written LINQ bypasses **all** filters—treat that as privileged code.

---

## 5. Ambient tenant: `ITenantScope` (non-HTTP)

**`ITenantScope.Begin(Guid tenantId)`** sets an **`AsyncLocal<Guid>`** override for the current async flow. **`ICurrentUser.TenantId`** returns that value when it is not `Guid.Empty`.

Use it in:

- **MassTransit consumers** (wrap `Consume` after reading tenant from the message).
- **Hangfire** job methods.
- **`IHostedService`** or channel readers that process per-tenant work.

Example:

```csharp
public async Task Consume(ConsumeContext<MyCommand> context)
{
    var tenantId = context.Message.TenantId; // or from envelope
    using (_tenantScope.Begin(tenantId))
    {
        await _handler.HandleAsync(context.Message, context.CancellationToken);
    }
}
```

Register **`ITenantScope`** once (singleton). Do not nest `Begin` without disposing the inner scope in reverse order (each `Begin` returns `IDisposable`).

---

## 6. Domain events and transactional outbox

- **`IDomainEvent.TenantId`** is **`Guid`** (not nullable). Implementations should set it before enqueue; **`BaseDomainEvent`** and the reference **`OrderPlacedDomainEvent`** follow this shape.
- The outbox interceptor sets **`OutboxMessage.TenantId`** from the event when non-empty, otherwise from **`ITenantEntity.TenantId`** on the aggregate.
- If multi-tenancy is **enabled** and the effective tenant id would be empty, enqueue **throws** (fail fast).

See also **`docs/HOSTING_APP_OUTBOX.md`** for event registration and processing.

---

## 7. Integration events (`IntegrationEvent`)

When **`MultiTenancyOptions.Enabled`** is true, **`EventPublisher.PublishAsync(IntegrationEvent)`** requires **`TenantId != Guid.Empty`**.

Set it from the publishing boundary (e.g. `ICurrentUser.TenantId` or the message’s tenant).

---

## 8. Tenant-scoped memory cache

Inject **`ITenantMemoryCache`** instead of raw **`IMemoryCache`** for tenant-isolated entries when multi-tenancy is enabled. Keys are prefixed with the current tenant id (`N` format) + `:`.

Registration (after `AddMemoryCache`):

```csharp
builder.Services.AddTenantMemoryCache();
```

---

## 9. Application-level checks

Handlers may still validate **`currentUser.TenantId == Guid.Empty`** when multi-tenancy is enabled (see reference `PlaceOrderCommandHandler`) to return a **400** with a clear message instead of relying on empty tenant matching no rows.

---

## 10. Quick checklist for a new service

1. Set **`Features:MultiTenancy`** appropriately; use **`AllowDefaultTenantFallback: false`** in production unless you fully understand the risk.
2. Call **`AddTenantContextCore`** / **`AddTenantContextForWorkers`** (or use **`BaseRegister`**).
3. Derive **`DbContext`** from **`TenantAwareDbContext`**; use **`UseApplicationServiceProvider`** on EF options.
4. Mark tenant rows with **`ITenantEntity`**; set **`TenantId`** on create.
5. For each **consumer / job**, **`using (tenantScope.Begin(tenantId))`** around work that touches the database.
6. Use **`ITenantMemoryCache`** for per-tenant cache keys.
7. Set **`IntegrationEvent.TenantId`** when publishing integration events.
8. Register an **`IQueryFilterBypassEvaluator`** that allows bypass **only** where strictly needed (never for normal request handling).

---

## Reference implementation

The **`eBuildingBlocks.ReferenceApp.*`** projects demonstrate: `ReferenceDbContext`, `Order` / `TenantEntity`, outbox + MassTransit consumer with **`ITenantScope`**, and **`appsettings.json`** under `Features:MultiTenancy`.
