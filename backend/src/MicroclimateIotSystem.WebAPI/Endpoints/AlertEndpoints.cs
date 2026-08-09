using MicroclimateIotSystem.Application.Interfaces.Services;
using MicroclimateIotSystem.Application.Models;
using MicroclimateIotSystem.Domain.Enums;
using MicroclimateIotSystem.WebAPI.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace MicroclimateIotSystem.WebAPI.Endpoints;

public static class AlertEndpoints
{
    public static IEndpointRouteBuilder MapAlertEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/alerts")
                       .WithTags("Alerts")
                       .RequireAuthorization();

        group.MapGet("/", GetAllAsync).WithName("GetAlerts").WithOpenApi();

        return app;
    }

    private static async Task<IResult> GetAllAsync(
        [AsParameters] PagingQueryParams paging,
        [FromQuery] AlertStatus? status,
        [FromQuery] int? deviceId,
        [FromQuery] int? ruleId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        IAlertService alertService,
        CancellationToken cancellationToken)
    {
        var response = await alertService.GetAlertsAsync(paging, status, deviceId, ruleId, from, to, cancellationToken);
        return ResultHandler.Handle(response);
    }
}