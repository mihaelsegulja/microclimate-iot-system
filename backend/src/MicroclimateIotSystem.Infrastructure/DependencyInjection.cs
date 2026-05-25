using MicroclimateIotSystem.Application.Common.Interfaces.Security;
using MicroclimateIotSystem.Application.Interfaces.Common;
using MicroclimateIotSystem.Domain.Abstractions;
using MicroclimateIotSystem.Domain.Interfaces;
using MicroclimateIotSystem.Infrastructure.Abstractions;
using MicroclimateIotSystem.Infrastructure.Repositories;
using MicroclimateIotSystem.Infrastructure.Security.Helpers;
using MicroclimateIotSystem.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MicroclimateIotSystem.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Data Access
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("Db"), 
                x => x.MigrationsAssembly("MicroclimateIotSystem.Infrastructure")));

        services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        // 2. Services & Helpers
        services.AddScoped<IPasswordHelper, PasswordHelper>();
        services.AddScoped<ITokenHelper, TokenHelper>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        return services;
    }
}
