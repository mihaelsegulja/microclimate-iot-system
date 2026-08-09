using MicroclimateIotSystem.Application.Constants;
using MicroclimateIotSystem.Application.DTOs;
using MicroclimateIotSystem.Application.Interfaces;
using MicroclimateIotSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MicroclimateIotSystem.Infrastructure.Messaging;

public class SensorDataProcessor(
    IAppDbContext db,
    ICacheService cache,
    ITelemetryBroadcaster broadcaster,
    IAlertEvaluator alertEvaluator,
    ILogger<SensorDataProcessor> logger)
    : ISensorDataProcessor
{
    private static readonly TimeSpan ActiveDeviceCacheTtl = TimeSpan.FromMinutes(10);

    public async Task ProcessAsync(TelemetryReadingDto message, CancellationToken cancellationToken = default)
    {
        var (found, isActive, deviceId, roomId) = await GetDeviceStateAsync(message.HardwareId, cancellationToken);

        if (!found)
        {
            await RegisterDeviceAsync(message.HardwareId, cancellationToken);
            logger.LogInformation("Auto-registered device {HardwareId} as inactive", message.HardwareId);
            throw new InvalidOperationException($"Device '{message.HardwareId}' is registered but inactive; telemetry rejected.");
        }

        if (!isActive)
        {
            logger.LogWarning("Device {HardwareId} is inactive", message.HardwareId);
            throw new InvalidOperationException($"Device '{message.HardwareId}' is inactive.");
        }

        var readings = message.Readings.Select(r => new TelemetryReading
        {
            HardwareId = message.HardwareId,
            Timestamp = message.Timestamp,
            Key = r.Key,
            Value = r.Value,
            Unit = r.Unit
        }).ToList();

        await db.TelemetryReadings.AddRangeAsync(readings, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogDebug("Persisted {Count} readings for device {HardwareId}", readings.Count, message.HardwareId);

        try
        {
            await broadcaster.BroadcastAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "SignalR broadcast failed for device {HardwareId}", message.HardwareId);
        }

        try
        {
            await alertEvaluator.EvaluateAsync(deviceId, roomId, message.HardwareId, message, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Alert evaluation failed for device {HardwareId}", message.HardwareId);
        }
    }

    private async Task<(bool Found, bool IsActive, int DeviceId, int? RoomId)> GetDeviceStateAsync(string hardwareId, CancellationToken cancellationToken)
    {
        if (cache.TryGet(CacheKeys.DeviceActive(hardwareId), out bool active) && active)
        {
            var id = await db.Devices
                .AsNoTracking()
                .Where(d => d.HardwareId == hardwareId)
                .Select(d => new { d.Id, d.RoomId })
                .FirstOrDefaultAsync(cancellationToken);

            return (id != null, true, id?.Id ?? 0, id?.RoomId);
        }

        var device = await db.Devices
            .AsNoTracking()
            .Where(d => d.HardwareId == hardwareId)
            .Select(d => new { d.Id, d.RoomId, d.IsActive })
            .FirstOrDefaultAsync(cancellationToken);

        if (device is null)
            return (false, false, 0, null);

        if (device.IsActive)
            cache.Set(CacheKeys.DeviceActive(hardwareId), true, ActiveDeviceCacheTtl);

        return (true, device.IsActive, device.Id, device.RoomId);
    }

    private async Task RegisterDeviceAsync(string hardwareId, CancellationToken cancellationToken)
    {
        var device = new Device
        {
            HardwareId = hardwareId,
            Name = hardwareId,
            IsActive = false,
            TelemetryIntervalSeconds = 60
        };

        db.Devices.Add(device);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // A concurrent first message may have created the row already;
            // the next message re-evaluates the real device state.
        }
    }
}
