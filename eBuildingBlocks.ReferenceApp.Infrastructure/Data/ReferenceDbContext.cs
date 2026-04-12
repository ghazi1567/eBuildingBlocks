using eBuildingBlocks.Common.Features;
using eBuildingBlocks.Domain.Interfaces;
using eBuildingBlocks.Infrastructure.Outbox;
using eBuildingBlocks.ReferenceApp.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace eBuildingBlocks.ReferenceApp.Infrastructure.Data;

/// <summary>
/// Application database. Outbox + multi-tenant indexes are configured here alongside the B2B order model.
/// </summary>
public sealed class ReferenceDbContext(
    DbContextOptions<ReferenceDbContext> options,
    ICurrentUser currentUser,
    IOptions<MultiTenancyOptions> multiTenancyOptions)
    : DefaultDbContext(options, currentUser, multiTenancyOptions)
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Maps OutboxMessages and aligns with DomainOutboxSaveChangesInterceptor expectations.
        modelBuilder.ConfigureDomainOutbox();

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrderNumber).HasMaxLength(64).IsRequired();
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.HasIndex(e => new { e.TenantId, e.OrderNumber }).IsUnique();
            entity.Property(e => e.RowVersion).IsRowVersion();
        });
    }
}
