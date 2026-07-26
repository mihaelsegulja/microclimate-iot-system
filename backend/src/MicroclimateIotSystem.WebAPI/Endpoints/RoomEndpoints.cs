using System.Text.Json;
using MicroclimateIotSystem.Application.DTOs;
using MicroclimateIotSystem.Application.Interfaces.Services;
using MicroclimateIotSystem.Application.Models;
using MicroclimateIotSystem.WebAPI.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace MicroclimateIotSystem.WebAPI.Endpoints;

public static class RoomEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static IEndpointRouteBuilder MapRoomEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/rooms")
                       .WithTags("Rooms")
                       .RequireAuthorization();

        group.MapGet("/lookup", GetLookupAsync).WithName("GetRoomsLookup").WithOpenApi();
        group.MapGet("/", GetAllAsync).WithName("GetRooms").WithOpenApi();
        group.MapGet("/{id:int}", GetByIdAsync).WithName("GetRoomById").WithOpenApi();
        group.MapGet("/{id:int}/devices", GetDevicesAsync).WithName("GetRoomDevices").WithOpenApi();
        group.MapPost("/", CreateAsync).WithName("CreateRoom").WithOpenApi();
        group.MapPut("/{id:int}", UpdateAsync).WithName("UpdateRoom").WithOpenApi();
        group.MapDelete("/{id:int}", DeleteAsync).WithName("DeleteRoom").WithOpenApi();
        group.MapPatch("/{id:int}/active", ToggleActiveAsync).WithName("ToggleRoomActive").WithOpenApi();
        group.MapPost("/{id:int}/devices", AssignDevicesAsync).WithName("AssignDevicesToRoom").WithOpenApi();

        return app;
    }

    private static async Task<IResult> GetLookupAsync(
        [AsParameters] LookupPagingQueryParams paging,
        [FromQuery] string? filters,
        IRoomService roomService)
    {
        FilterQueryParams? filterParams = null;
        if (!string.IsNullOrWhiteSpace(filters))
        {
            var rules = JsonSerializer.Deserialize<List<FilterRule>>(filters, JsonOptions);
            if (rules != null)
                filterParams = new FilterQueryParams(rules);
        }

        var response = await roomService.GetRoomsLookupAsync(paging, filterParams);
        return ResultHandler.Handle(response);
    }

    private static async Task<IResult> GetAllAsync(
        [AsParameters] PagingQueryParams paging,
        [FromQuery] string? filters,
        IRoomService roomService)
    {
        FilterQueryParams? filterParams = null;
        if (!string.IsNullOrWhiteSpace(filters))
        {
            var rules = JsonSerializer.Deserialize<List<FilterRule>>(filters, JsonOptions);
            if (rules != null)
                filterParams = new FilterQueryParams(rules);
        }

        var response = await roomService.GetRoomsAsync(paging, filterParams);
        return ResultHandler.Handle(response);
    }

    private static async Task<IResult> GetByIdAsync(
        int id,
        IRoomService roomService)
    {
        var response = await roomService.GetRoomByIdAsync(id);
        return ResultHandler.Handle(response);
    }

    private static async Task<IResult> GetDevicesAsync(
        int id,
        [AsParameters] PagingQueryParams paging,
        [FromQuery] string? filters,
        IRoomService roomService)
    {
        FilterQueryParams? filterParams = null;
        if (!string.IsNullOrWhiteSpace(filters))
        {
            var rules = JsonSerializer.Deserialize<List<FilterRule>>(filters, JsonOptions);
            if (rules != null)
                filterParams = new FilterQueryParams(rules);
        }

        var response = await roomService.GetRoomDevicesAsync(id, paging, filterParams);
        return ResultHandler.Handle(response);
    }

    private static async Task<IResult> CreateAsync(
        [FromBody] CreateRoomRequestDto request,
        IRoomService roomService)
    {
        var response = await roomService.CreateRoomAsync(request);
        return ResultHandler.Handle(response);
    }

    private static async Task<IResult> UpdateAsync(
        int id,
        [FromBody] UpdateRoomRequestDto request,
        IRoomService roomService)
    {
        var response = await roomService.UpdateRoomAsync(id, request);
        return ResultHandler.Handle(response);
    }

    private static async Task<IResult> DeleteAsync(
        int id,
        IRoomService roomService)
    {
        var response = await roomService.DeleteRoomAsync(id);
        return ResultHandler.Handle(response);
    }

    private static async Task<IResult> AssignDevicesAsync(
        int id,
        [FromBody] AssignDevicesRequestDto request,
        IRoomService roomService)
    {
        var response = await roomService.AssignDevicesToRoomAsync(id, request);
        return ResultHandler.Handle(response);
    }

    private static async Task<IResult> ToggleActiveAsync(
        int id,
        [FromBody] ToggleActiveRequestDto request,
        IRoomService roomService)
    {
        var response = await roomService.ToggleRoomActiveAsync(id, request.IsActive);
        return ResultHandler.Handle(response);
    }
}
