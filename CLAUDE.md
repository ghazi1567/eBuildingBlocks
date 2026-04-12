# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run Commands

```bash
# Restore dependencies
dotnet restore ./eBuildingBlocks.sln

# Build (Debug)
dotnet build ./eBuildingBlocks.sln

# Build (Release — also generates NuGet packages for each project)
dotnet build ./eBuildingBlocks.sln --configuration Release

# Run the reference app
dotnet run --project ./eBuildingBlocks.ReferenceApp.API

# Run a specific project build
dotnet build ./eBuildingBlocks.Infrastructure/eBuildingBlocks.Infrastructure.csproj
```

There are no automated test projects. Manual/integration testing is done through the reference apps (`eBuildingBlocks.ReferenceApp.API`, `eBuildingBlocks.API.Example`).

## Architecture Overview

This is a **.NET 10 modular framework** published as independent NuGet packages. It implements Clean Architecture with DDD, CQRS, and event-driven patterns for enterprise .NET applications.

### Layer Structure

| Project | Role |
|---|---|
| `eBuildingBlocks.Domain` | Base entities, domain events, repository/UoW interfaces, specifications |
| `eBuildingBlocks.Application` | Exception handling, response models, `IEventBus`/`IEventHandler<T>` abstractions |
| `eBuildingBlocks.Infrastructure` | EF Core implementations, audit interceptor, in-process event bus, outbox interceptor |
| `eBuildingBlocks.API` | ASP.NET Core extensions: versioning, Scalar/OpenAPI, JWT auth, Hangfire, CORS, health checks, metrics |
| `eBuildingBlocks.EventBus` | MassTransit integration for cross-service integration events |
| `eBuildingBlocks.Common` | UUID v7 utilities, JWT claim types, `FeatureGate`, `MultiTenancyOptions` |
| `eBuildingBlocks.SMPP` | SMS/telecom protocol support |
| `eBuildingBlocks.ReferenceApp.*` | Working reference implementation showing how all layers connect |

### Key Domain Abstractions (`eBuildingBlocks.Domain`)

- `BaseEntity<TKey>` — holds a `DomainEvents` collection; call `AddDomainEvent()` from aggregate methods
- `AuditableEntity<TKey>` — adds CreatedOn/CreatedBy/ModifiedOn/ModifiedBy
- `TenantEntity<TKey>` — adds `TenantId` for multi-tenant isolation
- `IRepository<TEntity, TKey>` — **does not include `SaveChangesAsync`**; use `IUnitOfWork` for committing
- `IEfQueryableRepository<TEntity>` — infrastructure-only port for raw `IQueryable<T>` access
- `ISpecification<T>` / `IEfSpecification<T>` — domain-side criteria vs. EF-specific Include expressions

### Event-Driven Architecture

Two distinct event channels exist:

1. **Domain Events** (in-process, same transaction)
   - Implement `IDomainEvent` (record types with `EventId`, `OccurredAt`, `TenantId`)
   - Raise via `entity.AddDomainEvent(new MyDomainEvent(...))`
   - `InProcessEventBus` dispatches to `IEventHandler<T>` implementations after `SaveChanges`
   - Configurable failure mode: `FailFast` (default) or `Continue`

2. **Integration Events** (cross-service, eventual consistency via MassTransit)
   - Inherit `IntegrationEvent` from `eBuildingBlocks.EventBus`
   - Published via `IEventPublisher`; consumed via MassTransit consumer implementations
   - Use the **transactional outbox pattern** to avoid dual-write: `DomainOutboxSaveChangesInterceptor` persists events to `OutboxMessages` table; a Hangfire job (`ReferenceHangfireOutboxBridgeJob`) processes and publishes them

### Multi-Tenancy

- Apply `ModelBuilderExtensions` global query filters in `OnModelCreating` to auto-filter by `TenantId`
- Use `TenantEntity<TKey>` as the base class for tenant-scoped aggregates
- Namespace for feature gates: `eBuildingBlocks.Common.Features` (not `eBuildingBlocks.API.Features`)

### Hosting App Setup Pattern

Consumer apps call two extension methods from `eBuildingBlocks.API`:
- `BaseRegister()` — registers all framework services (auth, versioning, health checks, Hangfire, etc.)
- `BaseAppUse()` — configures the middleware pipeline

See `eBuildingBlocks.ReferenceApp.API/Program.cs` for the canonical setup example.

### Breaking Changes (Recent)

- `FeatureGate` / `MultiTenancyOptions` → namespace `eBuildingBlocks.Common.Features`
- `IRepository` no longer exposes `SaveChangesAsync` — use `IUnitOfWork` instead

## Documentation

- `docs/HOSTING_APP_OUTBOX.md` — Wiring outbox: `IEventTypeRegistry`, EF interceptor, SQL Server processor, `IEventPublisher`
- `docs/REPOSITORY_AND_UOW.md` — `IUnitOfWork`, `AddDbContextUnitOfWork`, `IEfQueryableRepository`, specifications
- `docs/MULTI_TENANCY.md` — Multi-tenant setup guide
- `ARCHITECTURAL_REVIEW.md` — Domain event architecture specification (v2.1.0)
