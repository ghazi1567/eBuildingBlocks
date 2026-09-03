using eBuildingBlocks.Domain.Interfaces;
using eBuildingBlocks.Domain.Models;
using eBuildingBlocks.Infrastructure.Implementations;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace eBuildingBlocks.Infrastructure.Tests.Auditing;

/// <summary>
/// Regression coverage for TODO.md P-2: <see cref="AuditSaveChangesInterceptor"/> used to query
/// <c>ChangeTracker.Entries&lt;AuditableEntity&lt;Guid&gt;&gt;()</c>, so any entity keyed by something
/// other than <see cref="Guid"/> was silently never stamped. It now queries the non-generic
/// <see cref="IAuditableEntity"/> marker instead.
/// </summary>
public class AuditSaveChangesInterceptorTests
{
    private sealed class GuidKeyedEntity : AuditableEntity<Guid>
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class IntKeyedEntity : AuditableEntity<int>
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class LongKeyedEntity : AuditableEntity<long>
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class StringKeyedEntity : AuditableEntity<string>
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<GuidKeyedEntity> GuidKeyedEntities => Set<GuidKeyedEntity>();
        public DbSet<IntKeyedEntity> IntKeyedEntities => Set<IntKeyedEntity>();
        public DbSet<LongKeyedEntity> LongKeyedEntities => Set<LongKeyedEntity>();
        public DbSet<StringKeyedEntity> StringKeyedEntities => Set<StringKeyedEntity>();

        // AuditSaveChangesInterceptor also writes AuditLog rows on every SaveChanges (PrepareAuditLogs);
        // it must be part of the model for that to work. AuditLog itself declares no key property,
        // so every host must configure one (a shadow key here, matching what a real host also has to do).
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<AuditLog>(builder =>
            {
                builder.Property<int>("Id").ValueGeneratedOnAdd();
                builder.HasKey("Id");
            });
        }
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public string? UserId => null;
        public string? UserEmail => null;
        public string? IPAddress => "127.0.0.1";
        public string? UserName => "test-user";
        public Guid TenantId => Guid.Empty;
        public string UserAgent => "xunit";
    }

    private static TestDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(dbName)
            .AddInterceptors(new AuditSaveChangesInterceptor(new FakeCurrentUser()))
            .Options;

        return new TestDbContext(options);
    }

    [Fact]
    public async Task SaveChanges_Stamps_GuidKeyedEntity_OnAdd()
    {
        await using var context = CreateContext(nameof(SaveChanges_Stamps_GuidKeyedEntity_OnAdd));

        var entity = new GuidKeyedEntity { Id = Guid.NewGuid(), Name = "widget" };
        context.GuidKeyedEntities.Add(entity);

        await context.SaveChangesAsync();

        Assert.NotEqual(default, entity.CreatedOn);
        Assert.Equal("test-user", entity.CreatedBy);
    }

    [Fact]
    public async Task SaveChanges_Stamps_IntKeyedEntity_OnAdd()
    {
        // Before the P-2 fix, this entity would save successfully but CreatedOn/CreatedBy
        // would stay at their default values forever, because the interceptor only ever
        // looked at Entries<AuditableEntity<Guid>>().
        await using var context = CreateContext(nameof(SaveChanges_Stamps_IntKeyedEntity_OnAdd));

        var entity = new IntKeyedEntity { Id = 1, Name = "widget" };
        context.IntKeyedEntities.Add(entity);

        await context.SaveChangesAsync();

        Assert.NotEqual(default, entity.CreatedOn);
        Assert.Equal("test-user", entity.CreatedBy);
    }

    [Fact]
    public async Task SaveChanges_Stamps_LongKeyedEntity_OnAdd()
    {
        await using var context = CreateContext(nameof(SaveChanges_Stamps_LongKeyedEntity_OnAdd));

        var entity = new LongKeyedEntity { Id = 1L, Name = "widget" };
        context.LongKeyedEntities.Add(entity);

        await context.SaveChangesAsync();

        Assert.NotEqual(default, entity.CreatedOn);
        Assert.Equal("test-user", entity.CreatedBy);
    }

    [Fact]
    public async Task SaveChanges_Stamps_StringKeyedEntity_OnAdd()
    {
        await using var context = CreateContext(nameof(SaveChanges_Stamps_StringKeyedEntity_OnAdd));

        var entity = new StringKeyedEntity { Id = "widget-1", Name = "widget" };
        context.StringKeyedEntities.Add(entity);

        await context.SaveChangesAsync();

        Assert.NotEqual(default, entity.CreatedOn);
        Assert.Equal("test-user", entity.CreatedBy);
    }

    [Fact]
    public async Task SaveChanges_Stamps_IntKeyedEntity_OnModify()
    {
        await using var context = CreateContext(nameof(SaveChanges_Stamps_IntKeyedEntity_OnModify));

        var entity = new IntKeyedEntity { Id = 1, Name = "widget" };
        context.IntKeyedEntities.Add(entity);
        await context.SaveChangesAsync();

        entity.Name = "renamed";
        await context.SaveChangesAsync();

        Assert.NotNull(entity.ModifiedOn);
        Assert.Equal("test-user", entity.ModifiedBy);
    }
}
