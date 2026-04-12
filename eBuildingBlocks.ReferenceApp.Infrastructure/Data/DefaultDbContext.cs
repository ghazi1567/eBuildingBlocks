using eBuildingBlocks.Common.Features;
using eBuildingBlocks.Domain.Interfaces;
using eBuildingBlocks.Domain.Models;
using eBuildingBlocks.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace eBuildingBlocks.ReferenceApp.Infrastructure.Data;

/// <summary>
/// Shared application DbContext base from which <see cref="ReferenceDbContext"/> derives. The framework README
/// refers to this pattern as "DefaultDBContext": one place for cross-cutting sets such as <see cref="AuditLog"/>
/// that <see cref="eBuildingBlocks.Infrastructure.Implementations.AuditSaveChangesInterceptor"/> appends on each save.
/// </summary>
public abstract class DefaultDbContext : TenantAwareDbContext
{
    protected DefaultDbContext(
        DbContextOptions options,
        ICurrentUser currentUser,
        IOptions<MultiTenancyOptions> multiTenancyOptions)
        : base(options, currentUser, multiTenancyOptions)
    {
    }

    /// <summary>Row-level change history produced by the audit interceptor (shadow key for EF inserts).</summary>
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // AuditLog is a persistence concern; shadow PK keeps the domain model unchanged.
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.Property<Guid>("AuditLogId").ValueGeneratedOnAdd();
            entity.HasKey("AuditLogId");
        });
    }
}
