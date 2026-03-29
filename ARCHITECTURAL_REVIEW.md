# eBuildingBlocks Enhancement Proposal - Architectural Review

**Review Date**: 2024  
**Reviewer**: Architecture Review  
**Current Version**: 2.0.0  
**Proposed Version**: 2.1.0

---

## Quick Reference

| Item | Status | Priority | Breaking Change |
|------|--------|----------|-----------------|
| Enhance `IDomainEvent` interface | ✅ Approved | P0 | ⚠️ Yes (safe - no existing impls) |
| Create `BaseDomainEvent` class | ✅ Approved | P0 | No |
| Create `IEventHandler<T>` interface | ✅ Approved | P0 | No |
| Create `IEventBus` interface | ✅ Approved | P0 | No |
| Create `InProcessEventBus` | ✅ Approved | P0 | No |
| Repository event publishing extension | ✅ Approved | P1 | No |
| `IEventPublisher` interface | ❌ Rejected | - | - |
| `EventPublisherEntity` class | ❌ Rejected | - | - |

**Overall Recommendation**: ✅ **APPROVED FOR IMPLEMENTATION**

---

## Executive Summary

This document provides an architectural review of the proposed domain event enhancements for eBuildingBlocks. The review analyzes the current implementation, identifies gaps, overlaps, and provides a final recommended change list that aligns with existing architecture patterns while addressing the enhancement requirements.

**Key Finding**: The codebase already has foundational domain event infrastructure in `BaseEntity<TKey>`, but lacks the event bus and handler infrastructure needed for event-driven architecture in modular monoliths.

---

## 1. Current State Analysis

### 1.1 Existing Domain Event Infrastructure

**✅ Already Implemented:**
- `IDomainEvent` interface exists in `eBuildingBlocks.Domain.Models.BaseEntity` (line 82)
  - Currently a marker interface with no properties
- `BaseEntity<TKey>` includes domain event collection:
  - `DomainEvents` property (IReadOnlyCollection<IDomainEvent>)
  - `AddDomainEvent()` method (protected)
  - `RemoveDomainEvent()` method (protected)
  - `ClearDomainEvents()` method (public)
- `AuditableEntity<TKey>` exists and inherits from `BaseEntity<TKey>`
- `TenantEntity<TKey>` exists and inherits from `AuditableEntity<TKey>`

**❌ Missing Components:**
- Enhanced `IDomainEvent` with `EventId`, `OccurredAt`, `TenantId` properties
- `BaseDomainEvent` abstract class for convenience
- `IEventHandler<TEvent>` interface for type-safe event handling
- `IEventBus` interface for event publishing abstraction
- `InProcessEventBus` implementation for modular monolith scenarios
- Automatic event publishing from repository `SaveChangesAsync`
- Integration between domain events and the existing EventBus infrastructure

### 1.2 Existing EventBus Project

**Current Implementation:**
- `eBuildingBlocks.EventBus` project exists
- Uses MassTransit for integration events (microservices communication)
- Contains `IEventPublisher` and `IEventSubscriber` interfaces
- Designed for **integration events** (cross-service communication)
- **Not designed for domain events** (in-process, same transaction)

**⚠️ Issues Identified:**
- Namespace inconsistency: Uses `BuildingBlocks.EventBus` instead of `eBuildingBlocks.EventBus`
- Separation of concerns: Integration events vs Domain events should be distinct

### 1.3 Repository Pattern

**Current Implementation:**
- `IRepository<TEntity, TKey>` / `IReadRepository<TEntity, TKey>` exist; **`SaveChangesAsync` is on `IUnitOfWork`**, not on `IRepository`
- `Repository<TEntity, TKey, TDbContext>` implements `IRepository` and **`IEfQueryableRepository<TEntity, TKey>`** for **`Query()`** / `IQueryable<T>` (Infrastructure-only port)
- Domain **`ISpecification<T>`** is EF-free (criteria, **string** includes, ordering, paging via **`SpecificationBase<T>`**); EF expression/chained includes use **`IEfSpecification<T>`** / **`EfSpecification<T>`** and **`SpecificationEvaluator`** in Infrastructure
- `UnitOfWork<TDbContext>` provides `SaveChangesAsync()`
- No automatic domain event publishing after `SaveChangesAsync()` (transactional outbox is documented separately; outbox rows use **`EventName`**, not a legacy `EventType` column)

---

## 2. Proposal Analysis

### 2.1 Strengths

1. **Clear Separation**: Proposal distinguishes domain events from integration events
2. **Backward Compatible**: All changes are additive
3. **Modular Monolith Focus**: In-process event bus is appropriate for current needs
4. **Future-Proof**: Design allows evolution to microservices

### 2.2 Gaps & Overlaps

#### Overlaps with Existing Code

1. **IEventPublisher Interface (Proposal Section 4.1)**
   - **Issue**: `BaseEntity<TKey>` already provides event collection functionality
   - **Recommendation**: Do NOT create separate `IEventPublisher` interface
   - **Rationale**: The existing `BaseEntity` pattern is cleaner and more DDD-aligned

2. **EventPublisherEntity Base Class (Proposal Section 4.2)**
   - **Issue**: `BaseEntity<TKey>` already has domain event support
   - **Recommendation**: Do NOT create `EventPublisherEntity`
   - **Rationale**: Redundant - entities should inherit from `BaseEntity<TKey>` or `AuditableEntity<TKey>`

#### Missing from Proposal

1. **Integration with Existing EventBus**
   - Proposal doesn't address how domain events can become integration events
   - Need bridge pattern for domain → integration event transformation

2. **Error Handling**
   - No strategy for handling event handler failures
   - Should events be published if handlers fail?

3. **Transaction Boundaries**
   - Proposal publishes events after SaveChanges, but what if SaveChanges fails?
   - Need clear transaction semantics

4. **Event Ordering**
   - No guarantee of event ordering
   - May be important for some use cases

---

## 3. Recommended Changes

### 3.1 Phase 1: Core Domain Event Infrastructure (MUST HAVE)

#### 3.1.1 Enhance IDomainEvent Interface

**Location**: `eBuildingBlocks.Domain/Models/BaseEntity.cs` (modify existing)

**Change**: Enhance the existing `IDomainEvent` interface with required properties:

```csharp
namespace eBuildingBlocks.Domain.Models
{
    /// <summary>
    /// Marker interface for domain events with standard properties.
    /// All domain events must implement this interface.
    /// </summary>
    public interface IDomainEvent
    {
        /// <summary>
        /// Unique identifier for the event instance.
        /// </summary>
        Guid EventId { get; }
        
        /// <summary>
        /// Timestamp when the event occurred.
        /// </summary>
        DateTime OccurredAt { get; }
        
        /// <summary>
        /// Tenant identifier (for multi-tenant systems).
        /// </summary>
        Guid? TenantId { get; }
    }
}
```

**Breaking Change**: ⚠️ **YES** - This is a breaking change for any existing code that implements `IDomainEvent`

**Mitigation Strategy**:
- Provide `BaseDomainEvent` abstract class with default implementations
- Update version to 3.0.0 if breaking change is unacceptable, OR
- Create `IDomainEventV2` and deprecate `IDomainEvent` (not recommended)

**Recommendation**: Accept breaking change for 2.1.0 if no existing implementations, otherwise use migration path below.

#### 3.1.2 Create BaseDomainEvent Abstract Class

**Location**: `eBuildingBlocks.Domain/Models/BaseDomainEvent.cs` (NEW)

**Purpose**: Reduce boilerplate in event definitions

```csharp
namespace eBuildingBlocks.Domain.Models
{
    /// <summary>
    /// Base class for domain events with common properties.
    /// </summary>
    public abstract class BaseDomainEvent : IDomainEvent
    {
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTime OccurredAt { get; } = DateTime.UtcNow;
        public Guid? TenantId { get; set; }
        
        protected BaseDomainEvent()
        {
        }
        
        protected BaseDomainEvent(Guid? tenantId)
        {
            TenantId = tenantId;
        }
    }
}
```

**Breaking Change**: No

---

#### 3.1.3 Create IEventHandler Interface

**Location**: `eBuildingBlocks.Application/Events/IEventHandler.cs` (NEW)

**Purpose**: Type-safe event handling interface

```csharp
namespace eBuildingBlocks.Application.Events
{
    /// <summary>
    /// Handler interface for domain events.
    /// </summary>
    /// <typeparam name="TEvent">The type of domain event to handle.</typeparam>
    public interface IEventHandler<in TEvent> where TEvent : IDomainEvent
    {
        /// <summary>
        /// Handles the domain event.
        /// </summary>
        /// <param name="event">The domain event to handle.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Task representing the async operation.</returns>
        Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
    }
}
```

**Breaking Change**: No

---

#### 3.1.4 Create IEventBus Interface

**Location**: `eBuildingBlocks.Application/Events/IEventBus.cs` (NEW)

**Purpose**: Abstraction for event publishing (allows different implementations)

```csharp
namespace eBuildingBlocks.Application.Events
{
    /// <summary>
    /// Event bus interface for publishing domain events.
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// Publishes a domain event to all registered handlers.
        /// </summary>
        /// <typeparam name="TEvent">The type of domain event.</typeparam>
        /// <param name="event">The domain event to publish.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Task representing the async operation.</returns>
        Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) 
            where TEvent : IDomainEvent;
    }
}
```

**Note**: Removed `Subscribe` method - DI handles registration automatically

**Breaking Change**: No

---

#### 3.1.5 Create InProcessEventBus Implementation

**Location**: `eBuildingBlocks.Infrastructure/Events/InProcessEventBus.cs` (NEW)

**Purpose**: In-process event bus for modular monolith

```csharp
namespace eBuildingBlocks.Infrastructure.Events
{
    /// <summary>
    /// In-process event bus implementation for modular monoliths.
    /// Uses dependency injection to resolve event handlers.
    /// </summary>
    public class InProcessEventBus : IEventBus
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<InProcessEventBus> _logger;
        
        public InProcessEventBus(
            IServiceProvider serviceProvider,
            ILogger<InProcessEventBus> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }
        
        public async Task PublishAsync<TEvent>(
            TEvent @event, 
            CancellationToken cancellationToken = default) 
            where TEvent : IDomainEvent
        {
            if (@event == null)
                throw new ArgumentNullException(nameof(@event));
            
            var handlerType = typeof(IEventHandler<>).MakeGenericType(typeof(TEvent));
            var handlers = _serviceProvider.GetServices(handlerType);
            
            if (!handlers.Any())
            {
                _logger.LogDebug("No handlers found for event type {EventType}", typeof(TEvent).Name);
                return;
            }
            
            var tasks = handlers.Select(handler => 
            {
                var method = handlerType.GetMethod("HandleAsync");
                return (Task)method!.Invoke(handler, new object[] { @event, cancellationToken })!;
            });
            
            await Task.WhenAll(tasks);
        }
    }
}
```

**Breaking Change**: No

**Enhancement**: Add error handling strategy (continue on error vs fail fast)

---

### 3.2 Phase 2: Repository Integration (SHOULD HAVE)

#### 3.2.1 Create Repository Event Publishing Extension

**Location**: `eBuildingBlocks.Infrastructure/Extensions/RepositoryExtensions.cs` (NEW)

**Purpose**: Automatic event publishing after successful SaveChanges

```csharp
namespace eBuildingBlocks.Infrastructure.Extensions
{
    public static class RepositoryExtensions
    {
        /// <summary>
        /// Saves changes and publishes domain events from all entities.
        /// </summary>
        public static async Task<int> SaveChangesAndPublishEventsAsync<TContext>(
            this DbContext context,
            IEventBus eventBus,
            CancellationToken cancellationToken = default)
            where TContext : DbContext
        {
            // Collect events from all entities
            var entities = context.ChangeTracker
                .Entries<BaseEntity<object>>()
                .Where(e => e.Entity.DomainEvents.Any())
                .Select(e => e.Entity)
                .ToList();
            
            var events = entities
                .SelectMany(e => e.DomainEvents)
                .ToList();
            
            // Save changes first (transaction)
            var result = await context.SaveChangesAsync(cancellationToken);
            
            // Publish events after successful save
            foreach (var @event in events)
            {
                try
                {
                    // Use reflection to call PublishAsync with correct generic type
                    var eventType = @event.GetType();
                    var method = typeof(IEventBus).GetMethod("PublishAsync")!
                        .MakeGenericMethod(eventType);
                    await (Task)method.Invoke(eventBus, new object[] { @event, cancellationToken })!;
                }
                catch (Exception ex)
                {
                    // Log error but don't fail the transaction
                    // Consider implementing dead letter queue or retry mechanism
                    throw;
                }
            }
            
            // Clear events
            foreach (var entity in entities)
            {
                entity.ClearDomainEvents();
            }
            
            return result;
        }
    }
}
```

**Breaking Change**: No (extension method, optional usage)

**Alternative**: Override `SaveChangesAsync` in `UnitOfWork` to automatically publish events

---

#### 3.2.2 Enhance UnitOfWork (Alternative Approach)

**Location**: `eBuildingBlocks.Infrastructure/Implementations/UnitOfWork.cs` (MODIFY)

**Alternative**: Inject `IEventBus` into `UnitOfWork` and automatically publish events

```csharp
public class UnitOfWork<TDbContext> where TDbContext : DbContext
{
    private readonly TDbContext _dbContext;
    private readonly IEventBus? _eventBus;
    
    public UnitOfWork(TDbContext dbContext, IEventBus? eventBus = null)
    {
        _dbContext = dbContext;
        _eventBus = eventBus;
    }
    
    public virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var result = await _dbContext.SaveChangesAsync(cancellationToken);
        
        if (_eventBus != null)
        {
            await PublishDomainEventsAsync(cancellationToken);
        }
        
        return result;
    }
    
    private async Task PublishDomainEventsAsync(CancellationToken cancellationToken)
    {
        // Implementation similar to extension method
    }
}
```

**Recommendation**: Use extension method approach for flexibility

---

### 3.3 Phase 3: Integration & Polish (NICE TO HAVE)

#### 3.3.1 Fix EventBus Namespace Inconsistency

**Location**: `eBuildingBlocks.EventBus/**/*.cs` (MODIFY ALL)

**Change**: Update all namespaces from `BuildingBlocks.EventBus` to `eBuildingBlocks.EventBus`

**Files Affected**:
- `Events/IEventPublisher.cs`
- `Events/IEventSubscriber.cs`
- `Events/EventPublisher.cs`
- `Events/EventSubscriber.cs`
- `Events/MassTransitExtensions.cs`
- `Contracts/IntegrationEvent.cs`

**Breaking Change**: ⚠️ **YES** - Breaking change for consumers

**Recommendation**: Do in separate version (2.2.0) or major version bump

---

#### 3.3.2 Create Domain-to-Integration Event Bridge

**Location**: `eBuildingBlocks.Infrastructure/Events/DomainEventBridge.cs` (NEW)

**Purpose**: Allow domain events to be published as integration events when needed

```csharp
namespace eBuildingBlocks.Infrastructure.Events
{
    /// <summary>
    /// Bridge to publish domain events as integration events.
    /// Useful when evolving from modular monolith to microservices.
    /// </summary>
    public class DomainEventBridge : IEventHandler<IDomainEvent>
    {
        private readonly IEventPublisher _integrationEventPublisher;
        
        public async Task HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            // Transform domain event to integration event
            var integrationEvent = new IntegrationEvent
            {
                EventId = domainEvent.EventId,
                OccurredAt = domainEvent.OccurredAt,
                Payload = domainEvent
            };
            
            await _integrationEventPublisher.PublishAsync(integrationEvent, cancellationToken);
        }
    }
}
```

**Breaking Change**: No

---

#### 3.3.3 Add Error Handling Strategy

**Enhancement**: Add configuration for error handling behavior

```csharp
public class EventBusOptions
{
    public EventHandlerFailureMode FailureMode { get; set; } = EventHandlerFailureMode.Continue;
}

public enum EventHandlerFailureMode
{
    Continue,  // Log error, continue with other handlers
    FailFast   // Stop on first error
}
```

---

## 4. Final Recommended Change List

### 4.1 Must Have (Phase 1) - Version 2.1.0

| # | Component | Type | Location | Breaking Change | Priority |
|---|-----------|------|----------|-----------------|----------|
| 1 | Enhance `IDomainEvent` interface | Modify | `Domain/Models/BaseEntity.cs` | ⚠️ Yes* | P0 |
| 2 | Create `BaseDomainEvent` class | New | `Domain/Models/BaseDomainEvent.cs` | No | P0 |
| 3 | Create `IEventHandler<T>` interface | New | `Application/Events/IEventHandler.cs` | No | P0 |
| 4 | Create `IEventBus` interface | New | `Application/Events/IEventBus.cs` | No | P0 |
| 5 | Create `InProcessEventBus` implementation | New | `Infrastructure/Events/InProcessEventBus.cs` | No | P0 |
| 6 | Add DI registration extension | New | `Infrastructure/Extensions/EventBusExtensions.cs` | No | P0 |

**Note on Breaking Change #1**: 
- ✅ **VERIFIED**: No existing `IDomainEvent` implementations found in codebase
- **Decision**: Accept breaking change - safe to enhance interface
- **Mitigation**: Provide `BaseDomainEvent` abstract class for easy adoption

---

### 4.2 Should Have (Phase 2) - Version 2.1.0

| # | Component | Type | Location | Breaking Change | Priority |
|---|-----------|------|----------|-----------------|----------|
| 7 | Create `SaveChangesAndPublishEventsAsync` extension | New | `Infrastructure/Extensions/RepositoryExtensions.cs` | No | P1 |
| 8 | Update documentation with usage examples | New | `README.md` updates | No | P1 |

---

### 4.3 Nice to Have (Phase 3) - Version 2.2.0

| # | Component | Type | Location | Breaking Change | Priority |
|---|-----------|------|----------|-----------------|----------|
| 9 | Fix EventBus namespace inconsistency | Modify | `EventBus/**/*.cs` | ⚠️ Yes | P2 |
| 10 | Create domain-to-integration event bridge | New | `Infrastructure/Events/DomainEventBridge.cs` | No | P2 |
| 11 | Add error handling strategy configuration | New | `Application/Events/EventBusOptions.cs` | No | P2 |
| 12 | Add comprehensive unit tests | New | `Tests/**/*.cs` | No | P2 |

---

## 5. Rejected Proposals

> **📋 Quick Reference**: For a detailed guide with code examples and alternatives, see [REJECTED_PROPOSALS.md](./REJECTED_PROPOSALS.md)

### 5.1 IEventPublisher Interface (Proposal Section 4.1)

**Status**: ❌ **REJECTED**

**Reason**: 
- `BaseEntity<TKey>` already provides event collection functionality
- Adding separate interface creates confusion and duplication
- Current pattern is more DDD-aligned (events belong to entities)

**Alternative**: Use existing `BaseEntity<TKey>.AddDomainEvent()` method

---

### 5.2 EventPublisherEntity Base Class (Proposal Section 4.2)

**Status**: ❌ **REJECTED**

**Reason**:
- `BaseEntity<TKey>` already has domain event support
- `AuditableEntity<TKey>` already inherits from `BaseEntity<TKey>`
- Creating another base class adds unnecessary complexity

**Alternative**: Entities should inherit from:
- `BaseEntity<TKey>` (if no audit needed)
- `AuditableEntity<TKey>` (if audit needed)
- `TenantEntity<TKey>` (if tenant + audit needed)

All of these already support domain events.

---

### 5.3 Subscribe Method in IEventBus

**Status**: ❌ **REJECTED**

**Reason**:
- In-process bus uses DI for handler resolution
- Explicit subscription not needed
- Future message broker implementations can use different interface

**Alternative**: Handler registration via DI:
```csharp
services.AddScoped<IEventHandler<ProductCreatedEvent>, ProductCreatedEventHandler>();
```

---

## 6. Architecture Decisions

### 6.1 Domain Events vs Integration Events

**Decision**: Keep them separate

- **Domain Events**: In-process, same transaction, immediate consistency
- **Integration Events**: Cross-service, eventual consistency, via message broker

**Rationale**: Different concerns, different guarantees, different implementations

---

### 6.2 Event Publishing Strategy

**Decision**: Publish events AFTER successful SaveChanges

**Rationale**:
- Ensures data consistency (events only published if transaction succeeds)
- Prevents event loss if transaction fails
- Aligns with transactional outbox pattern principles

**Trade-off**: If event handler fails, data is already saved (eventual consistency)

---

### 6.3 Error Handling Strategy

**Decision**: Fail fast by default, make configurable

**Rationale**:
- Failures in event handlers indicate serious issues
- Better to fail early than silently ignore
- Can be configured per application needs

---

## 7. Migration Guide

### 7.1 For Existing Code Using IDomainEvent

If breaking change is accepted for `IDomainEvent`:

```csharp
// OLD (if it exists)
public class ProductCreatedEvent : IDomainEvent
{
    // Custom properties
}

// NEW
public record ProductCreatedEvent(
    Guid ProductId,
    string ProductCode,
    string Name,
    Guid TenantId
) : BaseDomainEvent(TenantId);
```

### 7.2 For New Code

```csharp
// 1. Define event
public record ProductCreatedEvent(
    Guid ProductId,
    string ProductCode,
    string Name,
    Guid TenantId
) : BaseDomainEvent(TenantId);

// 2. Publish in entity
public class Product : AuditableEntity<Guid>
{
    public void Create(string code, string name)
    {
        // Business logic...
        AddDomainEvent(new ProductCreatedEvent(Id, code, name, TenantId));
    }
}

// 3. Create handler
public class ProductCreatedEventHandler : IEventHandler<ProductCreatedEvent>
{
    public async Task HandleAsync(ProductCreatedEvent @event, CancellationToken ct)
    {
        // Handle event
    }
}

// 4. Register services
services.AddScoped<IEventBus, InProcessEventBus>();
services.AddScoped<IEventHandler<ProductCreatedEvent>, ProductCreatedEventHandler>();

// 5. Use in repository
await _context.SaveChangesAndPublishEventsAsync(_eventBus, cancellationToken);
```

---

## 8. Testing Strategy

### 8.1 Unit Tests Required

1. `BaseDomainEvent` property initialization
2. `InProcessEventBus` handler resolution and invocation
3. `RepositoryExtensions` event collection and publishing
4. Error handling in event bus

### 8.2 Integration Tests Required

1. End-to-end event publishing and handling flow
2. Multiple handlers for same event
3. Event publishing after SaveChanges
4. Transaction rollback scenarios

---

## 9. Documentation Requirements

1. **Getting Started Guide**: How to use domain events
2. **Event Handler Examples**: Common patterns
3. **Best Practices**: When to use events vs direct calls
4. **Migration Guide**: From direct calls to events (if applicable)
5. **Architecture Decision Records**: Document key decisions

---

## 10. Version Strategy

### ✅ Selected: Option A - Accept Breaking Change

- **Version**: 2.1.0 (minor bump)
- **Breaking Change**: `IDomainEvent` interface enhancement
- **Migration**: Provide `BaseDomainEvent` to ease migration
- **Verification**: ✅ No existing `IDomainEvent` implementations found in codebase
- **Risk**: Low - no impact on existing code

---

## 11. Summary

### Approved Changes (Phase 1 - Must Have)

✅ Enhance `IDomainEvent` interface with properties  
✅ Create `BaseDomainEvent` abstract class  
✅ Create `IEventHandler<T>` interface  
✅ Create `IEventBus` interface  
✅ Create `InProcessEventBus` implementation  
✅ Add DI registration extensions  

### Approved Changes (Phase 2 - Should Have)

✅ Create `SaveChangesAndPublishEventsAsync` extension method  
✅ Update documentation  

### Rejected Changes

❌ `IEventPublisher` interface (redundant)  
❌ `EventPublisherEntity` base class (redundant)  
❌ `Subscribe` method in `IEventBus` (not needed for DI-based approach)  

### Deferred Changes (Phase 3)

⏸️ Fix EventBus namespace inconsistency (breaking change, defer to 2.2.0)  
⏸️ Domain-to-integration event bridge  
⏸️ Error handling strategy configuration  

---

## 12. Next Steps

1. ✅ **Verify Breaking Change Impact**: COMPLETED - No existing implementations found
2. **Implement Phase 1**: Core domain event infrastructure
3. **Implement Phase 2**: Repository integration
4. **Write Tests**: Unit and integration tests
5. **Update Documentation**: README and examples
6. **Create Migration Guide**: For new adopters (no migration needed for existing code)

---

**Document Status**: ✅ **APPROVED FOR IMPLEMENTATION**  
**Implementation Priority**: **HIGH**  
**Estimated Effort**: 2-3 days for Phase 1, 1 day for Phase 2
