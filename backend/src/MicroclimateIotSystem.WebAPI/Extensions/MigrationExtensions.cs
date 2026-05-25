using MicroclimateIotSystem.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MicroclimateIotSystem.WebAPI.Extensions;

public static class MigrationExtensions
{
    public static void ApplyMigrations(this IHost app) 
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Migrate() will automatically create the database if it doesn't exist,
        // and apply any pending migrations.
        context.Database.Migrate();
    }
}