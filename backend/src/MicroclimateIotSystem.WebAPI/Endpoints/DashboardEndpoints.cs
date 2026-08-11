using MicroclimateIotSystem.Application.Interfaces.Services;
using MicroclimateIotSystem.WebAPI.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace MicroclimateIotSystem.WebAPI.Endpoints;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dashboard")
                       .WithTags("Dashboard")
                       .RequireAuthorization();

        group.MapGet("/telemetry", GetRoomTelemetryAsync).WithName("GetDashboardRoomTelemetry").WithOpenApi();

        return app;
    }

    private static async Task<IResult> GetRoomTelemetryAsync(
        [FromQuery] int roomId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int maxPoints = 150,
        IDashboardService dashboardService = null!,
        CancellationToken cancellationToken = default)
    {
        var response = await dashboardService.GetRoomTelemetryAsync(roomId, from, to, maxPoints, cancellationToken);
        return ResultHandler.Handle(response);
    }
}