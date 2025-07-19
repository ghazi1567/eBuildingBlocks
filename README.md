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

## 🎯 Core Functionalities

### 1. **Domain Layer** (`eBuildingBlocks.Domain`)
- **Entity Framework**: Base entity classes with audit trail support
- **Repository Pattern**: Generic repository interface
- **Multi-tenancy**: Built-in tenant isolation support
- **Audit Logging**: Automatic audit trail for all entity changes

#### Key Components:
- `BaseEntity`: Abstract base class with audit fields (CreatedAt, UpdatedAt, IsDeleted, etc.)
- `Entity<TKey>`: Generic entity interface with tenant support
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
- `EncryptionHelper`: AES encryption utilities
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
- `AuditSaveChangesInterceptor`: Automatic audit logging
- `ModelBuilderExtensions`: Multi-tenant query filtering

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
- Asynchronous event publishing
- Integration event patterns
- MassTransit message bus integration

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

### **4. Event Bus Configuration**
```csharp
// Configure MassTransit
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



### **Event Publishing**
```csharp
public class UserService
{
    private readonly IEventPublisher _eventPublisher;
    
    public async Task CreateUserAsync(User user)
    {
        // Create user logic...
        
        await _eventPublisher.PublishAsync(new UserCreatedEvent
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
