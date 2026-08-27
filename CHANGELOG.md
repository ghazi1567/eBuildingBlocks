# Changelog

Entries are grouped by package below. Each package versions independently — see the
`<Version>` in that project's `.csproj` for its current release.

## eBuildingBlocks.Domain

### 3.2.0
- Added `eBuildingBlocks.Domain.Interfaces.IAuditableEntity` — a non-generic marker
  (`CreatedOn`/`CreatedBy`/`ModifiedOn`/`ModifiedBy`, `SetCreated`/`SetModified`) now
  implemented by `AuditableEntity<TKey>`. Purely additive; no existing member changed.
  Added so infrastructure code can query audit-stamped entities regardless of their key
  type — see the `eBuildingBlocks.Infrastructure` 5.0.0 entry below for why.

### 3.0.1
- Removed the unused `ProjectReference` to `eBuildingBlocks.Common` (dead reference;
  no public API change).

## eBuildingBlocks.Common

### 3.1.0
- Added `eBuildingBlocks.Common.Outbox.IOutboxIntegrationPublisher` — a broker-agnostic
  contract used by the transactional outbox processor to publish integration events.

## eBuildingBlocks.EventBus

### 3.1.0
- `EventPublisher` now also implements `IOutboxIntegrationPublisher` (no new methods;
  `PublishAsync<T>` already satisfied the contract).
- `AddIntegrationMassTransit` now registers `IOutboxIntegrationPublisher` alongside
  `IEventPublisher`, pointing at the same instance. No code changes needed for existing
  consumers.

## eBuildingBlocks.Infrastructure

### 5.0.0 — Breaking
- Fixed: `AuditSaveChangesInterceptor` used to query
  `ChangeTracker.Entries<AuditableEntity<Guid>>()`, so any entity keyed by `int`,
  `long`, or `string` was silently never audit-stamped. It now queries the non-generic
  `IAuditableEntity` (from `eBuildingBlocks.Domain` 3.2.0) instead, so all key types are
  stamped. Not marked breaking on its own — this is a bugfix — but bundled into this
  major version alongside the change below. If you depended on the old (buggy)
  behavior of non-`Guid`-keyed entities never being stamped, that no longer holds.
- **Breaking:** `Repository<TEntity, TKey, TDbContext>` no longer inherits
  `UnitOfWork<TDbContext>`. It now composes the `DbContext` directly instead of
  exposing it through an IS-A relationship that was never architecturally correct — a
  repository is not a unit of work. `SaveChangesAsync`, `BeginTransactionAsync`,
  `ExecuteSqlAsync`, and the generic `Entities<TEntity>()` helper are no longer public
  members of `Repository<,,>`. Use `IUnitOfWork` (e.g. `DbContextUnitOfWork<TDbContext>`,
  registered separately via `AddDbContextUnitOfWork`) for `SaveChangesAsync`, as the docs
  already recommended. Nothing in this repository's own code (reference apps, docs
  examples) called these members directly on a repository instance — only consumers who
  cast a repository to its concrete `Repository<,,>` type and called these methods
  directly are affected.

### 4.0.0 — Breaking
- Removed the deprecated `IEventPublisher` fallback in the outbox processor
  (deprecated since 3.1.0). `IOutboxIntegrationPublisher` must now be registered
  explicitly before starting the outbox processor.
- Removed the `ProjectReference` to `eBuildingBlocks.EventBus`. Consumers who use
  the outbox + MassTransit combination and were relying on the transitive
  MassTransit/RabbitMQ.Client dependency must now reference
  `eBuildingBlocks.EventBus` directly.
- Also removed a direct, unused `RabbitMQ.Client` `PackageReference` that predated
  this effort and was independent of the `EventBus` reference — nothing in
  `Infrastructure`'s own code referenced it. Without removing it, `RabbitMQ.Client`
  would still have appeared in `Infrastructure`'s package graph despite the
  `EventBus` reference being gone.
- See [docs/MIGRATION_OUTBOX_PUBLISHER.md](docs/MIGRATION_OUTBOX_PUBLISHER.md) for the
  full migration path.

### 3.1.0
- The outbox processor now prefers `IOutboxIntegrationPublisher` over the
  MassTransit-specific `IEventPublisher`, with a backward-compatible fallback. See
  **Deprecations** below.

## Deprecations

### `IEventPublisher` fallback in the outbox processor
- **Introduced in:** `eBuildingBlocks.Infrastructure` 3.1.0
- **Deprecated in:** `eBuildingBlocks.Infrastructure` 3.1.0
- **Removed in:** `eBuildingBlocks.Infrastructure` 4.0.0

`OutboxProcessorBackgroundService` now resolves the broker-agnostic
`eBuildingBlocks.Common.Outbox.IOutboxIntegrationPublisher` first. If only the
MassTransit-specific `IEventPublisher` (from `eBuildingBlocks.EventBus`) is registered,
the processor falls back to it via an internal `LegacyEventPublisherAdapter` and logs a
one-time warning per process. Consumers calling `AddIntegrationMassTransit` (from
`eBuildingBlocks.EventBus` 3.1.0+) get `IOutboxIntegrationPublisher` registered
automatically with no code changes required.

See [docs/MIGRATION_OUTBOX_PUBLISHER.md](docs/MIGRATION_OUTBOX_PUBLISHER.md) for the
full migration guide. The `IEventPublisher` fallback and the `Infrastructure ->
EventBus` project reference will be removed together in the next major version.
