using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

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
        return new ReferenceDbContext(optionsBuilder.Options);
    }
}
