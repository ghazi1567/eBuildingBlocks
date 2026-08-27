# Multi-tenant SaaS API in 10 minutes

This walkthrough wires up a tenant-isolated Web API: every row belongs to a tenant, every query is automatically scoped to the caller's tenant, and there's no way to accidentally leak another tenant's data through a forgotten `WHERE` clause. For the full option reference (header vs. JWT claim resolution, worker hosts, bypassing filters), see the [multi-tenancy deep dive](../MULTI_TENANCY.md).

## 1. Create the project and add packages

```bash
dotnet new webapi -n Acme.Catalog
cd Acme.Catalog
dotnet add package eBuildingBlocks.Domain
dotnet add package eBuildingBlocks.Application
dotnet add package eBuildingBlocks.Infrastructure
dotnet add package eBuildingBlocks.API
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
```

## 2. Define a tenant-scoped entity

`TenantEntity<TKey>` adds `TenantId` plus the audit fields (`CreatedOn`, `CreatedBy`, ...) on top of `BaseEntity<TKey>`.

```csharp
public class Product : TenantEntity<Guid>
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
```

## 3. Configure `DbContext` with tenant filtering

Derive from `TenantAwareDbContext` — it applies the `ITenantEntity` query filters for you in `OnModelCreating`.

```csharp
public class CatalogDbContext(
    DbContextOptions<CatalogDbContext> options,
    ICurrentUser currentUser,
    IOptions<MultiTenancyOptions> multiTenancy)
    : TenantAwareDbContext(options, currentUser, multiTenancy)
{
    public DbSet<Product> Products => Set<Product>();
}
```

## 4. Register services in `Program.cs`

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.BaseRegister(builder.Configuration, builder.Host); // auth, versioning, health checks, current-user/tenant context

builder.Services.AddDbContext<CatalogDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContextUnitOfWork<CatalogDbContext>();

// Repository<TEntity,TKey,TDbContext> has three type params but IRepository<TEntity,TKey> only two,
// so register per-entity rather than as an open generic:
builder.Services.AddScoped<IRepository<Product, Guid>, Repository<Product, Guid, CatalogDbContext>>();

var app = builder.Build();
app.BaseAppUse(builder.Configuration);
app.Run();
```

## 5. Configure tenant resolution

```json
{
  "Features": {
    "MultiTenancy": {
      "Enabled": true,
      "HeaderName": "X-Tenant-Id",
      "ClaimType": "tenant_id"
    }
  }
}
```

With this in place, `ICurrentUser.TenantId` resolves from the caller's JWT claim (or header, depending on config) on every request, `Product` queries are automatically filtered to that tenant, and a missing/unresolvable tenant throws `TenantResolutionException` → HTTP 400 — you never have to remember to add `.Where(p => p.TenantId == currentTenant)` yourself.

## 6. Use it in a service

```csharp
public class ProductService(IRepository<Product, Guid> products, IUnitOfWork unitOfWork)
{
    public async Task<Product> CreateAsync(string name, decimal price, CancellationToken ct)
    {
        var product = new Product { Name = name, Price = price }; // TenantId is stamped automatically on save
        await products.AddAsync(product, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return product;
    }

    public Task<IReadOnlyList<Product>> ListAsync(CancellationToken ct) =>
        products.GetAllAsync(ct); // already scoped to the current tenant
}
```

## Next steps

- Add background jobs or message consumers? Call `AddTenantContextForWorkers` instead of relying on the HTTP pipeline — see the [multi-tenancy deep dive](../MULTI_TENANCY.md#worker--console-hosts).
- Need reliable cross-service events? Continue with [reliable event publishing with the outbox](reliable-events-with-outbox.md).
