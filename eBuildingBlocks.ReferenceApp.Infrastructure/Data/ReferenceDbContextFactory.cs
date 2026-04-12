using eBuildingBlocks.Common.Features;
using eBuildingBlocks.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Options;

namespace eBuildingBlocks.ReferenceApp.Infrastructure.Data;

/// <summary>Design-time factory for <c>dotnet ef migrations</c> (defaults match appsettings.json).</summary>
public sealed class ReferenceDbContextFactory : IDesignTimeDbContextFactory<ReferenceDbContext>
{
    private const string DesignTimeConnection =
        "Server=(localdb)\\mssqllocaldb;Database=eBuildingBlocks.ReferenceApp;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

    public ReferenceDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("REFERENCE_APP_CONNECTION") ?? DesignTimeConnection;
        var optionsBuilder = new DbContextOptionsBuilder<ReferenceDbContext>();
        optionsBuilder.UseSqlServer(cs);
        // Migrations: disable tenant query filters at design time so the model is stable.
        var mt = Options.Create(new MultiTenancyOptions { Enabled = false });
        return new ReferenceDbContext(optionsBuilder.Options, DesignTimeCurrentUser.Instance, mt);
    }

    private sealed class DesignTimeCurrentUser : ICurrentUser
    {
        internal static readonly DesignTimeCurrentUser Instance = new();

        public string? UserId => null;
        public string? UserEmail => null;
        public string IPAddress => "0.0.0.0";
        public string UserName => "ef-design-time";
        public Guid TenantId => Guid.Empty;
        public string UserAgent => string.Empty;
    }
}
