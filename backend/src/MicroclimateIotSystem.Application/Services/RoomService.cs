using MicroclimateIotSystem.Application.DTOs;
using MicroclimateIotSystem.Application.Extensions;
using MicroclimateIotSystem.Application.Interfaces;
using MicroclimateIotSystem.Application.Interfaces.Services;
using MicroclimateIotSystem.Application.Models;
using MicroclimateIotSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MicroclimateIotSystem.Application.Services;

public class RoomService(IAppDbContext db) : IRoomService
{
    public async Task<PaginatedResponse<RoomResponseDto>> GetRoomsAsync(
        PagingQueryParams paging, FilterQueryParams? filters, CancellationToken cancellationToken = default)
    {
        return await db.Rooms
            .AsNoTracking()
            .Select(r => new RoomResponseDto(
                r.Id,
                r.Name,
                r.Description,
                r.IsActive,
                r.Devices.Count()
            ))
            .ApplyFilters(filters?.FilterRules)
            .ToPaginatedResponseAsync(paging, cancellationToken);
    }

    public async Task<StandardResponse<RoomResponseDto>> GetRoomByIdAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var dto = await db.Rooms
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new RoomResponseDto(
                r.Id,
                r.Name,
                r.Description,
                r.IsActive,
                r.Devices.Count()
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (dto == null)
            return StandardResponse<RoomResponseDto>.NotFound($"Room with id {id} not found.");

        return StandardResponse<RoomResponseDto>.SuccessOk(dto);
    }

    public async Task<PaginatedResponse<DeviceItemDto>> GetRoomDevicesAsync(
        int roomId, PagingQueryParams paging, FilterQueryParams? filters,
        CancellationToken cancellationToken = default)
    {
        var roomExists = await db.Rooms.AnyAsync(r => r.Id == roomId, cancellationToken);
        if (!roomExists)
            return PaginatedResponse<DeviceItemDto>.Create(
                ResultStatus.NotFound, [], paging.Page, paging.PageSize, 0, "Room not found.");

        return await db.Devices
            .AsNoTracking()
            .Where(d => d.RoomId == roomId)
            .Select(d => new DeviceItemDto(d.Id, d.HardwareId, d.Name))
            .ApplyFilters(filters?.FilterRules)
            .ToPaginatedResponseAsync(paging, cancellationToken);
    }

    public async Task<StandardResponse<bool>> AssignDevicesToRoomAsync(
        int roomId, AssignDevicesRequestDto request, CancellationToken cancellationToken = default)
    {
        var roomExists = await db.Rooms.AnyAsync(r => r.Id == roomId, cancellationToken);
        if (!roomExists)
            return StandardResponse<bool>.NotFound($"Room with id {roomId} not found.");

        var devices = await db.Devices
            .Where(d => request.DeviceIds.Contains(d.Id))
            .ToListAsync(cancellationToken);

        foreach (var device in devices)
        {
            device.RoomId = roomId;
        }

        await db.SaveChangesAsync(cancellationToken);

        return StandardResponse<bool>.SuccessOk(true, $"{devices.Count} device(s) assigned to room successfully.");
    }

    public async Task<StandardResponse<int>> CreateRoomAsync(
        CreateRoomRequestDto request, CancellationToken cancellationToken = default)
    {
        var room = new Room
        {
            Name = request.Name,
            Description = request.Description,
            IsActive = true
        };

        db.Rooms.Add(room);

        if (request.DeviceIds?.Count > 0)
        {
            var devices = await db.Devices
                .Where(d => request.DeviceIds.Contains(d.Id))
                .ToListAsync(cancellationToken);

            foreach (var device in devices)
            {
                device.RoomId = room.Id;
            }
        }
        
        await db.SaveChangesAsync(cancellationToken);

        return StandardResponse<int>.SuccessCreated(room.Id, "Room created successfully.");
    }

    public async Task<StandardResponse<bool>> UpdateRoomAsync(
        int id, UpdateRoomRequestDto request, CancellationToken cancellationToken = default)
    {
        var room = await db.Rooms.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (room == null)
            return StandardResponse<bool>.NotFound($"Room with id {id} not found.");

        room.Name = request.Name;
        room.Description = request.Description;
        room.IsActive = request.IsActive;

        await db.SaveChangesAsync(cancellationToken);

        return StandardResponse<bool>.SuccessOk(true, "Room updated successfully.");
    }

    public async Task<StandardResponse<bool>> DeleteRoomAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var room = await db.Rooms.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (room == null)
            return StandardResponse<bool>.NotFound($"Room with id {id} not found.");

        db.Rooms.Remove(room);
        await db.SaveChangesAsync(cancellationToken);

        return StandardResponse<bool>.SuccessOk(true, "Room deleted successfully.");
    }

    public async Task<PaginatedResponse<LookupItemDto>> GetRoomsLookupAsync(
        LookupPagingQueryParams paging, FilterQueryParams? filters, CancellationToken cancellationToken = default)
    {
        return await db.Rooms
            .AsNoTracking()
            .Select(r => new LookupItemDto(r.Id, r.Name, r.IsActive))
            .ApplyFilters(filters?.FilterRules)
            .ToPaginatedResponseAsync(new PagingQueryParams(paging.Page, paging.PageSize), cancellationToken);
    }

    public async Task<StandardResponse<bool>> ToggleRoomActiveAsync(
        int id, bool isActive, CancellationToken cancellationToken = default)
    {
        var room = await db.Rooms.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (room == null)
            return StandardResponse<bool>.NotFound($"Room with id {id} not found.");

        room.IsActive = isActive;
        await db.SaveChangesAsync(cancellationToken);

        return StandardResponse<bool>.SuccessOk(true, isActive ? "Room activated." : "Room deactivated.");
    }
}
