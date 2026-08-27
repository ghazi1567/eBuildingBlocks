using eBlocksApi.Domain.Products;
using eBuildingBlocks.Common.Features;
using eBuildingBlocks.Domain.Interfaces;
using eBuildingBlocks.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace eBlocksApi.Infrastructure.Data;

// Derives from TenantAwareDbContext so this project is one config flag away from
// multi-tenant isolation (see docs/examples/multi-tenant-saas-quickstart.md in the
// eBuildingBlocks repo) — it's a no-op today since Product isn't a tenant entity and
// Features:MultiTenancy:Enabled is false by default in appsettings.json.
public class AppDbContext(
    DbContextOptions<AppDbContext> options,
    ICurrentUser currentUser,
    IOptions<MultiTenancyOptions> multiTenancy)
    : TenantAwareDbContext(options, currentUser, multiTenancy)
{
    public DbSet<Product> Products => Set<Product>();
}
