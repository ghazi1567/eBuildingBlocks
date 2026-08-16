# Code Review Findings — 2026-08-17

Full-branch review of `latest-dotnet-10` (via `/code-review`). 10 findings, most severe
first. All verified/confirmed. None originate from the outbox-decoupling change in
commit `478cd4c` — these are pre-existing issues the review surfaced elsewhere in the
branch.

---

## 1. Tenant header spoofing when JWT tenant claim is missing
**File:** `eBuildingBlocks.Application/Middlewares/TenantHeaderValidationMiddleware.cs:31`
**Category:** security

Header-vs-claim spoofing check is skipped entirely when the JWT tenant claim is missing
or unparsable, and the unvalidated `X-Tenant-Id` header is then used as the authoritative
tenant.

**Failure scenario:** An authenticated request whose JWT lacks (or has a malformed)
tenant claim short-circuits to `_next()` at line 34 without comparing the header, and
`TenantResolver.TenantId` then falls back to reading `X-Tenant-Id` directly — letting an
attacker with any valid token but no tenant claim set an arbitrary
`X-Tenant-Id: <victim-tenant-guid>` header and have EF Core's global query filter scope
all data to that tenant.

---

## 2. MultiTenancy default flipped from off to on
**File:** `eBuildingBlocks.Common/Features/MultiTenancyOptions.cs:8`
**Category:** breaking-change

`MultiTenancyOptions.Enabled` default flipped from `false` (old
`eBuildingBlocks.API/Features/MultiTenancyOptions.cs`) to `true`, with no compensating
guard.

**Failure scenario:** A host that upgrades and binds `Features:MultiTenancy` from config
without an explicit `Enabled` key silently goes from multi-tenancy OFF to ON; combined
with `TenantResolver` throwing `TenantResolutionException` when no tenant can be
resolved (and `TenantAwareDbContext` applying a `TenantId` query filter otherwise),
previously-working single-tenant requests start throwing or silently returning zero rows
after upgrade.

---

## 3. DomainEventBridge removed with no migration path
**File:** `eBuildingBlocks.Infrastructure/Events/DomainEventBridge.cs:1`
**Category:** breaking-change

The catch-all `DomainEventBridge` (forwarded every domain event to `IEventPublisher` as
an integration event) was deleted with no drop-in replacement or migration note; the new
outbox path throws `InvalidOperationException` for any event type not explicitly
registered via `IEventTypeRegistry`.

**Failure scenario:** A consumer app that previously relied on `DomainEventBridge` for
automatic cross-service propagation of all domain events gets no propagation for any
event type it hasn't manually registered after upgrading — a silent drop of integration
events rather than a compile error, and undocumented in `docs/HOSTING_APP_OUTBOX.md` or
`ARCHITECTURAL_REVIEW.md`.

---

## 4. GetByIdAsync misses pending entities, hardcodes "Id"
**File:** `eBuildingBlocks.Infrastructure/Implementations/Repository.cs:56`
**Category:** correctness

`GetByIdAsync` for tenant-scoped entities replaced `FindAsync` with
`FirstOrDefaultAsync(EF.Property<TKey>(e, "Id")...)`, losing the change-tracker
short-circuit and hardcoding the literal property name `"Id"` though `IEntity`/
`ITenantEntity` guarantee no such member.

**Failure scenario:** Within one unit-of-work, adding a tenant entity then calling
`GetByIdAsync` before `SaveChangesAsync` now returns `null` instead of the pending
instance (`FirstOrDefaultAsync` always hits the DB). Separately, any `ITenantEntity`
implementer that doesn't inherit `TenantEntity<TKey>`'s `"Id"` property, or maps its key
under a different name, throws `InvalidOperationException` at runtime from
`EF.Property<TKey>(e, "Id")` despite compiling fine against the interface.

---

## 5. Sync SaveChanges() bypasses outbox interceptor
**File:** `eBuildingBlocks.Infrastructure/Outbox/DomainOutboxSaveChangesInterceptor.cs:21`
**Category:** correctness

`DomainOutboxSaveChangesInterceptor` overrides only the async `SaveChangesInterceptor`
hooks (`SavingChangesAsync`/`SavedChangesAsync`/`SaveChangesFailedAsync`); EF Core does
not forward sync `SaveChanges()` calls to these, so outbox writes and domain-event
clearing would be silently skipped for any caller using the sync API.

**Failure scenario:** Any code path (design-time tooling, a seed script, a future
consumer unaware the framework is async-only) that calls `DbContext.SaveChanges()` on a
context with this interceptor persists entity changes but never enqueues
`OutboxMessage` rows and never clears in-memory domain events — no exception, the
integration event is simply lost.

---

## 6. Domain events lost on mid-loop publish failure
**File:** `eBuildingBlocks.Infrastructure/Extensions/RepositoryExtensions.cs:47`
**Category:** correctness

`SaveChangesAndPublishDomainEventsInProcessAsync` clears domain events from tracked
entities and commits `SaveChanges` before the publish loop runs, so a mid-loop
`PublishAsync` failure permanently loses any events after the failing one with no
outbox/retry path.

**Failure scenario:** If an aggregate raises three domain events and the second event's
handler throws (e.g. transient dependency outage), event one succeeds but the third is
lost forever — no persisted record exists to retry it, since events were cleared from
the entity and the transaction already committed. Currently this method has no call
sites in the shipped code, so the risk is latent rather than active.

---

## 7. OrderBy/OrderByDescending silently mutually exclusive
**File:** `eBuildingBlocks.Infrastructure/Specifications/SpecificationEvaluator.cs:41`
**Category:** correctness

Ordering logic uses `if`/`else if` between `OrderBy` and `OrderByDescending`, so a
specification that sets both silently applies only `OrderBy` and drops
`OrderByDescending`.

**Failure scenario:** A derived specification (`SpecificationBase<T>` exposes
independent `ApplyOrderBy`/`ApplyOrderByDescending` setters with no mutual-exclusion
guard) that calls both methods gets ascending order silently instead of descending, with
no error — currently dormant since no shipped spec calls both, but a live trap for the
next one that does.

---

## 8. IgnoreQueryFilters bypass silently defeated if unwired
**File:** `eBuildingBlocks.Infrastructure/Specifications/SpecificationEvaluator.cs:19`
**Category:** reliability

`IgnoreQueryFilters` bypass resolves `IQueryFilterBypassEvaluator` via
`dbContext.GetService<T>()`, which reads EF Core's internal service provider and only
sees app-registered services if the `DbContext` was built with
`UseApplicationServiceProvider` — a requirement with no framework-level enforcement.

**Failure scenario:** Any host that registers its `DbContext` without calling
`UseApplicationServiceProvider(sp)` always gets `bypass == null`, so every
`ISpecification.IgnoreQueryFilters=true` spec throws `InvalidOperationException` even if
that host correctly registered a permissive `IQueryFilterBypassEvaluator` for an
admin/migration scenario — the extension point is silently defeated. Only the reference
app happens to wire this correctly today.

---

## 9. Outbox claim query hardcoded to SQL Server T-SQL
**File:** `eBuildingBlocks.Infrastructure/Outbox/SqlServerOutboxBatchClaim.cs:11`
**Category:** reliability

The generic `AddOutboxProcessor<TDbContext>()` framework API hardcodes SQL-Server-only
T-SQL (`WITH (UPDLOCK, READPAST, ROWLOCK)`, `OUTPUT inserted.*`) with no provider check,
so a non-SQL-Server `DbContext` fails at every poll instead of at startup.

**Failure scenario:** A consumer registers `AddOutboxProcessor<TDbContext>()` against a
Postgres/MySQL EF Core provider; the claim query throws a SQL syntax exception on every
poll cycle inside `ProcessOnceAsync`'s try/catch, which logs it as a transient DB error
and retries forever, rather than failing fast at startup with a clear
unsupported-provider message.

---

## 10. Outbox processor issues one SaveChanges per message
**File:** `eBuildingBlocks.Infrastructure/Outbox/OutboxProcessorBackgroundService.cs:141`
**Category:** efficiency

`ProcessOnceAsync` calls `SaveChangesAsync` once per message inside the batch loop (via
`PublishSingleAsync`/`RecordFailureAsync`/`MarkUnknownEventNamePoisonAsync`) instead of
batching all updates into a single `SaveChangesAsync` after the loop.

**Failure scenario:** With `BatchSize=N` claimed outbox messages, a single poll cycle
issues N separate `SaveChangesAsync` round-trips to SQL Server instead of 1, multiplying
connection/transaction overhead and tail latency under load with a large outbox
backlog.
