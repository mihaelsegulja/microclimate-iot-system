using MicroclimateIotSystem.Application.Constants;
using MicroclimateIotSystem.Application.DTOs;
using MicroclimateIotSystem.Application.Extensions;
using MicroclimateIotSystem.Application.Interfaces;
using MicroclimateIotSystem.Application.Interfaces.Queue;
using MicroclimateIotSystem.Application.Interfaces.Services;
using MicroclimateIotSystem.Application.Models;
using MicroclimateIotSystem.Application.Models.Messaging;
using MicroclimateIotSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MicroclimateIotSystem.Application.Services;

public class DeviceService(IAppDbContext db, IMessageQueuePublisher publisher, ICacheService cache) : IDeviceService
{
    public async Task<PaginatedResponse<DeviceResponseDto>> GetDevicesAsync(
        PagingQueryParams paging, FilterQueryParams? filters, CancellationToken cancellationToken = default)
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
            .ToPaginatedResponseAsync(paging, cancellationToken);
    }

    public async Task<StandardResponse<DeviceResponseDto>> GetDeviceByIdAsync(
        int id, CancellationToken cancellationToken = default)
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
            .FirstOrDefaultAsync(cancellationToken);

        if (dto == null)
            return StandardResponse<DeviceResponseDto>.NotFound($"Device with id {id} not found.");

        return StandardResponse<DeviceResponseDto>.SuccessOk(dto);
    }

    public async Task<StandardResponse<int>> CreateDeviceAsync(
        CreateDeviceRequestDto request, CancellationToken cancellationToken = default)
    {
        var exists = await db.Devices.AnyAsync(d => d.HardwareId == request.HardwareId, cancellationToken);
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
        await db.SaveChangesAsync(cancellationToken);

        return StandardResponse<int>.SuccessCreated(device.Id, "Device created successfully.");
    }

    public async Task<StandardResponse<bool>> UpdateDeviceAsync(
        int id, UpdateDeviceRequestDto request, CancellationToken cancellationToken = default)
    {
        var device = await db.Devices.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (device == null)
            return StandardResponse<bool>.NotFound($"Device with id {id} not found.");

        if (!string.Equals(device.HardwareId, request.HardwareId, StringComparison.OrdinalIgnoreCase))
        {
            var duplicate = await db.Devices.AnyAsync(d => d.HardwareId == request.HardwareId, cancellationToken);
            if (duplicate)
                return StandardResponse<bool>.Failure(ResultStatus.Conflict, $"HardwareId '{request.HardwareId}' is already in use.");
        }

        device.Name = request.Name;
        device.HardwareId = request.HardwareId;
        device.IsActive = request.IsActive;
        device.RoomId = request.RoomId;

        await db.SaveChangesAsync(cancellationToken);

        cache.Remove(CacheKeys.DeviceActive(device.HardwareId));
        cache.Remove(CacheKeys.DeviceActive(request.HardwareId));

        return StandardResponse<bool>.SuccessOk(true, "Device updated successfully.");
    }

    public async Task<StandardResponse<bool>> DeleteDeviceAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var device = await db.Devices.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (device == null)
            return StandardResponse<bool>.NotFound($"Device with id {id} not found.");

        var ruleIds = await db.AlertRules
            .Where(r => r.DeviceId == id)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        var ruleIdSet = new HashSet<int>(ruleIds);

        var alerts = await db.Alerts
            .Where(a => a.DeviceId == id || ruleIdSet.Contains(a.AlertRuleId))
            .ToListAsync(cancellationToken);
        db.Alerts.RemoveRange(alerts);

        var rules = await db.AlertRules
            .Where(r => r.DeviceId == id)
            .ToListAsync(cancellationToken);
        db.AlertRules.RemoveRange(rules);

        db.Devices.Remove(device);
        await db.SaveChangesAsync(cancellationToken);

        cache.Remove(CacheKeys.DeviceActive(device.HardwareId));

        return StandardResponse<bool>.SuccessOk(true, "Device deleted successfully.");
    }

    public async Task<PaginatedResponse<LookupItemDto>> GetDevicesLookupAsync(
        LookupPagingQueryParams paging, FilterQueryParams? filters, bool? available = null,
        CancellationToken cancellationToken = default)
    {
        var query = db.Devices.AsNoTracking();

        if (available == true)
            query = query.Where(d => d.RoomId == null);

        return await query
            .Select(d => new LookupItemDto(d.Id, d.Name, d.IsActive))
            .ApplyFilters(filters?.FilterRules)
            .ToPaginatedResponseAsync(new PagingQueryParams(paging.Page, paging.PageSize), cancellationToken);
    }

    public async Task<StandardResponse<bool>> ToggleDeviceActiveAsync(
        int id, bool isActive, CancellationToken cancellationToken = default)
    {
        var device = await db.Devices.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (device == null)
            return StandardResponse<bool>.NotFound($"Device with id {id} not found.");

        device.IsActive = isActive;
        await db.SaveChangesAsync(cancellationToken);

        cache.Remove(CacheKeys.DeviceActive(device.HardwareId));

        return StandardResponse<bool>.SuccessOk(true, isActive ? "Device activated." : "Device deactivated.");
    }

    public async Task<StandardResponse<bool>> UpdateDeviceConfigAsync(
        int id, DeviceConfigRequestDto request, CancellationToken cancellationToken = default)
    {
        var device = await db.Devices.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (device == null)
            return StandardResponse<bool>.NotFound($"Device with id {id} not found.");

        device.TelemetryIntervalSeconds = request.TelemetryIntervalSeconds;
        await db.SaveChangesAsync(cancellationToken);

        var command = new DeviceCommandMessage(
            CommandId: Guid.NewGuid().ToString(),
            HardwareId: device.HardwareId,
            CommandType: "UPDATE_CONFIG",
            SentAt: DateTime.UtcNow,
            Payload: new { telemetryIntervalSeconds = request.TelemetryIntervalSeconds }
        );

        var routingKey = $"devices.{device.HardwareId}.commands";
        await publisher.PublishAsync(routingKey, command);

        return StandardResponse<bool>.SuccessOk(true, "Device configuration updated.");
    }

    public async Task<StandardResponse<bool>> RebootDeviceAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var device = await db.Devices.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (device == null)
            return StandardResponse<bool>.NotFound($"Device with id {id} not found.");

        var command = new DeviceCommandMessage(
            CommandId: Guid.NewGuid().ToString(),
            HardwareId: device.HardwareId,
            CommandType: "REBOOT",
            SentAt: DateTime.UtcNow,
            Payload: null
        );

        var routingKey = $"devices.{device.HardwareId}.commands";
        await publisher.PublishAsync(routingKey, command);

        return StandardResponse<bool>.SuccessOk(true, "Reboot command sent to device.");
    }
}
