# Repository and unit of work

## `IUnitOfWork`

`SaveChangesAsync` is no longer on `IRepository<TEntity, TKey>`. Use **`IUnitOfWork`** to commit a transaction boundary.

Register in your host (same `DbContext` scope as repositories):

```csharp
using eBuildingBlocks.Infrastructure.Extensions;

builder.Services.AddDbContext<YourDbContext>(...);
builder.Services.AddDbContextUnitOfWork<YourDbContext>();
```

Inject `IUnitOfWork` in application services:

```csharp
public class OrderService(IRepository<Order, Guid> orders, IUnitOfWork unitOfWork)
{
    public async Task CreateAsync(Order order, CancellationToken ct)
    {
        await orders.AddAsync(order, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
```

`Repository<,,>` still inherits the EF `UnitOfWork` base type, so the same `SaveChangesAsync` implementation applies when you resolve `IUnitOfWork` as `DbContextUnitOfWork<TDbContext>` against the shared context.

## `IEfQueryableRepository` and `IQueryable<T>`

`IQueryable<T>` is **not** on domain `IRepository<TEntity, TKey>` (avoids leaking LINQ providers into the domain model).

The EF implementation `Repository<TEntity, TKey, TDbContext>` also implements **`IEfQueryableRepository<TEntity, TKey>`** (`eBuildingBlocks.Infrastructure.Data`), which exposes **`Query()`** (default: no-tracking). Inject that interface where you need LINQ-to-EF, or cast: **`repository.AsEfQueryable()`** (`eBuildingBlocks.Infrastructure.Extensions`).

Prefer **`ISpecification<T>`** with **`SpecificationBase<T>`** (Domain) for criteria, string includes, and paging. Expression-based **`Include` / `ThenInclude`** belong in **`EfSpecification<T>`** / **`IEfSpecification<T>`** (Infrastructure); **`SpecificationEvaluator`** applies those only when the spec implements **`IEfSpecification<T>`**.
