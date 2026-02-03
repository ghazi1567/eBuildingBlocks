# Rejected Proposals - Alternatives Guide

This document lists all proposals that were rejected during the architectural review, along with recommended alternatives.

---

## 1. IEventPublisher Interface

**Proposal Section**: 4.1  
**Status**: ❌ **REJECTED**

### What Was Proposed

```csharp
public interface IEventPublisher
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void AddDomainEvent(IDomainEvent domainEvent);
    void RemoveDomainEvent(IDomainEvent domainEvent);
    void ClearDomainEvents();
}
```

### Why It Was Rejected

- `BaseEntity<TKey>` already provides event collection functionality
- Adding separate interface creates confusion and duplication
- Current pattern is more DDD-aligned (events belong to entities)
- Would require entities to implement an additional interface unnecessarily

### ✅ Alternative: Use Existing BaseEntity Pattern

**Use the existing domain event methods in `BaseEntity<TKey>`:**

```csharp
// ✅ CORRECT - Use existing BaseEntity methods
public class Product : AuditableEntity<Guid>
{
    public void Create(string code, string name)
    {
        // Business logic...
        
        // Add domain event using existing method
        AddDomainEvent(new ProductCreatedEvent(Id, code, name, TenantId));
    }
}

// ❌ DON'T - Don't create separate IEventPublisher interface
public class Product : AuditableEntity<Guid>, IEventPublisher  // Unnecessary
{
    // ...
}
```

**Key Points:**
- All entities inheriting from `BaseEntity<TKey>` already have:
  - `DomainEvents` property (read-only collection)
  - `AddDomainEvent()` method (protected)
  - `RemoveDomainEvent()` method (protected)
  - `ClearDomainEvents()` method (public)
- No additional interface needed
- Works with `AuditableEntity<TKey>` and `TenantEntity<TKey>` (they inherit from `BaseEntity<TKey>`)

---

## 2. EventPublisherEntity Base Class

**Proposal Section**: 4.2  
**Status**: ❌ **REJECTED**

### What Was Proposed

```csharp
public abstract class EventPublisherEntity<TKey> : AuditableEntity<TKey>, IEventPublisher
    where TKey : notnull
{
    // Domain event collection implementation
}
```

### Why It Was Rejected

- `BaseEntity<TKey>` already has domain event support
- `AuditableEntity<TKey>` already inherits from `BaseEntity<TKey>` and has event support
- `TenantEntity<TKey>` already inherits from `AuditableEntity<TKey>` and has event support
- Creating another base class adds unnecessary complexity and inheritance depth
- Would create confusion about which base class to use

### ✅ Alternative: Use Existing Entity Hierarchy

**Choose the appropriate base class based on your needs:**

```csharp
// ✅ Option 1: BaseEntity - If you only need domain events and identity
public class Product : BaseEntity<Guid>
{
    public string Name { get; set; }
    
    public void Create(string name)
    {
        Name = name;
        AddDomainEvent(new ProductCreatedEvent(Id, Name));  // ✅ Already available
    }
}

// ✅ Option 2: AuditableEntity - If you need audit fields + domain events
public class Product : AuditableEntity<Guid>
{
    public string Name { get; set; }
    
    public void Create(string name)
    {
        Name = name;
        AddDomainEvent(new ProductCreatedEvent(Id, Name));  // ✅ Already available
    }
}

// ✅ Option 3: TenantEntity - If you need tenant + audit + domain events
public class Product : TenantEntity<Guid>
{
    public string Name { get; set; }
    
    public void Create(string name)
    {
        Name = name;
        AddDomainEvent(new ProductCreatedEvent(Id, Name, TenantId));  // ✅ Already available
    }
}

// ❌ DON'T - Don't create EventPublisherEntity
public class Product : EventPublisherEntity<Guid>  // Unnecessary - redundant
{
    // ...
}
```

**Entity Hierarchy (All Support Domain Events):**
```
BaseEntity<TKey>
  ├── DomainEvents property ✅
  ├── AddDomainEvent() method ✅
  ├── RemoveDomainEvent() method ✅
  └── ClearDomainEvents() method ✅
  
  └── AuditableEntity<TKey> (inherits from BaseEntity)
      ├── All domain event methods ✅
      └── Audit fields (CreatedOn, CreatedBy, etc.)
      
      └── TenantEntity<TKey> (inherits from AuditableEntity)
          ├── All domain event methods ✅
          ├── All audit fields ✅
          └── TenantId property
```

**Decision Matrix:**

| Requirements | Use This Base Class |
|-------------|-------------------|
| Domain events only | `BaseEntity<TKey>` |
| Domain events + Audit | `AuditableEntity<TKey>` |
| Domain events + Audit + Tenant | `TenantEntity<TKey>` |

---

## 3. Subscribe Method in IEventBus

**Proposal Section**: 3.1 (IEventBus interface)  
**Status**: ❌ **REJECTED**

### What Was Proposed

```csharp
public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) 
        where TEvent : IDomainEvent;
    
    void Subscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : IDomainEvent;  // ❌ Rejected
}
```

### Why It Was Rejected

- In-process bus uses Dependency Injection (DI) for handler resolution
- Explicit subscription not needed - handlers are automatically discovered via DI
- Future message broker implementations can use different interface/pattern
- Manual subscription adds complexity and potential for errors
- DI-based approach is more testable and follows .NET best practices

### ✅ Alternative: Use Dependency Injection for Handler Registration

**Register handlers via DI container (automatic discovery):**

```csharp
// ✅ CORRECT - Register handlers via DI
public void ConfigureServices(IServiceCollection services)
{
    // Register event bus
    services.AddScoped<IEventBus, InProcessEventBus>();
    
    // Register event handlers (DI automatically resolves them)
    services.AddScoped<IEventHandler<ProductCreatedEvent>, ProductCreatedEventHandler>();
    services.AddScoped<IEventHandler<ProductUpdatedEvent>, ProductUpdatedEventHandler>();
    services.AddScoped<IEventHandler<OrderCreatedEvent>, OrderCreatedEventHandler>();
    
    // Or use assembly scanning for automatic registration
    services.Scan(scan => scan
        .FromAssemblyOf<ProductCreatedEventHandler>()
        .AddClasses(classes => classes.AssignableTo(typeof(IEventHandler<>)))
        .AsImplementedInterfaces()
        .WithScopedLifetime());
}

// ✅ Handler implementation
public class ProductCreatedEventHandler : IEventHandler<ProductCreatedEvent>
{
    public async Task HandleAsync(ProductCreatedEvent @event, CancellationToken cancellationToken)
    {
        // Handle event
    }
}

// ✅ Event bus automatically resolves and invokes handlers
public class InProcessEventBus : IEventBus
{
    private readonly IServiceProvider _serviceProvider;
    
    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) 
        where TEvent : IDomainEvent
    {
        // Automatically resolves all IEventHandler<TEvent> from DI container
        var handlers = _serviceProvider.GetServices<IEventHandler<TEvent>>();
        
        foreach (var handler in handlers)
        {
            await handler.HandleAsync(@event, cancellationToken);
        }
    }
    
    // ❌ No Subscribe method needed - DI handles it
}
```

**Key Benefits:**
- ✅ Automatic handler discovery via DI
- ✅ No manual subscription code needed
- ✅ Testable (can mock handlers via DI)
- ✅ Supports multiple handlers for same event
- ✅ Follows .NET dependency injection patterns
- ✅ Easy to add/remove handlers without changing event bus code

**For Future Message Broker Implementations:**

```csharp
// Future: Message broker implementation can use different pattern
public class RabbitMqEventBus : IEventBus
{
    // Uses message broker subscription model
    // DI still used for handler registration, but broker handles delivery
}
```

---

## Summary Table

| # | Rejected Proposal | Reason | Alternative |
|---|------------------|--------|-------------|
| 1 | `IEventPublisher` Interface | Redundant - `BaseEntity` already provides this | Use `BaseEntity<TKey>.AddDomainEvent()` |
| 2 | `EventPublisherEntity` Base Class | Redundant - existing hierarchy already supports events | Use `BaseEntity<TKey>`, `AuditableEntity<TKey>`, or `TenantEntity<TKey>` |
| 3 | `Subscribe()` Method in `IEventBus` | Not needed - DI handles registration | Register handlers via DI: `services.AddScoped<IEventHandler<T>, Handler>()` |

---

## Quick Reference: What to Use Instead

### For Adding Domain Events to Entities

```csharp
// ✅ Use existing BaseEntity methods
public class MyEntity : AuditableEntity<Guid>
{
    public void DoSomething()
    {
        // Business logic...
        AddDomainEvent(new MyDomainEvent(Id, TenantId));  // ✅ This method exists
    }
}
```

### For Entity Base Classes

```csharp
// ✅ Choose based on requirements:
// - BaseEntity<TKey>          → Domain events only
// - AuditableEntity<TKey>     → Domain events + Audit
// - TenantEntity<TKey>        → Domain events + Audit + Tenant
```

### For Event Handler Registration

```csharp
// ✅ Register via DI
services.AddScoped<IEventHandler<MyEvent>, MyEventHandler>();

// ✅ Event bus automatically discovers and invokes handlers
```

---

## Questions?

If you have questions about why a proposal was rejected or need clarification on the alternatives, refer to the main [ARCHITECTURAL_REVIEW.md](./ARCHITECTURAL_REVIEW.md) document for detailed analysis.
