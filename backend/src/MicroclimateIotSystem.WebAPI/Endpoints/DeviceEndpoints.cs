using System.Text.Json;
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
        PropertyNameCaseInsensitive = true
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

        return app;
    }

    private static async Task<IResult> GetLookupAsync(
        [AsParameters] LookupPagingQueryParams paging,
        [FromQuery] string? filters,
        [FromQuery] bool? available,
        IDeviceService deviceService)
    {
        FilterQueryParams? filterParams = null;
        if (!string.IsNullOrWhiteSpace(filters))
        {
            var rules = JsonSerializer.Deserialize<List<FilterRule>>(filters, JsonOptions);
            if (rules != null)
                filterParams = new FilterQueryParams(rules);
        }

        var response = await deviceService.GetDevicesLookupAsync(paging, filterParams, available);
        return ResultHandler.Handle(response);
    }

    private static async Task<IResult> GetAllAsync(
        [AsParameters] PagingQueryParams paging,
        [FromQuery] string? filters,
        IDeviceService deviceService)
    {
        FilterQueryParams? filterParams = null;
        if (!string.IsNullOrWhiteSpace(filters))
        {
            var rules = JsonSerializer.Deserialize<List<FilterRule>>(filters, JsonOptions);
            if (rules != null)
                filterParams = new FilterQueryParams(rules);
        }

        var response = await deviceService.GetDevicesAsync(paging, filterParams);
        return ResultHandler.Handle(response);
    }

    private static async Task<IResult> GetByIdAsync(
        int id,
        IDeviceService deviceService)
    {
        var response = await deviceService.GetDeviceByIdAsync(id);
        return ResultHandler.Handle(response);
    }

    private static async Task<IResult> CreateAsync(
        [FromBody] CreateDeviceRequestDto request,
        IDeviceService deviceService)
    {
        var response = await deviceService.CreateDeviceAsync(request);
        return ResultHandler.Handle(response);
    }

    private static async Task<IResult> UpdateAsync(
        int id,
        [FromBody] UpdateDeviceRequestDto request,
        IDeviceService deviceService)
    {
        var response = await deviceService.UpdateDeviceAsync(id, request);
        return ResultHandler.Handle(response);
    }

    private static async Task<IResult> DeleteAsync(
        int id,
        IDeviceService deviceService)
    {
        var response = await deviceService.DeleteDeviceAsync(id);
        return ResultHandler.Handle(response);
    }

    private static async Task<IResult> ToggleActiveAsync(
        int id,
        [FromBody] ToggleActiveRequestDto request,
        IDeviceService deviceService)
    {
        var response = await deviceService.ToggleDeviceActiveAsync(id, request.IsActive);
        return ResultHandler.Handle(response);
    }
}
