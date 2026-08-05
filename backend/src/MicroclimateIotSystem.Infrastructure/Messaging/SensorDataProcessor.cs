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
    ILogger<SensorDataProcessor> logger)
    : ISensorDataProcessor
{
    private static readonly TimeSpan ActiveDeviceCacheTtl = TimeSpan.FromMinutes(10);

    public async Task ProcessAsync(TelemetryReadingDto message, CancellationToken cancellationToken = default)
    {
        if (!await IsDeviceActiveAsync(message.HardwareId, cancellationToken))
        {
            logger.LogWarning("Device {HardwareId} not found or inactive", message.HardwareId);
            throw new InvalidOperationException($"Device '{message.HardwareId}' not found or inactive.");
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
    }

    private async Task<bool> IsDeviceActiveAsync(string hardwareId, CancellationToken cancellationToken)
    {
        if (cache.TryGet(CacheKeys.DeviceActive(hardwareId), out bool active) && active)
            return true;

        var isActive = await db.Devices
            .AsNoTracking()
            .Where(d => d.HardwareId == hardwareId)
            .Select(d => (bool?)d.IsActive)
            .FirstOrDefaultAsync(cancellationToken) == true;

        if (isActive)
            cache.Set(CacheKeys.DeviceActive(hardwareId), true, ActiveDeviceCacheTtl);

        return isActive;
    }
}
