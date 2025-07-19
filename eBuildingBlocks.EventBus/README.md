# eBuildingBlocks.EventBus

A comprehensive event-driven architecture building block for .NET applications that provides event publishing, subscription, and integration capabilities using MassTransit and RabbitMQ.

## Overview

eBuildingBlocks.EventBus is a foundational library that implements event-driven architecture patterns and provides event publishing, subscription, and integration capabilities. It leverages MassTransit and RabbitMQ to provide a robust, scalable event bus solution for microservices and distributed applications.

## Key Features

### 📡 Event Publishing
- **Event Publisher**: Centralized event publishing interface
- **MassTransit Integration**: Built-in MassTransit support
- **RabbitMQ Support**: Reliable message queuing with RabbitMQ
- **Event Contracts**: Standardized event contract definitions

### 📨 Event Subscription
- **Event Subscriber**: Event subscription and handling
- **Message Consumers**: MassTransit consumer implementations
- **Event Handlers**: Centralized event handling logic
- **Integration Events**: Cross-service communication events

### 🔄 Event Types & Contracts
- **Integration Events**: Cross-service communication
- **Domain Events**: Domain-specific event types
- **Event Type Enum**: Categorized event types
- **Event Contracts**: Standardized event structures

### 🛠️ Infrastructure
- **MassTransit Extensions**: Configuration and setup utilities
- **RabbitMQ Configuration**: Connection and queue management
- **Event Routing**: Intelligent event routing and filtering
- **Error Handling**: Robust error handling and retry mechanisms

## Installation

```bash
dotnet add package eBuildingBlocks.EventBus
```

## Quick Start

### 1. Configure Event Bus

```csharp
using eBuildingBlocks.EventBus.Events;

// Configure MassTransit with RabbitMQ
services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });
        
        cfg.ConfigureEndpoints(context);
    });
});
```

### 2. Create Event Contracts

```csharp
using eBuildingBlocks.EventBus.Contracts;

public class UserCreatedEvent : IntegrationEvent
{
    public Guid UserId { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### 3. Publish Events

```csharp
using eBuildingBlocks.EventBus.Events;

public class UserService
{
    private readonly IEventPublisher _eventPublisher;
    
    public UserService(IEventPublisher eventPublisher)
    {
        _eventPublisher = eventPublisher;
    }
    
    public async Task<User> CreateUserAsync(CreateUserRequest request)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            Email = request.Email
        };
        
        // Save user to database...
        
        // Publish event
        await _eventPublisher.PublishAsync(new UserCreatedEvent
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            CreatedAt = DateTime.UtcNow
        });
        
        return user;
    }
}
```

### 4. Subscribe to Events

```csharp
using eBuildingBlocks.EventBus.Events;

public class UserCreatedEventHandler : IEventSubscriber<UserCreatedEvent>
{
    private readonly IEmailService _emailService;
    
    public UserCreatedEventHandler(IEmailService emailService)
    {
        _emailService = emailService;
    }
    
    public async Task HandleAsync(UserCreatedEvent @event)
    {
        // Send welcome email
        await _emailService.SendWelcomeEmailAsync(@event.Email, @event.Username);
        
        // Additional processing...
    }
}
```

## Features in Detail

### Event Publisher Interface

Centralized event publishing interface:

```csharp
public interface IEventPublisher
{
    Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class;
    Task PublishAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default) where T : class;
}
```

### Event Subscriber Interface

Event subscription and handling interface:

```csharp
public interface IEventSubscriber<T> where T : class
{
    Task HandleAsync(T message);
}
```

### Integration Events

Base class for integration events:

```csharp
public abstract class IntegrationEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreationDate { get; set; } = DateTime.UtcNow;
    public string Payload { get; set; } = string.Empty;
}
```

### Event Types

Categorized event types for better organization:

```csharp
public enum EventType
{
    UserCreated,
    UserUpdated,
    UserDeleted,
    OrderCreated,
    OrderConfirmed,
    OrderCancelled,
    PaymentProcessed,
    PaymentFailed
}
```

### MassTransit Extensions

Configuration utilities for MassTransit:

```csharp
public static class MassTransitExtensions
{
    public static void ConfigureRabbitMq(this IBusRegistrationConfigurator configurator, string host, string username, string password)
    {
        configurator.UsingRabbitMq((context, cfg) =>
        {
            cfg.Host(host, "/", h =>
            {
                h.Username(username);
                h.Password(password);
            });
            
            cfg.ConfigureEndpoints(context);
        });
    }
}
```

## Project Structure

```
eBuildingBlocks.EventBus/
├── Contracts/
│   ├── IntegrationEvent.cs
│   ├── ITenantSubscriptionExpired.cs
│   └── IUserProjectAssigned.cs
├── Enums/
│   └── EventType.cs
├── Events/
│   ├── EventPublisher.cs
│   ├── EventSubscriber.cs
│   ├── IEventPublisher.cs
│   ├── IEventSubscriber.cs
│   └── MassTransitExtensions.cs
└── eBuildingBlocks.EventBus.csproj
```

## Usage Examples

### Publishing Events

```csharp
public class OrderService
{
    private readonly IEventPublisher _eventPublisher;
    
    public async Task<Order> CreateOrderAsync(CreateOrderRequest request)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            TotalAmount = request.TotalAmount,
            Status = OrderStatus.Created
        };
        
        // Save order to database...
        
        // Publish order created event
        await _eventPublisher.PublishAsync(new OrderCreatedEvent
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            TotalAmount = order.TotalAmount,
            CreatedAt = DateTime.UtcNow
        });
        
        return order;
    }
    
    public async Task ConfirmOrderAsync(Guid orderId)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        order.Status = OrderStatus.Confirmed;
        
        // Update order in database...
        
        // Publish order confirmed event
        await _eventPublisher.PublishAsync(new OrderConfirmedEvent
        {
            OrderId = order.Id,
            ConfirmedAt = DateTime.UtcNow
        });
    }
}
```

### Subscribing to Events

```csharp
public class OrderCreatedEventHandler : IEventSubscriber<OrderCreatedEvent>
{
    private readonly IInventoryService _inventoryService;
    private readonly INotificationService _notificationService;
    
    public async Task HandleAsync(OrderCreatedEvent @event)
    {
        // Update inventory
        await _inventoryService.ReserveItemsAsync(@event.OrderId);
        
        // Send notification to customer
        await _notificationService.SendOrderConfirmationAsync(@event.CustomerId, @event.OrderId);
        
        // Additional processing...
    }
}

public class OrderConfirmedEventHandler : IEventSubscriber<OrderConfirmedEvent>
{
    private readonly IPaymentService _paymentService;
    
    public async Task HandleAsync(OrderConfirmedEvent @event)
    {
        // Process payment
        await _paymentService.ProcessPaymentAsync(@event.OrderId);
    }
}
```

### Tenant-Aware Events

```csharp
public class TenantSubscriptionExpiredEvent : IntegrationEvent
{
    public Guid TenantId { get; set; }
    public DateTime ExpiredAt { get; set; }
}

public class TenantSubscriptionExpiredHandler : IEventSubscriber<TenantSubscriptionExpiredEvent>
{
    private readonly ITenantService _tenantService;
    
    public async Task HandleAsync(TenantSubscriptionExpiredEvent @event)
    {
        // Disable tenant access
        await _tenantService.DisableTenantAsync(@event.TenantId);
        
        // Send notification to tenant admin
        await _notificationService.SendSubscriptionExpiredNotificationAsync(@event.TenantId);
    }
}
```

### Custom Event Contracts

```csharp
public class UserProjectAssignedEvent : IntegrationEvent
{
    public Guid UserId { get; set; }
    public Guid ProjectId { get; set; }
    public string Role { get; set; }
    public DateTime AssignedAt { get; set; }
}

public class UserProjectAssignedHandler : IEventSubscriber<UserProjectAssignedEvent>
{
    private readonly IProjectService _projectService;
    private readonly INotificationService _notificationService;
    
    public async Task HandleAsync(UserProjectAssignedEvent @event)
    {
        // Add user to project
        await _projectService.AddUserToProjectAsync(@event.ProjectId, @event.UserId, @event.Role);
        
        // Send notification to user
        await _notificationService.SendProjectAssignmentNotificationAsync(@event.UserId, @event.ProjectId);
    }
}
```

### Error Handling

```csharp
public class ResilientEventHandler<T> : IEventSubscriber<T> where T : class
{
    private readonly IEventSubscriber<T> _innerHandler;
    private readonly ILogger<ResilientEventHandler<T>> _logger;
    
    public async Task HandleAsync(T message)
    {
        try
        {
            await _innerHandler.HandleAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling event {EventType}", typeof(T).Name);
            
            // Implement retry logic or dead letter queue
            throw;
        }
    }
}
```

## Configuration

### appsettings.json

```json
{
  "EventBus": {
    "RabbitMQ": {
      "Host": "localhost",
      "Username": "guest",
      "Password": "guest",
      "VirtualHost": "/"
    },
    "RetryPolicy": {
      "MaxRetries": 3,
      "RetryInterval": "00:00:05"
    }
  }
}
```

### Service Registration

```csharp
public static class EventBusExtensions
{
    public static IServiceCollection AddEventBus(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            // Register consumers
            x.AddConsumer<UserCreatedEventHandler>();
            x.AddConsumer<OrderCreatedEventHandler>();
            
            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbitMqConfig = configuration.GetSection("EventBus:RabbitMQ").Get<RabbitMqConfig>();
                
                cfg.Host(rabbitMqConfig.Host, rabbitMqConfig.VirtualHost, h =>
                {
                    h.Username(rabbitMqConfig.Username);
                    h.Password(rabbitMqConfig.Password);
                });
                
                cfg.ConfigureEndpoints(context);
            });
        });
        
        services.AddScoped<IEventPublisher, EventPublisher>();
        
        return services;
    }
}
```

## Dependencies

- **MassTransit** - Message bus and routing
- **MassTransit.AspNetCore** - ASP.NET Core integration
- **MassTransit.RabbitMQ** - RabbitMQ transport
- **RabbitMQ.Client** - RabbitMQ client library

## Best Practices

### Event Design

```csharp
// ✅ Good - Use integration events for cross-service communication
public class UserCreatedEvent : IntegrationEvent
{
    public Guid UserId { get; set; }
    public string Username { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ❌ Avoid - Don't include sensitive data in events
public class UserCreatedEvent : IntegrationEvent
{
    public string Password { get; set; } // Sensitive data
    public string SSN { get; set; } // Sensitive data
}
```

### Event Handling

```csharp
// ✅ Good - Keep handlers focused and idempotent
public class OrderCreatedHandler : IEventSubscriber<OrderCreatedEvent>
{
    public async Task HandleAsync(OrderCreatedEvent @event)
    {
        // Check if already processed
        if (await IsAlreadyProcessedAsync(@event.OrderId))
            return;
            
        // Process the event
        await ProcessOrderAsync(@event);
        
        // Mark as processed
        await MarkAsProcessedAsync(@event.OrderId);
    }
}

// ❌ Avoid - Don't make handlers dependent on order
public class OrderCreatedHandler : IEventSubscriber<OrderCreatedEvent>
{
    public async Task HandleAsync(OrderCreatedEvent @event)
    {
        // This depends on other events being processed first
        var user = await GetUserAsync(@event.UserId);
        // ...
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