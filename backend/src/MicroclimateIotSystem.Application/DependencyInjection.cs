using MicroclimateIotSystem.Application.Interfaces;
using MicroclimateIotSystem.Application.Interfaces.Services;
using MicroclimateIotSystem.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MicroclimateIotSystem.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDeviceService, DeviceService>();
        services.AddScoped<IRoomService, RoomService>();
        services.AddScoped<IAlertRuleService, AlertRuleService>();
        services.AddScoped<ITelemetryService, TelemetryService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IAlertService, AlertService>();
        services.AddScoped<IAlertEvaluator, AlertEvaluator>();
        return services;
    }
}
