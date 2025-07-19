# eBuildingBlocks.Common

A lightweight utility library for .NET applications that provides common utilities, extensions, and helper classes for cross-cutting concerns.

## Overview

eBuildingBlocks.Common is a foundational utility library that provides essential common utilities, helper classes, and extensions that can be used across different layers of your application. It contains lightweight, dependency-free utilities that enhance development productivity.

## Key Features

### 🔧 Utility Classes
- **Guid Generator**: Modern GUID generation utilities
- **Custom Claim Types**: Standardized JWT claim type definitions
- **Extension Methods**: Common extension methods for everyday tasks

### 🆔 Identity & Security
- **Custom Claim Types**: Predefined claim types for JWT tokens
- **Tenant Support**: Multi-tenancy claim definitions
- **Security Utilities**: Common security-related utilities

### 🛠️ Development Utilities
- **Guid Generation**: Modern GUID v7 generation
- **Common Extensions**: Reusable extension methods
- **Helper Classes**: Utility classes for common operations

## Installation

```bash
dotnet add package eBuildingBlocks.Common
```

## Quick Start

### 1. Using Guid Generator

```csharp
using eBuildingBlocks.Common.utils;

// Generate a new GUID v7
var newGuid = GuidGenerator.NewV7();
```

### 2. Using Custom Claim Types

```csharp
using eBuildingBlocks.Common.utils;

// Access predefined claim types
var tenantIdClaim = CustomClaimTypes.TenantId; // "tenant_id"
```

## Features in Detail

### Guid Generator

The library provides a modern GUID generation utility:

```csharp
using eBuildingBlocks.Common.utils;

public class UserService
{
    public User CreateUser(string name)
    {
        var user = new User
        {
            Id = GuidGenerator.NewV7(), // Modern GUID v7
            Name = name,
            CreatedAt = DateTime.UtcNow
        };
        
        return user;
    }
}
```

**Benefits of GUID v7:**
- Time-ordered for better database performance
- Monotonic within the same millisecond
- Compatible with distributed systems
- Better for indexing and sorting

### Custom Claim Types

Standardized JWT claim type definitions:

```csharp
using eBuildingBlocks.Common.utils;

public class JwtService
{
    public string GenerateToken(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(CustomClaimTypes.TenantId, user.TenantId.ToString())
        };
        
        // Token generation logic...
        return token;
    }
}
```

#### Available Claim Types

```csharp
public static class CustomClaimTypes
{
    public const string TenantId = "tenant_id";
    // Additional claim types can be added here
}
```

## Project Structure

```
eBuildingBlocks.Common/
├── utils/
│   ├── CustomClaimTypes.cs
│   └── GuidGenerator.cs
└── eBuildingBlocks.Common.csproj
```

## Usage Examples

### Entity Creation with Modern GUIDs

```csharp
using eBuildingBlocks.Common.utils;

public abstract class BaseEntity
{
    public Guid Id { get; set; }
    
    protected BaseEntity()
    {
        if (Id == Guid.Empty)
        {
            Id = GuidGenerator.NewV7();
        }
    }
}
```

### JWT Token Generation

```csharp
using eBuildingBlocks.Common.utils;
using System.Security.Claims;

public class TokenService
{
    public string CreateToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(CustomClaimTypes.TenantId, user.TenantId.ToString())
        };
        
        // Token generation logic...
        return token;
    }
}
```

### Multi-Tenant Application

```csharp
using eBuildingBlocks.Common.utils;

public class TenantAwareService
{
    public async Task<IActionResult> GetUserData(HttpContext context)
    {
        var tenantIdClaim = context.User.FindFirst(CustomClaimTypes.TenantId);
        
        if (tenantIdClaim == null)
        {
            return Unauthorized("Tenant information not found");
        }
        
        var tenantId = Guid.Parse(tenantIdClaim.Value);
        var userData = await _userService.GetByTenantAsync(tenantId);
        
        return Ok(userData);
    }
}
```

### Database Entity with Modern GUIDs

```csharp
using eBuildingBlocks.Common.utils;

public class Product : BaseEntity
{
    public string Name { get; set; }
    public decimal Price { get; set; }
    public Guid TenantId { get; set; }
    
    public Product()
    {
        // Id is automatically set to GUID v7 in BaseEntity constructor
    }
}
```

## Dependencies

- **.NET 9.0** - Target framework
- **No external dependencies** - Lightweight utility library

## Performance Considerations

### GUID Generation

- **GUID v7**: Time-ordered, better for database indexing
- **Performance**: Optimized for high-frequency generation
- **Uniqueness**: Guaranteed uniqueness across distributed systems

### Memory Usage

- **Lightweight**: Minimal memory footprint
- **No allocations**: Efficient utility methods
- **Thread-safe**: Safe for concurrent usage

## Best Practices

### Using GUID Generator

```csharp
// ✅ Good - Use for new entities
var newEntity = new MyEntity
{
    Id = GuidGenerator.NewV7()
};

// ❌ Avoid - Don't use for existing entities
var existingEntity = new MyEntity
{
    Id = GuidGenerator.NewV7() // This will overwrite existing ID
};
```

### Using Custom Claim Types

```csharp
// ✅ Good - Use predefined claim types
new Claim(CustomClaimTypes.TenantId, tenantId.ToString())

// ❌ Avoid - Don't hardcode claim types
new Claim("tenant_id", tenantId.ToString())
```

## Extension Points

### Adding Custom Claim Types

```csharp
public static class CustomClaimTypes
{
    public const string TenantId = "tenant_id";
    public const string UserRole = "user_role";
    public const string Department = "department";
    // Add more as needed
}
```

### Extending Guid Generator

```csharp
public static class GuidGeneratorExtensions
{
    public static Guid NewSequentialGuid()
    {
        // Custom sequential GUID generation
        return GuidGenerator.NewV7();
    }
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