using MicroclimateIotSystem.Application.DTOs;
using MicroclimateIotSystem.Application.Extensions;
using MicroclimateIotSystem.Application.Interfaces;
using MicroclimateIotSystem.Application.Interfaces.Services;
using MicroclimateIotSystem.Application.Models;
using MicroclimateIotSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MicroclimateIotSystem.Application.Services;

public class DashboardService(IAppDbContext db) : IDashboardService
{
    public async Task<StandardResponse<DashboardTelemetryDto>> GetRoomTelemetryAsync(
        int roomId, DateTime? from, DateTime? to, int maxPoints,
        CancellationToken cancellationToken = default)
    {
        var room = await db.Rooms
            .AsNoTracking()
            .Where(r => r.Id == roomId)
            .Select(r => new { r.Id, r.Name })
            .FirstOrDefaultAsync(cancellationToken);

        if (room == null)
            return StandardResponse<DashboardTelemetryDto>.NotFound($"Room with id {roomId} not found.");

        var devices = await db.Devices
            .AsNoTracking()
            .Where(d => d.RoomId == roomId && d.IsActive)
            .OrderBy(d => d.Name)
            .Select(d => new { d.Id, d.Name, d.HardwareId })
            .ToListAsync(cancellationToken);

        if (devices.Count == 0)
            return StandardResponse<DashboardTelemetryDto>.SuccessOk(
                new DashboardTelemetryDto(room.Id, room.Name, []));

        var hardwareIds = devices.Select(d => d.HardwareId).ToList();

        var fromUtc = from?.ToUniversalTime() ?? DateTime.UtcNow.AddDays(-1);
        var toUtc = to?.ToUniversalTime() ?? DateTime.UtcNow;
        if (toUtc <= fromUtc)
            toUtc = fromUtc.AddHours(1);

        var bucketSeconds = ChartQueryExtensions.ComputeBucketSeconds(toUtc - fromUtc, maxPoints);
        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var query = db.TelemetryReadings
            .AsNoTracking()
            .Where(r => hardwareIds.Contains(r.HardwareId)
                        && r.Timestamp >= fromUtc
                        && r.Timestamp <= toUtc);

        var buckets = await query
            .GroupBy(r => new
            {
                r.HardwareId,
                r.Key,
                Bucket = EF.Functions.DateDiffSecond(epoch, r.Timestamp) / bucketSeconds
            })
            .Select(g => new
            {
                g.Key.HardwareId,
                g.Key.Key,
                g.Key.Bucket,
                Timestamp = g.Min(r => r.Timestamp),
                Average = g.Average(r => r.Value),
                Min = g.Min(r => r.Value),
                Max = g.Max(r => r.Value)
            })
            .OrderBy(x => x.Key)
            .ThenBy(x => x.HardwareId)
            .ThenBy(x => x.Bucket)
            .ToListAsync(cancellationToken);

        var unitByKey = await query
            .GroupBy(r => r.Key)
            .Select(g => new { g.Key, Unit = g.Select(r => r.Unit).FirstOrDefault() })
            .ToListAsync(cancellationToken);
        var unitLookup = unitByKey.ToDictionary(u => u.Key, u => u.Unit, StringComparer.Ordinal);

        var deviceLookup = devices.ToDictionary(d => d.HardwareId, d => (d.Id, d.Name), StringComparer.Ordinal);

        var series = buckets
            .GroupBy(b => b.Key)
            .Select(g => new DashboardSeriesDto(
                g.Key,
                unitLookup.GetValueOrDefault(g.Key),
                g.GroupBy(b => b.HardwareId)
                    .Select(dg =>
                    {
                        var (deviceId, name) = deviceLookup.GetValueOrDefault(dg.Key);
                        return new DeviceAggregatedSeriesDto(
                            deviceId,
                            name,
                            dg.Key,
                            dg.Select(b => new AggregatedChartPointDto(b.Timestamp, b.Average, b.Min, b.Max)).ToList());
                    })
                    .ToList()))
            .OrderBy(s => s.Key)
            .ToList();

        return StandardResponse<DashboardTelemetryDto>.SuccessOk(
            new DashboardTelemetryDto(room.Id, room.Name, series));
    }
}