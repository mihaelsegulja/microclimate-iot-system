using System.Text.Json;
using System.Text.Json.Serialization;
using MicroclimateIotSystem.Application.DTOs;
using MicroclimateIotSystem.Application.Interfaces.Services;
using MicroclimateIotSystem.Application.Models;
using MicroclimateIotSystem.WebAPI.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace MicroclimateIotSystem.WebAPI.Endpoints;

public static class DeviceEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static IEndpointRouteBuilder MapDeviceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/devices")
                       .WithTags("Devices")
                       .RequireAuthorization();

        group.MapGet("/lookup", GetLookupAsync).WithName("GetDevicesLookup").WithOpenApi();
        group.MapGet("/", GetAllAsync).WithName("GetDevices").WithOpenApi();
        group.MapGet("/{id:int}", GetByIdAsync).WithName("GetDeviceById").WithOpenApi();
        group.MapPost("/", CreateAsync).WithName("CreateDevice").WithOpenApi();
        group.MapPut("/{id:int}", UpdateAsync).WithName("UpdateDevice").WithOpenApi();
        group.MapDelete("/{id:int}", DeleteAsync).WithName("DeleteDevice").WithOpenApi();
        group.MapPatch("/{id:int}/active", ToggleActiveAsync).WithName("ToggleDeviceActive").WithOpenApi();
        group.MapPut("/{id:int}/config", UpdateConfigAsync).WithName("UpdateDeviceConfig").WithOpenApi();
        group.MapPost("/{id:int}/reboot", RebootAsync).WithName("RebootDevice").WithOpenApi();
        group.MapGet("/{id:int}/telemetry", GetTelemetryAsync).WithName("GetDeviceTelemetry").WithOpenApi();
        group.MapGet("/{id:int}/telemetry/aggregate", GetAggregatedTelemetryAsync).WithName("GetDeviceAggregatedTelemetry").WithOpenApi();
        group.MapGet("/{id:int}/telemetry/latest", GetLatestTelemetryAsync).WithName("GetDeviceLatestTelemetry").WithOpenApi();

        return app;
    }

    private static async Task<IResult> GetLookupAsync(
        [AsParameters] LookupPagingQueryParams paging,
        [FromQuery] string? filters,
        [FromQuery] bool? available,
        IDeviceService deviceService,
        CancellationToken cancellationToken)
    {
        FilterQueryParams? filterParams = null;
        if (!string.IsNullOrWhiteSpace(filters))
        {
            var rules = JsonSerializer.Deserialize<List<FilterRule>>(filters, JsonOptions);
            if (rules != null)
                filterParams = new FilterQueryParams(rules);
        }

        var response = await deviceService.GetDevicesLookupAsync(paging, filterParams, available, cancellationToken);
        return ResultHandler.Handle(response);
    }

    private static async Task<IResult> GetAllAsync(
        [AsParameters] PagingQueryParams paging,
        [FromQuery] string? filters,
        IDeviceService deviceService,
        CancellationToken cancellationToken)
    {
        FilterQueryParams? filterParams = null;
        if (!string.IsNullOrWhiteSpace(filters))
        {
            var rules = JsonSerializer.Deserialize<List<FilterRule>>(filters, JsonOptions);
            if (rules != null)
                filterParams = new FilterQueryParams(rules);
        }

        var response = await deviceService.GetDevicesAsync(paging, filterParams, cancellationToken);
        return ResultHandler.Handle(response);
    }

    private static async Task<IResult> GetByIdAsync(
        int id,
        IDeviceService deviceService,
        CancellationToken cancellationToken)
    {
        var response = await deviceService.GetDeviceByIdAsync(id, cancellationToken);
        return ResultHandler.Handle(response);
    }

    private static async Task<IResult> CreateAsync(
        [FromBody] CreateDeviceRequestDto request,
        IDeviceService deviceService,
        CancellationToken cancellationToken)
    {
        var response = await deviceService.CreateDeviceAsync(request, cancellationToken);
        return ResultHandler.Handle(response);
    }

    private static async Task<IResult> UpdateAsync(
        int id,
        [FromBody] UpdateDeviceRequestDto request,
        IDeviceService deviceService,
        CancellationToken cancellationToken)
    {
        var response = await deviceService.UpdateDeviceAsync(id, request, cancellationToken);
        return ResultHandler.Handle(response);
    }

    private static async Task<IResult> DeleteAsync(
        int id,
        IDeviceService deviceService,
        CancellationToken cancellationToken)
    {
        var response = await deviceService.DeleteDeviceAsync(id, cancellationToken);
        return ResultHandler.Handle(response);
    }

    private static async Task<IResult> ToggleActiveAsync(
        int id,
        [FromBody] ToggleActiveRequestDto request,
        IDeviceService deviceService,
        CancellationToken cancellationToken)
    {
        var response = await deviceService.ToggleDeviceActiveAsync(id, request.IsActive, cancellationToken);
        return ResultHandler.Handle(response);
    }

    private static async Task<IResult> UpdateConfigAsync(
        int id,
        [FromBody] DeviceConfigRequestDto request,
        IDeviceService deviceService,
        CancellationToken cancellationToken)
    {
        var response = await deviceService.UpdateDeviceConfigAsync(id, request, cancellationToken);
        return ResultHandler.Handle(response);
    }

    private static async Task<IResult> RebootAsync(
        int id,
        IDeviceService deviceService,
        CancellationToken cancellationToken)
    {
        var response = await deviceService.RebootDeviceAsync(id, cancellationToken);
        return ResultHandler.Handle(response);
    }

    private static async Task<IResult> GetTelemetryAsync(
        int id,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? keys,
        ITelemetryService telemetryService,
        CancellationToken cancellationToken)
    {
        var response = await telemetryService.GetChartAsync(id, from, to, ParseKeys(keys), cancellationToken);
        return ResultHandler.Handle(response);
    }

    private static async Task<IResult> GetLatestTelemetryAsync(
        int id,
        ITelemetryService telemetryService,
        CancellationToken cancellationToken)
    {
        var response = await telemetryService.GetLatestAsync(id, cancellationToken);
        return ResultHandler.Handle(response);
    }

    private static async Task<IResult> GetAggregatedTelemetryAsync(
        int id,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? keys,
        [FromQuery] int maxPoints = 150,
        ITelemetryService telemetryService = null!,
        CancellationToken cancellationToken = default)
    {
        var response = await telemetryService.GetAggregatedChartAsync(id, from, to, ParseKeys(keys), maxPoints, cancellationToken);
        return ResultHandler.Handle(response);
    }

    private static IReadOnlyList<string>? ParseKeys(string? keys)
    {
        if (string.IsNullOrWhiteSpace(keys))
            return null;

        return keys.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
