# eBuildingBlocks.Application

A comprehensive application layer building block for .NET applications that provides essential cross-cutting concerns, exception handling, response models, and application utilities.

## Overview

eBuildingBlocks.Application is a foundational library that encapsulates common application patterns, exception handling, response models, and middleware components. It provides a standardized approach to building application layers with built-in support for validation, exception handling, and response formatting.

## Key Features

### 🛡️ Exception Handling
- **Global Exception Handler**: Centralized exception handling middleware
- **Custom Exceptions**: Predefined exception types for common scenarios
- **HTTP Response Middleware**: Standardized HTTP response formatting
- **Error Response Models**: Consistent error response structures

### 📋 Response Models
- **Standardized Response Format**: Consistent API response structure
- **Paged List Support**: Built-in pagination support
- **View Models**: Common view model patterns
- **Enum View Models**: Enumeration display models

### 🔧 Middleware Components
- **HTTP Response Middleware**: Response formatting and logging
- **Global Exception Handler**: Centralized error handling
- **Request/Response Logging**: Built-in request/response logging

### ✅ Validation & Business Logic
- **FluentValidation Integration**: Built-in validation support
- **AutoMapper Integration**: Object mapping utilities
- **Feature Management**: Feature flag support

## Installation

```bash
dotnet add package eBuildingBlocks.Application
```

## Quick Start

### 1. Register Services

```csharp
using eBuildingBlocks.Application;

var builder = WebApplication.CreateBuilder(args);

// Register application services
builder.Services.AddApplication();
```

### 2. Configure Middleware

```csharp
var app = builder.Build();

// Add global exception handler
app.UseMiddleware<GlobalExceptionHandler>();

// Add HTTP response middleware
app.UseMiddleware<HttpResponseMiddleware>();
```

### 3. Use Response Models

```csharp
using eBuildingBlocks.Application.Models;

public class UserController : ControllerBase
{
    [HttpGet]
    public async Task<ResponseModel<List<UserDto>>> GetUsers()
    {
        var users = await _userService.GetAllAsync();
        return ResponseModel<List<UserDto>>.Success(users);
    }
}
```

## Features in Detail

### Exception Handling

The library provides a comprehensive exception handling system:

```csharp
// Global exception handler automatically catches and formats exceptions
app.UseMiddleware<GlobalExceptionHandler>();
```

#### Available Exception Types

```csharp
// HTTP 400 - Bad Request
throw new BadRequestException("Invalid input");

// HTTP 401 - Unauthorized
throw new UnauthorizedException("Authentication required");

// HTTP 403 - Forbidden
throw new ForbiddenException("Access denied");

// HTTP 404 - Not Found
throw new NotFoundException("Resource not found");

// HTTP 405 - Method Not Allowed
throw new MethodNotAllowedException("Method not supported");

// HTTP 409 - Conflict
throw new ConflictException("Resource conflict");

// HTTP 429 - Too Many Requests
throw new TooManyRequestException("Rate limit exceeded");

// HTTP 501 - Not Implemented
throw new NotImplementedException("Feature not implemented");
```

### Response Models

#### Standard Response Format

```csharp
public class ResponseModel<T>
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; }
    public T Data { get; set; }
    public List<string> Errors { get; set; }
    public int StatusCode { get; set; }
}

// Usage
return ResponseModel<UserDto>.Success(user);
return ResponseModel<UserDto>.Failure("User not found", 404);
```

#### Paged List Support

```csharp
public class PagedList<T>
{
    public List<T> Items { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }
}
```

### View Models

#### Entity View Model

```csharp
public class EntityViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; }
}
```

#### Enum View Model

```csharp
public class EnumViewModel
{
    public int Value { get; set; }
    public string Name { get; set; }
    public string DisplayName { get; set; }
}
```

#### Redirect View Model

```csharp
public class RedirectViewModel
{
    public string Url { get; set; }
    public bool IsPermanent { get; set; }
}
```

## Project Structure

```
eBuildingBlocks.Application/
├── Exceptions/
│   ├── BadRequestException.cs
│   ├── ConflictException.cs
│   ├── ForbiddenException.cs
│   ├── MethodNotAllowedException.cs
│   ├── NotFoundException.cs
│   ├── NotImplementedException.cs
│   ├── TooManyRequestException.cs
│   └── UnauthorizedException.cs
├── Models/
│   ├── PagedList.cs
│   └── ResponseModel.cs
├── ViewModels/
│   ├── EntityViewModel.cs
│   ├── EnumViewModel.cs
│   └── RedirectViewModel.cs
├── Middlewares/
│   └── HttpResponseMiddleware.cs
├── GlobalExceptionHandler.cs
└── eBuildingBlocks.Application.csproj
```

## Usage Examples

### Custom Exception Handler

```csharp
public class CustomExceptionHandler : GlobalExceptionHandler
{
    protected override async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Custom exception handling logic
        await base.HandleExceptionAsync(context, exception);
    }
}
```

### Custom Response Middleware

```csharp
public class CustomResponseMiddleware : HttpResponseMiddleware
{
    protected override async Task ProcessResponseAsync(HttpContext context)
    {
        // Custom response processing
        await base.ProcessResponseAsync(context);
    }
}
```

### Using Response Models

```csharp
[HttpGet]
public async Task<ResponseModel<PagedList<UserDto>>> GetUsers(int page = 1, int pageSize = 10)
{
    try
    {
        var users = await _userService.GetPagedAsync(page, pageSize);
        return ResponseModel<PagedList<UserDto>>.Success(users);
    }
    catch (Exception ex)
    {
        return ResponseModel<PagedList<UserDto>>.Failure(ex.Message, 500);
    }
}
```

### Custom Exception

```csharp
public class CustomBusinessException : Exception
{
    public CustomBusinessException(string message) : base(message)
    {
    }
}

// Usage
throw new CustomBusinessException("Business rule violation");
```

## Dependencies

- **ASP.NET Core 9.0**
- **FluentValidation** - Input validation
- **AutoMapper** - Object mapping
- **Microsoft.FeatureManagement** - Feature flags
- **Polly** - Resilience and transient-fault-handling
- **Swashbuckle.AspNetCore.Annotations** - API documentation
- **System.IdentityModel.Tokens.Jwt** - JWT token handling

## Configuration

### Exception Handling Configuration

```csharp
// Configure global exception handler
app.UseMiddleware<GlobalExceptionHandler>();

// Configure response middleware
app.UseMiddleware<HttpResponseMiddleware>();
```

### Validation Configuration

```csharp
// Register FluentValidation
services.AddFluentValidationAutoValidation();
services.AddValidatorsFromAssemblyContaining<Startup>();
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