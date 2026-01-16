# eBuildingBlocks.API

A comprehensive API building block library for .NET applications that provides essential cross-cutting concerns, infrastructure components, and utilities for building robust web APIs.

## Overview

eBuildingBlocks.API is a foundational library that encapsulates common API patterns, middleware, and configuration utilities. It provides a standardized approach to building ASP.NET Core APIs with built-in support for versioning, documentation, monitoring, and cross-cutting concerns.

## Key Features

### 🔧 Core Infrastructure
- **API Versioning**: Built-in support for URL segment and header-based API versioning
- **Swagger/OpenAPI Integration**: Automatic API documentation with JWT authentication support
- **CORS Configuration**: Pre-configured CORS policies for cross-origin requests
- **Health Checks**: Built-in health monitoring endpoints
- **Metrics & Monitoring**: Prometheus metrics integration and OpenTelemetry tracing

### 🛡️ Security & Authentication
- **JWT Bearer Authentication**: Ready-to-use JWT authentication configuration
- **Authorization Headers**: Automatic Accept-Language header parameter injection
- **Current User Context**: User context management with tenant support

### 📊 Monitoring & Observability
- **OpenTelemetry Integration**: Distributed tracing and metrics collection
- **Prometheus Metrics**: HTTP metrics and custom application metrics
- **Health Checks**: Database and application health monitoring
- **Hangfire Dashboard**: Background job monitoring and management

### 🔄 Background Processing
- **Hangfire Integration**: Background job processing with memory storage
- **Redis Support**: Distributed caching and session management
- **Memory Cache**: In-memory caching capabilities

## Installation

```bash
dotnet add package eBuildingBlocks.API
```

## Quick Start

### 1. Register Services

```csharp
using eBuildingBlocks.API.Startup;

var builder = WebApplication.CreateBuilder(args);

// Register all base services
builder.Services.BaseRegister(builder.Configuration, builder.Host);
```

### 2. Configure Application

```csharp
var app = builder.Build();

// Configure all base middleware
app.BaseAppUse(builder.Configuration);
```

### 3. Create Controllers

```csharp
using eBuildingBlocks.API.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class SampleController : BaseController
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok("Hello from eBuildingBlocks!");
    }
}
```

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "RedisConnection": "localhost:6379"
  },
  "OpenTelemetry": {
    "name": "YourApiService",
    "url": "http://localhost:4317"
  },
  "PathBase": "/api"
}
```

### API Versioning

The library supports multiple API versioning strategies:

```csharp
// URL-based versioning: /api/v1/controller/action
// Header-based versioning: X-API-Version: 1.0
```

### Swagger Configuration

Automatic Swagger documentation with JWT authentication:

```csharp
// Available at: /swagger
// JWT Bearer token support included
```

## Features in Detail

### API Versioning

```csharp
// Configure versioning in DependencyInjection.cs
services.RegisterAPIVersioning();
```

### Health Checks

```csharp
// Health check endpoint: /healthz
// Includes database and application health
```

### Metrics

```csharp
// Prometheus metrics endpoint: /metrics
// HTTP metrics automatically collected
```

### Background Jobs

```csharp
// Hangfire dashboard: /hangfire
// Default credentials: admin/admin
```

## Dependencies

- **ASP.NET Core 9.0**
- **Swashbuckle.AspNetCore** - API documentation
- **Asp.Versioning** - API versioning
- **Hangfire.AspNetCore** - Background job processing
- **prometheus-net.AspNetCore** - Metrics collection
- **OpenTelemetry** - Distributed tracing
- **StackExchange.Redis** - Redis client

## Project Structure

```
eBuildingBlocks.API/
├── Configs/
│   ├── AddAcceptLanguageHeaderParameter.cs
│   ├── AppUseExtensions.cs
│   ├── CaptchaSettings.cs
│   ├── CurrentUser.cs
│   └── DependencyInjection.cs
├── Controllers/
│   └── BaseController.cs
└── eBuildingBlocks.API.csproj
```

## Usage Examples

### Custom Controller

```csharp
public class UserController : BaseController
{
    private readonly IUserService _userService;
    
    public UserController(IUserService userService)
    {
        _userService = userService;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _userService.GetAllAsync();
        return Ok(users);
    }
}
```

### Adding Custom Middleware

```csharp
public static IApplicationBuilder UsingCustomMiddleware(this IApplicationBuilder app)
{
    app.UseMiddleware<CustomMiddleware>();
    return app;
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