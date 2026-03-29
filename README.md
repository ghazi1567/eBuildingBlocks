# eBuildingBlocks

A comprehensive .NET 9.0 building blocks framework providing enterprise-grade infrastructure, patterns, and utilities for building scalable, maintainable, and robust applications.

## 🏗️ Project Overview

`eBuildingBlocks` is a modular, clean architecture-based framework that provides essential building blocks for enterprise .NET applications. It implements best practices including Domain-Driven Design (DDD), CQRS patterns, event-driven architecture, and comprehensive audit logging.

## 📁 Project Structure

The solution follows a clean architecture pattern with the following layers:

```
eBuildingBlocks/
├── eBuildingBlocks.API/           # Presentation Layer
├── eBuildingBlocks.Application/    # Application Layer
├── eBuildingBlocks.Domain/        # Domain Layer
├── eBuildingBlocks.Infrastructure/# Infrastructure Layer
├── eBuildingBlocks.EventBus/      # Event Bus Layer
└── eBuildingBlocks.Common/        # Shared Utilities
```

## 📚 Documentation

- **[Hosting app: transactional outbox](docs/HOSTING_APP_OUTBOX.md)** — Wire `IEventTypeRegistry`, EF outbox interceptor, SQL Server outbox processor, and `IEventPublisher` in your host.
- **[Repository & unit of work](docs/REPOSITORY_AND_UOW.md)** — `IUnitOfWork`, `AddDbContextUnitOfWork`, `IEfQueryableRepository`, and specification/queryable split.

**Breaking changes (recent):** `FeatureGate` / `MultiTenancyOptions` live in namespace `eBuildingBlocks.Common.Features` (not `eBuildingBlocks.API.Features`). `IRepository` no longer includes `SaveChangesAsync` — use `IUnitOfWork` (see doc above).

## 🎯 Core Functionalities

### 1. **Domain Layer** (`eBuildingBlocks.Domain`)
- **Entity Framework**: Base entity classes with audit trail support
- **Repository Pattern**: Generic repository interface
- **Multi-tenancy**: Built-in tenant isolation support
- **Audit Logging**: Automatic audit trail for all entity changes

#### Key Components:
- `BaseEntity<TKey>`: Abstract base class with domain event support
- `AuditableEntity<TKey>`: Base entity with audit fields (CreatedOn, CreatedBy, etc.)
- `TenantEntity<TKey>`: Base entity with tenant support and audit
- `IDomainEvent`: Interface for domain events
- `BaseDomainEvent`: Base class for domain events
- `IRepository<TEntity, TKey>`: Generic repository interface
- `ICurrentUser`: Interface for current user context
- `AuditLog`: Comprehensive audit logging model

### 2. **Application Layer** (`eBuildingBlocks.Application`)
- **Validation**: FluentValidation integration
- **Exception Handling**: Global exception handler with standardized error responses
- **Response Models**: Standardized API response patterns
- **Pagination**: Built-in paging support
- **Encryption**: AES encryption utilities

#### Key Components:
- `ResponseModel<T>`: Standardized API response wrapper
- `PagedList<T>`: Pagination support
- `GlobalExceptionHandler`: Centralized exception handling
- `IEventBus`: Domain event bus interface
- `IEventHandler<TEvent>`: Event handler interface
- `EventBusOptions`: Event bus configuration options
- Custom exception classes for different HTTP status codes

### 3. **Infrastructure Layer** (`eBuildingBlocks.Infrastructure`)
- **Entity Framework Core**: Database context with audit interceptor
- **Repository Implementation**: Generic repository implementation
- **Audit Interceptor**: Automatic audit logging for all database changes
- **Multi-tenancy**: Global tenant filtering
- **Dapper Integration**: Raw SQL support for complex queries
- **Identity Integration**: ASP.NET Core Identity support

#### Key Components:
- `DefaultDBContext`: Base DbContext with audit support
- `Repository<TEntity, TKey, TDbContext>`: Generic repository implementation
- `InProcessEventBus`: In-process domain event bus implementation
- `AuditSaveChangesInterceptor`: Automatic audit logging
- `ModelBuilderExtensions`: Multi-tenant query filtering
- `RepositoryExtensions`: Extension methods for event publishing
- `EventBusExtensions`: DI registration extensions for event bus

### 4. **API Layer** (`eBuildingBlocks.API`)
- **API Versioning**: Built-in API versioning support
- **Swagger/OpenAPI**: Auto-generated API documentation
- **CORS**: Cross-origin resource sharing configuration
- **Health Checks**: Application health monitoring
- **Metrics**: Prometheus metrics integration
- **Localization**: Multi-language support
- **Background Jobs**: Hangfire integration for background processing

#### Key Components:
- `BaseController`: Abstract base controller
- `DependencyInjection`: Service registration extensions
- `AppUseExtensions`: Application configuration extensions
- `CurrentUser`: Current user context implementation

### 5. **Event Bus** (`eBuildingBlocks.EventBus`)
- **MassTransit Integration**: Message bus implementation
- **Event Publishing**: Asynchronous event publishing
- **Event Subscribing**: Event subscription patterns
- **Integration Events**: Standardized integration event model

#### Key Components:
- `IntegrationEvent`: Base integration event class
- `IEventPublisher`: Event publishing interface
- `EventPublisher`: MassTransit-based event publisher
- `IEventSubscriber`: Event subscription interface
- `EventType`: Predefined event types

### 6. **Common Utilities** (`eBuildingBlocks.Common`)
- **Guid Generation**: UUID v7 generation utilities
- **Custom Claims**: JWT claim type definitions

## 🚀 Key Features

### **Multi-tenancy Support**
- Automatic tenant filtering at the database level
- Tenant isolation for all entities
- Global query filters for tenant-specific data

### **Comprehensive Audit Logging**
- Automatic audit trail for all entity changes
- Detailed change tracking (old values, new values)
- IP address and user tracking
- JSON serialization of changes



### **Global Exception Handling**
- Standardized error responses
- HTTP status code mapping
- Detailed error logging
- Validation error handling

### **API Infrastructure**
- API versioning support
- Swagger documentation
- Health checks and metrics
- CORS configuration
- Background job processing

### **Event-Driven Architecture**
- **Domain Events**: In-process event bus for modular monoliths
- **Integration Events**: MassTransit-based message bus for microservices
- **Event Handlers**: Type-safe event handling with dependency injection
- **Automatic Event Publishing**: Events published after successful SaveChanges

## 📦 Dependencies

### **Core Dependencies**
- **.NET 9.0**: Latest .NET framework
- **Entity Framework Core**: ORM and data access
- **FluentValidation**: Input validation
- **AutoMapper**: Object mapping
- **MassTransit**: Message bus implementation

### **API Dependencies**
- **Swashbuckle.AspNetCore**: Swagger/OpenAPI
- **Asp.Versioning**: API versioning
- **Hangfire**: Background job processing
- **StackExchange.Redis**: Redis caching
- **prometheus-net**: Metrics collection
- **OpenTelemetry**: Distributed tracing

### **Infrastructure Dependencies**
- **Microsoft.AspNetCore.Identity**: Identity management
- **Dapper**: Micro-ORM for raw SQL
- **RabbitMQ.Client**: Message queuing

## 🛠️ Setup and Configuration

### **1. Database Configuration**
```csharp
// In your DbContext
services.AddDbContext<YourDbContext>(options =>
    options.UseSqlServer(connectionString)
    .AddInterceptors(new AuditSaveChangesInterceptor(currentUser)));
```

### **2. Repository Registration**
```csharp
// Register repositories
services.AddScoped(typeof(IRepository<,>), typeof(Repository<,,>));
```

### **3. API Configuration**
```csharp
// In Program.cs
services.BaseRegister(configuration, hostBuilder);
app.BaseAppUse(configuration);
```

### **4. Domain Event Bus Configuration**
```csharp
// Register in-process event bus for domain events
services.AddInProcessEventBus(options =>
{
    options.FailureMode = EventHandlerFailureMode.FailFast; // or Continue
});

// Register event handlers
services.AddScoped<IEventHandler<ProductCreatedEvent>, ProductCreatedEventHandler>();
```

### **5. Integration Event Bus Configuration (MassTransit)**
```csharp
// Configure MassTransit for integration events (microservices)
services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });
    });
});
```

## 📋 Usage Examples

### **Creating an Entity**
```csharp
public class User : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
```

### **Repository Usage**
```csharp
public class UserService
{
    private readonly IRepository<User, Guid> _userRepository;
    
    public async Task<User?> GetUserAsync(Guid id)
    {
        return await _userRepository.GetByIdAsync(id);
    }
    
         public async Task<IReadOnlyList<User>> GetUsersAsync()
     {
         return await _userRepository.GetAllAsync();
     }
}
```



### **Domain Events (In-Process)**
```csharp
// 1. Define domain event
public record UserCreatedEvent(
    Guid UserId,
    string UserName,
    Guid TenantId
) : BaseDomainEvent(TenantId);

// 2. Publish in entity
public class User : AuditableEntity<Guid>
{
    public string Name { get; set; } = string.Empty;
    
    public void Create(string name)
    {
        Name = name;
        AddDomainEvent(new UserCreatedEvent(Id, name, TenantId));
    }
}

// 3. Create handler
public class UserCreatedEventHandler : IEventHandler<UserCreatedEvent>
{
    public async Task HandleAsync(UserCreatedEvent @event, CancellationToken ct)
    {
        // Handle event - e.g., send welcome email
    }
}

// 4. Commit: use transactional outbox (processor publishes) OR in-process-only — not both for the same events
// Outbox path (register DomainOutboxSaveChangesInterceptor + outbox processor):
await _context.SaveChangesWithTransactionalOutboxAsync(cancellationToken);
// In-process-only path (suppresses outbox enqueue for this call if interceptor is registered):
// await _context.SaveChangesAndPublishDomainEventsInProcessAsync(_eventBus, cancellationToken);
```

### **Integration Events (Cross-Service)**
```csharp
public class UserService
{
    private readonly IEventPublisher _eventPublisher; // From eBuildingBlocks.EventBus
    
    public async Task CreateUserAsync(User user)
    {
        // Create user logic...
        
        await _eventPublisher.PublishAsync(new UserCreatedIntegrationEvent
        {
            UserId = user.Id,
            UserName = user.Name
        });
    }
}
```

### **API Controller**
```csharp
[ApiController]
[Route("[controller]/[action]")]
public class UsersController : BaseController
{
    private readonly IRepository<User, Guid> _userRepository;
    
    [HttpGet]
    public async Task<ResponseModel<User>> GetUser(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        return new ResponseModel<User>().AddData(user);
    }
}
```

## 🔧 Configuration Requirements

### **Required Configuration Sections**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "your-connection-string",
    "RedisConnection": "your-redis-connection"
  },
  "OpenTelemetry": {
    "name": "YourServiceName",
    "url": "your-otel-endpoint"
  },
  "PathBase": "/api"
}
```

### **Environment Variables**
- `ASPNETCORE_ENVIRONMENT`: Development/Staging/Production
- `ConnectionStrings__DefaultConnection`: Database connection string
- `ConnectionStrings__RedisConnection`: Redis connection string

## 🧪 Testing

The framework is designed to be easily testable with:
- Dependency injection support
- Interface-based abstractions
- Mockable repositories and services

## 🤝 Contributing

### **Extension Points**
1. **Custom Repositories**: Implement `IRepository<TEntity, TKey>` for specialized data access
2. **Custom Events**: Extend `IntegrationEvent` for domain events
3. **Custom Middleware**: Extend existing middleware or create new ones
4. **Custom Validators**: Use FluentValidation for custom validation rules

### **Guidelines**
- Follow the existing naming conventions
- Maintain clean architecture principles
- Add comprehensive unit tests
- Update documentation for new features
- Follow the established exception handling patterns

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 👥 Authors

- **Inam Ul Haq** - *Initial work* - [LinkedIn](https://www.linkedin.com/in/inam1567/)

## 🙏 Acknowledgments

- Clean Architecture principles by Robert C. Martin
- Domain-Driven Design concepts by Eric Evans
- MassTransit for message bus implementation
- Entity Framework Core for data access 
