using eBuildingBlocks.ReferenceApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace eBuildingBlocks.ReferenceApp.API.Hosting;

public static class DatabaseExtensions
{
    /// <summary>Applies pending EF Core migrations in Development so the reference app is runnable from clone.</summary>
    public static async Task ApplyReferenceMigrationsAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
            return;

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ReferenceDbContext>();
        await db.Database.MigrateAsync().ConfigureAwait(false);
    }
}
