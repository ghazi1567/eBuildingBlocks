using BuildingBlocks.API.Startup;
using BuildingBlocks.EventBus.Events;
using eBuildingBlocks.Application.Events;
using eBuildingBlocks.Common.Features;
using eBuildingBlocks.Infrastructure.Extensions;
using eBuildingBlocks.ReferenceApp.API.Hosting;
using eBuildingBlocks.ReferenceApp.Application.Orders;
using eBuildingBlocks.ReferenceApp.Domain.Orders.Events;
using eBuildingBlocks.ReferenceApp.Infrastructure.Data;
using eBuildingBlocks.ReferenceApp.Infrastructure.DependencyInjection;
using Hangfire;

// -----------------------------------------------------------------------------
// Reference host: BuildingBlocks.API BaseRegister / BaseAppUse + shared outbox,
// MassTransit, health checks, and FluentValidation; app code = catalog + EF wiring + handlers.
// -----------------------------------------------------------------------------

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// FluentValidation assembly = application layer (validators live with commands).
builder.Services.BaseRegister(
    configuration,
    builder.Host,
    fluentValidationAssembly: typeof(PlaceOrderCommandValidator).Assembly);

// Register catalog before outbox processor hosted service so IEventTypeRegistry is populated first at startup.
builder.Services.AddHostedService<OutboxEventCatalogStartup>();

// Event type registry + outbox interceptor + outbox processor
builder.Services.AddTransactionalOutboxInfrastructure<ReferenceDbContext>(configuration);

// EF Core, audit interceptor, repositories (outbox interceptor already registered above).
builder.Services.AddReferenceInfrastructure(configuration);

// MassTransit + IEventPublisher; consumers from this assembly (API).
builder.Services.AddIntegrationMassTransit(configuration, typeof(Program).Assembly);

// Application handlers (domain-specific).
builder.Services.AddScoped<PlaceOrderCommandHandler>();
builder.Services.AddScoped<GetOrderByIdQueryHandler>();
builder.Services.AddScoped<OrderPlacedDomainEventHandler>();
builder.Services.AddScoped<IEventHandler<OrderPlacedDomainEvent>>(sp => sp.GetRequiredService<OrderPlacedDomainEventHandler>());
builder.Services.AddScoped<ReferenceHangfireOutboxBridgeJob>();

builder.Services.RegisterHealthChecksWithDbContext<ReferenceDbContext>(configuration);

var app = builder.Build();

await app.ApplyReferenceMigrationsAsync().ConfigureAwait(false);

app.BaseAppUse(configuration);

if (FeatureGate.Enabled(configuration, "Features:Hangfire", fallback: true))
{
    RecurringJob.AddOrUpdate<ReferenceHangfireOutboxBridgeJob>(
        "reference-operational-heartbeat",
        j => j.RunScheduledOperationalHeartbeatAsync(),
        Cron.Minutely);
}

app.Run();
