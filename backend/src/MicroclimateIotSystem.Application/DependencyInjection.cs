using MicroclimateIotSystem.Application.Interfaces.Services;
using MicroclimateIotSystem.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MicroclimateIotSystem.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }
}
