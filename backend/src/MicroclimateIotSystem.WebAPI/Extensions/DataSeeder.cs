using MicroclimateIotSystem.Application.Common.Interfaces.Security;
using MicroclimateIotSystem.Domain.Entities;
using MicroclimateIotSystem.Domain.Enums;
using MicroclimateIotSystem.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MicroclimateIotSystem.WebAPI.Extensions;

public static class DataSeeder
{
    public static async Task SeedDataAsync(this IHost app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHelper = scope.ServiceProvider.GetRequiredService<IPasswordHelper>();

        if (!await context.Users.AnyAsync(u => u.Username == "admin"))
        {
            var salt = passwordHelper.GenerateSalt();
            var hash = passwordHelper.HashPassword("admin", salt);

            context.Users.Add(new User
            {
                Username = "admin",
                PasswordHash = hash,
                PasswordSalt = salt,
                Role = Roles.Admin
            });
        }

        if (!await context.Users.AnyAsync(u => u.Username == "user"))
        {
            var salt = passwordHelper.GenerateSalt();
            var hash = passwordHelper.HashPassword("user", salt);

            context.Users.Add(new User
            {
                Username = "user",
                PasswordHash = hash,
                PasswordSalt = salt,
                Role = Roles.User
            });
        }

        await context.SaveChangesAsync();
    }
}

