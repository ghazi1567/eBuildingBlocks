# eBuildingBlocks.Domain

A domain-driven design (DDD) building block library for .NET applications that provides core domain entities, interfaces, and business logic foundations.

## Overview

eBuildingBlocks.Domain is a foundational library that implements domain-driven design patterns and provides core domain entities, interfaces, and business logic foundations. It serves as the heart of your application's business logic and provides the building blocks for creating robust domain models.

## Key Features

### 🏗️ Domain Entities
- **Base Entity**: Abstract base class with audit fields and tenant support
- **Entity Interface**: Generic entity interface with key support
- **Audit Log**: Comprehensive audit logging capabilities
- **Multi-Tenancy**: Built-in tenant support for SaaS applications

### 🔧 Domain Interfaces
- **Repository Pattern**: Generic repository interface for data access
- **Current User**: User context interface for multi-tenant applications
- **Domain Services**: Interfaces for domain service implementations

### 📊 Audit & Tracking
- **Audit Logging**: Automatic audit trail generation
- **Change Tracking**: Track entity modifications
- **User Context**: Current user information tracking
- **Tenant Isolation**: Multi-tenant data isolation

### 🌐 Multi-Tenancy Support
- **Tenant Context**: Built-in tenant identification
- **Data Isolation**: Automatic tenant-based data filtering
- **Tenant Entity**: Base entity with tenant support

## Installation

```bash
dotnet add package eBuildingBlocks.Domain
```

## Quick Start

### 1. Create Domain Entities

```csharp
using eBuildingBlocks.Domain.Models;

public class Product : BaseEntity
{
    public string Name { get; set; }
    public decimal Price { get; set; }
    public string Description { get; set; }
    
    public Product(string name, decimal price)
    {
        Name = name;
        Price = price;
        Created("system"); // Audit tracking
    }
}
```

### 2. Implement Repository Interface

```csharp
using eBuildingBlocks.Domain.Interfaces;

public interface IProductRepository : IRepository<Product, Guid>
{
    Task<IReadOnlyList<Product>> GetByCategoryAsync(string category);
}
```

### 3. Use Current User Context

```csharp
using eBuildingBlocks.Domain.Interfaces;

public class ProductService
{
    private readonly IProductRepository _repository;
    private readonly ICurrentUser _currentUser;
    
    public ProductService(IProductRepository repository, ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }
    
    public async Task<Product> CreateProductAsync(string name, decimal price)
    {
        var product = new Product(name, price);
        product.Created(_currentUser.UserName ?? "system");
        
        await _repository.AddAsync(product);
        await _repository.CommitChangesAsync();
        
        return product;
    }
}
```

## Features in Detail

### Base Entity

The `BaseEntity` class provides a solid foundation for all domain entities:

```csharp
public abstract class BaseEntity : Entity<Guid>
{
    public DateTime CreatedAt { get; private set; }
    public string CreatedBy { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public string UpdatedBy { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public string DeletedBy { get; private set; }
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    
    protected BaseEntity()
    {
        if (Id == Guid.Empty)
        {
            Id = GuidGenerator.NewV7(); // Modern GUID generation
        }
    }
    
    public void Created(string createdBy)
    {
        CreatedAt = DateTime.Now;
        CreatedBy = createdBy;
    }
    
    public void Updated(string updatedBy)
    {
        UpdatedAt = DateTime.Now;
        UpdatedBy = updatedBy;
    }
    
    public void Deleted(string deletedBy)
    {
        IsDeleted = true;
        DeletedAt = DateTime.Now;
        DeletedBy = deletedBy;
    }
}
```

### Repository Interface

Generic repository interface for data access:

```csharp
public interface IRepository<TEntity, TKey> where TEntity : class, Entity<TKey>
{
    Task<bool> CommitChangesAsync(CancellationToken cancellationToken = default);
    IQueryable<TEntity> Query();
    Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken = default);
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
    
    Task<TEntity> SingleOrDefaultAsync(Expression<Func<TEntity, bool>> wherePredicate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TEntity>> ListAsync(Expression<Func<TEntity, bool>> wherePredicate, CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> wherePredicate, CancellationToken cancellationToken = default);
}
```

### Current User Interface

User context interface for multi-tenant applications:

```csharp
public interface ICurrentUser
{
    string? UserId { get; }
    string? IPAddress { get; }
    string? UserName { get; }
    Guid TenantId { get; }
    string UserAgent { get; }
}
```

### Audit Log

Comprehensive audit logging capabilities:

```csharp
public class AuditLog : BaseEntity
{
    public string TableName { get; set; }
    public string Action { get; set; } // Insert / Update / Delete
    public string KeyValues { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string PerformedBy { get; set; }
    public string IPAddress { get; set; }
    public DateTime PerformedAt { get; set; }
}
```

## Project Structure

```
eBuildingBlocks.Domain/
├── Enums/
│   └── Languages.cs
├── Interfaces/
│   ├── ICurrentUser.cs
│   └── IRepository.cs
├── Models/
│   ├── AuditLog.cs
│   ├── BaseEntity.cs
│   └── Entity.cs
└── eBuildingBlocks.Domain.csproj
```

## Usage Examples

### Creating Domain Entities

```csharp
public class Order : BaseEntity
{
    public string OrderNumber { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public List<OrderItem> Items { get; set; } = new();
    
    public Order(string orderNumber)
    {
        OrderNumber = orderNumber;
        Status = OrderStatus.Created;
    }
    
    public void AddItem(Product product, int quantity)
    {
        var item = new OrderItem(product, quantity);
        Items.Add(item);
        Updated("system");
    }
    
    public void Confirm()
    {
        Status = OrderStatus.Confirmed;
        Updated("system");
    }
}
```

### Implementing Repository Pattern

```csharp
public interface IOrderRepository : IRepository<Order, Guid>
{
    Task<IReadOnlyList<Order>> GetByStatusAsync(OrderStatus status);
    Task<IReadOnlyList<Order>> GetByCustomerAsync(Guid customerId);
    Task<Order?> GetByOrderNumberAsync(string orderNumber);
}
```

### Multi-Tenant Service

```csharp
public class OrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICurrentUser _currentUser;
    
    public OrderService(IOrderRepository orderRepository, ICurrentUser currentUser)
    {
        _orderRepository = orderRepository;
        _currentUser = currentUser;
    }
    
    public async Task<Order> CreateOrderAsync(string orderNumber, List<OrderItemDto> items)
    {
        var order = new Order(orderNumber);
        order.Created(_currentUser.UserName ?? "system");
        
        foreach (var itemDto in items)
        {
            // Add items to order
            order.AddItem(itemDto.Product, itemDto.Quantity);
        }
        
        await _orderRepository.AddAsync(order);
        await _orderRepository.CommitChangesAsync();
        
        return order;
    }
    
    public async Task<IReadOnlyList<Order>> GetMyOrdersAsync()
    {
        // Automatically filtered by tenant
        return await _orderRepository.ListAsync(o => o.TenantId == _currentUser.TenantId);
    }
}
```

### Audit Logging

```csharp
public class AuditService
{
    private readonly IRepository<AuditLog, Guid> _auditRepository;
    private readonly ICurrentUser _currentUser;
    
    public async Task LogEntityChangeAsync<T>(T entity, string action, string? oldValues = null, string? newValues = null)
    {
        var auditLog = new AuditLog
        {
            TableName = typeof(T).Name,
            Action = action,
            KeyValues = entity.Id.ToString(),
            OldValues = oldValues,
            NewValues = newValues,
            PerformedBy = _currentUser.UserName ?? "system",
            IPAddress = _currentUser.IPAddress ?? "",
            PerformedAt = DateTime.UtcNow
        };
        
        auditLog.Created(_currentUser.UserName ?? "system");
        
        await _auditRepository.AddAsync(auditLog);
        await _auditRepository.CommitChangesAsync();
    }
}
```

### Domain Events

```csharp
public class OrderCreatedEvent
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; }
    public Guid CustomerId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class Order
{
    public event EventHandler<OrderCreatedEvent>? OrderCreated;
    
    public void Confirm()
    {
        Status = OrderStatus.Confirmed;
        Updated("system");
        
        // Raise domain event
        OrderCreated?.Invoke(this, new OrderCreatedEvent
        {
            OrderId = Id,
            OrderNumber = OrderNumber,
            CustomerId = CustomerId,
            CreatedAt = CreatedAt
        });
    }
}
```

## Dependencies

- **.NET 9.0** - Target framework
- **eBuildingBlocks.Common** - Common utilities and GUID generation

## Best Practices

### Entity Design

```csharp
// ✅ Good - Use BaseEntity for consistency
public class Product : BaseEntity
{
    public string Name { get; set; }
    public decimal Price { get; set; }
}

// ❌ Avoid - Don't create entities without proper base
public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; }
}
```

### Repository Pattern

```csharp
// ✅ Good - Use generic repository interface
public interface IProductRepository : IRepository<Product, Guid>
{
    // Add specific methods here
}

// ❌ Avoid - Don't create repositories without interface
public class ProductRepository
{
    // Implementation without interface
}
```

### Multi-Tenancy

```csharp
// ✅ Good - Always check tenant context
public async Task<IReadOnlyList<Product>> GetProductsAsync()
{
    return await _repository.ListAsync(p => p.TenantId == _currentUser.TenantId);
}

// ❌ Avoid - Don't ignore tenant isolation
public async Task<IReadOnlyList<Product>> GetProductsAsync()
{
    return await _repository.GetAllAsync(); // No tenant filtering
}
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

Copyright © Inam Ul Haq. All rights reserved.

## Support

- **Author**: Inam Ul Haq
- **Email**: inam.sys@gmail.com
- **LinkedIn**: https://www.linkedin.com/in/inam1567/
- **Repository**: https://github.com/ghazi1567/eBuildingBlocks 