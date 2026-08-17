# Changelog

Entries are grouped by package below. Each package versions independently — see the
`<Version>` in that project's `.csproj` for its current release.

## eBuildingBlocks.Domain

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
