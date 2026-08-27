# eBuildingBlocks

[![CI](https://github.com/ghazi1567/eBuildingBlocks/actions/workflows/ci.yml/badge.svg?branch=latest-dotnet-10)](https://github.com/ghazi1567/eBuildingBlocks/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/eBuildingBlocks.Domain.svg)](https://www.nuget.org/packages/eBuildingBlocks.Domain)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)

A comprehensive .NET 10.0 building blocks framework providing enterprise-grade infrastructure, patterns, and utilities for building scalable, maintainable, and robust applications.

## Why eBuildingBlocks?

Every team building a new .NET service ends up hand-rolling the same plumbing: a generic repository + unit of work, an audit trail interceptor, multi-tenant query filters, a standardized API response envelope, a transactional outbox for reliable event publishing, global exception handling, API versioning, health checks... `eBuildingBlocks` packages that plumbing as small, independently-versioned NuGet packages so you can pull in only the layers you need instead of rewriting them per project.

It's not a replacement for MediatR, MassTransit, or EF Core — it's the glue and conventions that sit on top of them (in fact it integrates with MassTransit and EF Core directly) so a new service starts from "wire up DI" instead of "design the repository pattern from scratch."

## How this compares to the alternatives

You're probably also looking at one of these. Here's the honest difference — pick based on which tradeoff fits your team, not on which list is longer:

| | eBuildingBlocks | Ardalis.CleanArchitecture | Jason Taylor's Clean Architecture Template | ABP Framework |
|---|---|---|---|---|
| **You get it as** | A NuGet dependency you version and upgrade | A `dotnet new` scaffold you fork and own from day one | Same — a scaffold you fork and own | NuGet packages plus an application/module framework |
| **Upgrading later** | `dotnet add package` to a newer version, changes tracked in a changelog per package | Nothing to upgrade — it's your code now; you manually port improvements if you want them | Same | Package bumps, with framework-provided migration tooling for bigger jumps |
| **Core scope** | Repository/UoW, audit logging, multi-tenancy, domain + integration events, transactional outbox, API scaffolding (versioning, health checks, Hangfire) | Repository/UoW, MediatR-based CQRS, Specification pattern, minimal API surface | Repository/UoW, MediatR-based CQRS, EF Core, ASP.NET Identity, background jobs, Angular/React SPA scaffolding | All of the above and more: dynamic API generation, permission system, admin UI, plugin/module architecture |
| **Multi-tenancy** | Built in (tenant resolution + automatic EF query filters) | Not included | Not included | Built in — this is ABP's signature feature, more mature and deeper than ours |
| **Admin UI / plugin system** | None — this is a library, not an application | None | None | Yes — a major part of the value, with some modules commercial |
| **License** | MIT, every package | MIT | MIT | MIT core; some modules are commercial |

**Pick eBuildingBlocks if:** you want the repeated plumbing (repository, audit, multi-tenancy, outbox) as something you `dotnet add package` and upgrade, not something you copy once and then own the drift on forever, and you don't need ABP's admin UI or module system.

**Pick a template (Ardalis / Jason Taylor) if:** you want full, immediate ownership of every line from the start, or need the SPA scaffolding Jason Taylor's template includes.

**Pick ABP if:** you need the admin UI, plugin ecosystem, or dynamic API generation, and the larger surface area and (partial) commercial licensing are acceptable for your project.

## Quickstart

**Option A — scaffold a working project in one command:**

```bash
dotnet new install eBuildingBlocks.Templates
dotnet new eblocks-api -n MyService
cd MyService
dotnet run --project MyService.API
```

This generates a complete Domain/Application/Infrastructure/API solution with a sample entity, repository, controller, and audit logging already wired up, running on an EF Core in-memory database — no external services required to try it. See [`templates/`](templates) for details.

**Option B — add packages to an existing project:**

```bash
dotnet new webapi -n MyService
cd MyService
dotnet add package eBuildingBlocks.Domain
dotnet add package eBuildingBlocks.Application
dotnet add package eBuildingBlocks.Infrastructure
dotnet add package eBuildingBlocks.API
```

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.BaseRegister(builder.Configuration, builder.Host);

var app = builder.Build();
app.BaseAppUse(builder.Configuration);
app.Run();
```

That wires up API versioning, Scalar/OpenAPI, JWT auth, health checks, and Hangfire. See [`eBuildingBlocks.ReferenceApp.API`](eBuildingBlocks.ReferenceApp.API) for a complete working example, or [`docs/examples`](docs/examples) for guided walkthroughs (multi-tenancy, the transactional outbox, repository/UoW).

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

- **[Examples & tutorials](docs/examples/README.md)** — guided walkthroughs for common scenarios.
- **[Hosting app: transactional outbox](docs/HOSTING_APP_OUTBOX.md)** — Wire `IEventTypeRegistry`, EF outbox interceptor, SQL Server outbox processor, and `IEventPublisher` in your host.
- **[Repository & unit of work](docs/REPOSITORY_AND_UOW.md)** — `IUnitOfWork`, `AddDbContextUnitOfWork`, `IEfQueryableRepository`, and specification/queryable split.
- **[Multi-tenancy](docs/MULTI_TENANCY.md)** — tenant isolation setup with `TenantEntity<TKey>` and global query filters.
- **[Migration: Outbox publisher resolution](docs/MIGRATION_OUTBOX_PUBLISHER.md)** — the outbox processor now prefers the broker-agnostic `IOutboxIntegrationPublisher` over the MassTransit-specific `IEventPublisher`; no action needed for existing `AddIntegrationMassTransit` consumers.
- **[Contributing](CONTRIBUTING.md)** — local setup, PR conventions, coding guidelines.

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
- `TenantAwareDbContext`: Base DbContext that applies tenant query filters and indexes
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
- **.NET 10.0**: Latest .NET framework
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
// Repository<TEntity,TKey,TDbContext> has three type parameters but IRepository<TEntity,TKey> only two,
// so register per-entity rather than as an open generic:
services.AddScoped<IRepository<Product, Guid>, Repository<Product, Guid, YourDbContext>>();
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
public class User : AuditableEntity<Guid>
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
         return await _userRepository.ListAllAsync();
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

Contributions are welcome — see [CONTRIBUTING.md](CONTRIBUTING.md) for how to build, test, and submit a PR.

### **Extension Points**
1. **Custom Repositories**: Implement `IRepository<TEntity, TKey>` for specialized data access
2. **Custom Events**: Extend `IntegrationEvent` for domain events
3. **Custom Middleware**: Extend existing middleware or create new ones
4. **Custom Validators**: Use FluentValidation for custom validation rules

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 👥 Authors

- **Inam Ul Haq** - *Initial work* - [LinkedIn](https://www.linkedin.com/in/inam1567/)

## 🙏 Acknowledgments

- Clean Architecture principles by Robert C. Martin
- Domain-Driven Design concepts by Eric Evans
- MassTransit for message bus implementation
- Entity Framework Core for data access 
