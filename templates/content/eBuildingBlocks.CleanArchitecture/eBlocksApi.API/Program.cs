using BuildingBlocks.API.Startup;
using eBlocksApi.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Framework core: controllers, API versioning, OpenAPI/Scalar, CORS, health checks, current-user/tenant
// context, global exception handling — each piece is a no-op unless enabled under "Features" in appsettings.json.
builder.Services.BaseRegister(builder.Configuration, builder.Host);

// EF Core (in-memory by default), repository, and unit of work for this project's entities.
builder.Services.AddAppInfrastructure(builder.Configuration);

var app = builder.Build();

app.BaseAppUse(builder.Configuration);

app.Run();
