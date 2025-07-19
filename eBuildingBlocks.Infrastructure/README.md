# eBuildingBlocks.Infrastructure

A comprehensive infrastructure building block for .NET applications that provides data access, repository implementations, audit logging, and Entity Framework Core utilities.

## Overview

eBuildingBlocks.Infrastructure is a foundational library that implements data access patterns, repository implementations, audit logging, and Entity Framework Core utilities. It provides the infrastructure layer for your application with built-in support for Entity Framework Core, Dapper, and audit logging.

## Key Features

### 🗄️ Data Access
- **Repository Pattern**: Generic repository implementation
- **Entity Framework Core**: Full EF Core integration
- **Dapper Support**: Lightweight data access with Dapper
- **Unit of Work**: Transaction management and unit of work pattern

### 📊 Audit Logging
- **Audit Save Changes Interceptor**: Automatic audit trail generation
- **Change Tracking**: Track entity modifications automatically
- **Audit Log Storage**: Store audit logs in database
- **User Context Integration**: Link audit logs to current user

### 🔧 Infrastructure Components
- **Model Builder Extensions**: EF Core configuration utilities
- **Repository Properties**: Repository configuration and settings
- **Database Extensions**: Database-specific utilities
- **Identity Integration**: ASP.NET Core Identity support

### 🛡️ Security & Identity
- **ASP.NET Core Identity**: Built-in identity management
- **User Management**: User and role management utilities
- **Tenant Support**: Multi-tenant data access patterns
- **Security Extensions**: Security-related utilities

## Installation

```bash
dotnet add package eBuildingBlocks.Infrastructure
```

## Quick Start

### 1. Configure Entity Framework

```csharp
using eBuildingBlocks.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Apply audit configuration
        modelBuilder.ApplyAuditConfiguration();
    }
}
```

### 2. Register Services

```csharp
using eBuildingBlocks.Infrastructure.Implementations;

services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
services.AddScoped<IUnitOfWork, UnitOfWork>();
```

### 3. Use Repository Pattern

```csharp
using eBuildingBlocks.Domain.Interfaces;

public class ProductService
{
    private readonly IRepository<Product, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork;
    
    public ProductService(IRepository<Product, Guid> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }
    
    public async Task<Product> CreateProductAsync(string name, decimal price)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = name,
            Price = price
        };
        
        await _repository.AddAsync(product);
        await _unitOfWork.CommitAsync();
        
        return product;
    }
}
```

## Features in Detail

### Repository Implementation

Generic repository implementation with full CRUD operations:

```csharp
public class Repository<TEntity, TKey> : IRepository<TEntity, TKey> 
    where TEntity : class, Entity<TKey>
{
    private readonly DbContext _context;
    private readonly DbSet<TEntity> _dbSet;
    
    public Repository(DbContext context)
    {
        _context = context;
        _dbSet = context.Set<TEntity>();
    }
    
    public async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
    }
    
    public async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.ToListAsync(cancellationToken);
    }
    
    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
    }
    
    public async Task<bool> CommitChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken) > 0;
    }
    
    // Additional methods...
}
```

### Unit of Work Pattern

Transaction management and unit of work implementation:

```csharp
public class UnitOfWork : IUnitOfWork
{
    private readonly DbContext _context;
    
    public UnitOfWork(DbContext context)
    {
        _context = context;
    }
    
    public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
    
    public async Task BeginTransactionAsync()
    {
        await _context.Database.BeginTransactionAsync();
    }
    
    public async Task CommitTransactionAsync()
    {
        await _context.Database.CommitTransactionAsync();
    }
    
    public async Task RollbackTransactionAsync()
    {
        await _context.Database.RollbackTransactionAsync();
    }
}
```

### Audit Save Changes Interceptor

Automatic audit trail generation:

```csharp
public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUser _currentUser;
    private readonly IRepository<AuditLog, Guid> _auditRepository;
    
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, 
        InterceptionResult<int> result, 
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        var auditEntries = OnBeforeSaveChanges(context);
        
        var result = await base.SavingChangesAsync(eventData, result, cancellationToken);
        
        await OnAfterSaveChangesAsync(context, auditEntries);
        
        return result;
    }
    
    private List<AuditEntry> OnBeforeSaveChanges(DbContext context)
    {
        context.ChangeTracker.DetectChanges();
        var auditEntries = new List<AuditEntry>();
        
        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is AuditLog || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                continue;
                
            var auditEntry = new AuditEntry(entry)
            {
                TableName = entry.Entity.GetType().Name,
                Action = entry.State.ToString(),
                UserId = _currentUser.UserId,
                TenantId = _currentUser.TenantId
            };
            
            auditEntries.Add(auditEntry);
        }
        
        return auditEntries;
    }
}
```

### Model Builder Extensions

Entity Framework Core configuration utilities:

```csharp
public static class ModelBuilderExtensions
{
    public static void ApplyAuditConfiguration(this ModelBuilder modelBuilder)
    {
        // Configure audit properties
        modelBuilder.Entity<BaseEntity>(entity =>
        {
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
            entity.Property(e => e.UpdatedBy).HasMaxLength(100);
            entity.Property(e => e.DeletedBy).HasMaxLength(100);
        });
        
        // Configure soft delete filter
        modelBuilder.Entity<BaseEntity>().HasQueryFilter(e => !e.IsDeleted);
    }
    
    public static void ApplyTenantConfiguration(this ModelBuilder modelBuilder)
    {
        // Configure tenant filtering
        modelBuilder.Entity<BaseEntity>().HasIndex(e => e.TenantId);
    }
}
```

## Project Structure

```
eBuildingBlocks.Infrastructure/
├── Extensions/
│   └── ModelBuilderExtensions.cs
├── Implementations/
│   ├── AuditSaveChangesInterceptor.cs
│   ├── Repository.cs
│   ├── RepositoryProperties.cs
│   └── UnitOfWork.cs
└── eBuildingBlocks.Infrastructure.csproj
```

## Usage Examples

### Custom Repository Implementation

```csharp
public interface IProductRepository : IRepository<Product, Guid>
{
    Task<IReadOnlyList<Product>> GetByCategoryAsync(string category);
    Task<IReadOnlyList<Product>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice);
}

public class ProductRepository : Repository<Product, Guid>, IProductRepository
{
    public ProductRepository(DbContext context) : base(context)
    {
    }
    
    public async Task<IReadOnlyList<Product>> GetByCategoryAsync(string category)
    {
        return await Query()
            .Where(p => p.Category == category && !p.IsDeleted)
            .ToListAsync();
    }
    
    public async Task<IReadOnlyList<Product>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice)
    {
        return await Query()
            .Where(p => p.Price >= minPrice && p.Price <= maxPrice && !p.IsDeleted)
            .ToListAsync();
    }
}
```

### Multi-Tenant Repository

```csharp
public class TenantAwareRepository<TEntity, TKey> : Repository<TEntity, TKey>
    where TEntity : class, Entity<TKey>
{
    private readonly ICurrentUser _currentUser;
    
    public TenantAwareRepository(DbContext context, ICurrentUser currentUser) 
        : base(context)
    {
        _currentUser = currentUser;
    }
    
    public override IQueryable<TEntity> Query()
    {
        return base.Query().Where(e => e.TenantId == _currentUser.TenantId);
    }
    
    public override async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var entity = await base.GetByIdAsync(id, cancellationToken);
        
        if (entity != null && entity.TenantId != _currentUser.TenantId)
        {
            return null; // Tenant isolation
        }
        
        return entity;
    }
}
```

### Audit Logging Service

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
            PerformedAt = DateTime.UtcNow,
            TenantId = _currentUser.TenantId
        };
        
        auditLog.Created(_currentUser.UserName ?? "system");
        
        await _auditRepository.AddAsync(auditLog);
    }
}
```

### Database Configuration

```csharp
public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add DbContext
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            options.AddInterceptors(new AuditSaveChangesInterceptor());
        });
        
        // Register repositories
        services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        // Register specific repositories
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        
        return services;
    }
}
```

### Dapper Integration

```csharp
public class DapperRepository<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : class, Entity<TKey>
{
    private readonly IDbConnection _connection;
    
    public DapperRepository(IDbConnection connection)
    {
        _connection = connection;
    }
    
    public async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var sql = "SELECT * FROM {TableName} WHERE Id = @Id";
        return await _connection.QueryFirstOrDefaultAsync<TEntity>(sql, new { Id = id });
    }
    
    public async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var sql = "SELECT * FROM {TableName} WHERE IsDeleted = 0";
        var results = await _connection.QueryAsync<TEntity>(sql);
        return results.ToList();
    }
    
    // Additional methods...
}
```

### Identity Integration

```csharp
public class ApplicationUser : IdentityUser
{
    public Guid TenantId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string UpdatedBy { get; set; }
}

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure identity tables
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.HasIndex(e => e.TenantId);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
        });
    }
}
```

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MyApp;Trusted_Connection=true;MultipleActiveResultSets=true"
  },
  "Audit": {
    "Enabled": true,
    "IncludeOldValues": true,
    "IncludeNewValues": true
  }
}
```

### Service Registration

```csharp
public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            
            // Add audit interceptor
            if (configuration.GetValue<bool>("Audit:Enabled"))
            {
                options.AddInterceptors(new AuditSaveChangesInterceptor());
            }
        });
        
        // Identity
        services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();
        
        // Repositories
        services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        return services;
    }
}
```

## Dependencies

- **Entity Framework Core** - ORM and data access
- **Dapper** - Lightweight data access
- **ASP.NET Core Identity** - User and role management
- **RabbitMQ.Client** - Message queuing

## Best Practices

### Repository Pattern

```csharp
// ✅ Good - Use generic repository interface
public interface IProductRepository : IRepository<Product, Guid>
{
    // Add specific methods
}

// ❌ Avoid - Don't create repositories without interface
public class ProductRepository
{
    // Implementation without interface
}
```

### Audit Logging

```csharp
// ✅ Good - Enable audit logging for important entities
public class Product : BaseEntity
{
    // Entity properties
}

// ❌ Avoid - Don't disable audit for sensitive operations
public class SecretEntity : BaseEntity
{
    // This will still be audited
}
```

### Multi-Tenancy

```csharp
// ✅ Good - Always filter by tenant
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