using MicroclimateIotSystem.Application.DTOs;
using MicroclimateIotSystem.Application.Extensions;
using MicroclimateIotSystem.Application.Interfaces;
using MicroclimateIotSystem.Application.Interfaces.Services;
using MicroclimateIotSystem.Application.Models;
using MicroclimateIotSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MicroclimateIotSystem.Application.Services;

public class DeviceService(IAppDbContext db) : IDeviceService
{
    public async Task<PaginatedResponse<DeviceResponseDto>> GetDevicesAsync(
        PagingQueryParams paging, FilterQueryParams? filters)
    {
        return await db.Devices
            .AsNoTracking()
            .Select(d => new DeviceResponseDto(
                d.Id,
                d.Name,
                d.HardwareId,
                d.IsActive,
                d.TelemetryIntervalSeconds,
                d.RoomId,
                d.Room.Name
            ))
            .ApplyFilters(filters?.FilterRules)
            .ToPaginatedResponseAsync(paging);
    }

    public async Task<StandardResponse<DeviceResponseDto>> GetDeviceByIdAsync(int id)
    {
        var dto = await db.Devices
            .AsNoTracking()
            .Where(d => d.Id == id)
            .Select(d => new DeviceResponseDto(
                d.Id,
                d.Name,
                d.HardwareId,
                d.IsActive,
                d.TelemetryIntervalSeconds,
                d.RoomId,
                d.Room.Name
            ))
            .FirstOrDefaultAsync();

        if (dto == null)
            return StandardResponse<DeviceResponseDto>.NotFound($"Device with id {id} not found.");

        return StandardResponse<DeviceResponseDto>.SuccessOk(dto);
    }

    public async Task<StandardResponse<int>> CreateDeviceAsync(CreateDeviceRequestDto request)
    {
        var exists = await db.Devices.AnyAsync(d => d.HardwareId == request.HardwareId);
        if (exists)
            return StandardResponse<int>.Failure(ResultStatus.Conflict, $"Device with HardwareId '{request.HardwareId}' already exists.");

        var device = new Device
        {
            Name = request.Name,
            HardwareId = request.HardwareId,
            RoomId = request.RoomId,
            IsActive = true,
            TelemetryIntervalSeconds = 60
        };

        db.Devices.Add(device);
        await db.SaveChangesAsync();

        return StandardResponse<int>.SuccessCreated(device.Id, "Device created successfully.");
    }

    public async Task<StandardResponse<bool>> UpdateDeviceAsync(int id, UpdateDeviceRequestDto request)
    {
        var device = await db.Devices.FirstOrDefaultAsync(d => d.Id == id);
        if (device == null)
            return StandardResponse<bool>.NotFound($"Device with id {id} not found.");

        if (!string.Equals(device.HardwareId, request.HardwareId, StringComparison.OrdinalIgnoreCase))
        {
            var duplicate = await db.Devices.AnyAsync(d => d.HardwareId == request.HardwareId);
            if (duplicate)
                return StandardResponse<bool>.Failure(ResultStatus.Conflict, $"HardwareId '{request.HardwareId}' is already in use.");
        }

        device.Name = request.Name;
        device.HardwareId = request.HardwareId;
        device.IsActive = request.IsActive;
        device.TelemetryIntervalSeconds = request.TelemetryIntervalSeconds;
        device.RoomId = request.RoomId;

        await db.SaveChangesAsync();

        return StandardResponse<bool>.SuccessOk(true, "Device updated successfully.");
    }

    public async Task<StandardResponse<bool>> DeleteDeviceAsync(int id)
    {
        var device = await db.Devices.FirstOrDefaultAsync(d => d.Id == id);
        if (device == null)
            return StandardResponse<bool>.NotFound($"Device with id {id} not found.");

        db.Devices.Remove(device);
        await db.SaveChangesAsync();

        return StandardResponse<bool>.SuccessOk(true, "Device deleted successfully.");
    }

    public async Task<PaginatedResponse<LookupItemDto>> GetDevicesLookupAsync(
        LookupPagingQueryParams paging, FilterQueryParams? filters, bool? available = null)
    {
        var query = db.Devices.AsNoTracking();

        if (available == true)
            query = query.Where(d => d.RoomId == null);

        return await query
            .Select(d => new LookupItemDto(d.Id, d.Name, d.IsActive))
            .ApplyFilters(filters?.FilterRules)
            .ToPaginatedResponseAsync(new PagingQueryParams(paging.Page, paging.PageSize));
    }

    public async Task<StandardResponse<bool>> ToggleDeviceActiveAsync(int id, bool isActive)
    {
        var device = await db.Devices.FirstOrDefaultAsync(d => d.Id == id);
        if (device == null)
            return StandardResponse<bool>.NotFound($"Device with id {id} not found.");

        device.IsActive = isActive;
        await db.SaveChangesAsync();

        return StandardResponse<bool>.SuccessOk(true, isActive ? "Device activated." : "Device deactivated.");
    }
}
