using eBuildingBlocks.Common.Features;
using eBuildingBlocks.Domain.Interfaces;
using eBuildingBlocks.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace eBuildingBlocks.Infrastructure.Data;

/// <summary>
/// DbContext base that applies runtime EF global query filters for <see cref="ITenantEntity"/> when multi-tenancy is enabled,
/// using the scoped <see cref="ICurrentUser.TenantId"/> captured from the closure (evaluated per query).
/// </summary>
public abstract class TenantAwareDbContext : DbContext
{
    private readonly ICurrentUser _currentUser;
    private readonly IOptions<MultiTenancyOptions> _multiTenancy;

    protected TenantAwareDbContext(
        DbContextOptions options,
        ICurrentUser currentUser,
        IOptions<MultiTenancyOptions> multiTenancy)
        : base(options)
    {
        _currentUser = currentUser;
        _multiTenancy = multiTenancy;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        if (_multiTenancy.Value.Enabled)
            modelBuilder.ApplyRuntimeTenantQueryFilters(_currentUser);
        modelBuilder.ApplyTenantEntityIndexes();
    }
}
