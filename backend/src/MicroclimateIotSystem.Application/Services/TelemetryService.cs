using MicroclimateIotSystem.Application.DTOs;
using MicroclimateIotSystem.Application.Extensions;
using MicroclimateIotSystem.Application.Interfaces;
using MicroclimateIotSystem.Application.Interfaces.Services;
using MicroclimateIotSystem.Application.Models;
using MicroclimateIotSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MicroclimateIotSystem.Application.Services;

public class TelemetryService(IAppDbContext db) : ITelemetryService
{
    public async Task<StandardResponse<List<ChartSeriesDto>>> GetChartAsync(
        int deviceId, DateTime? from, DateTime? to, IReadOnlyList<string>? keys,
        CancellationToken cancellationToken = default)
    {
        var hardwareId = await ResolveHardwareIdAsync(deviceId, cancellationToken);
        if (hardwareId == null)
            return StandardResponse<List<ChartSeriesDto>>.NotFound($"Device with id {deviceId} not found.");

        var query = BuildQuery(hardwareId, from, to, keys);

        var series = await query.ToChartSeriesAsync(cancellationToken: cancellationToken);

        return StandardResponse<List<ChartSeriesDto>>.SuccessOk(series);
    }

    public async Task<StandardResponse<LatestTelemetryDto>> GetLatestAsync(
        int deviceId, CancellationToken cancellationToken = default)
    {
        var hardwareId = await ResolveHardwareIdAsync(deviceId, cancellationToken);
        if (hardwareId == null)
            return StandardResponse<LatestTelemetryDto>.NotFound($"Device with id {deviceId} not found.");

        var latest = await db.TelemetryReadings
            .Where(r => r.HardwareId == hardwareId)
            .GetLatestGroupAsync(cancellationToken);

        if (latest == null)
            return StandardResponse<LatestTelemetryDto>.NotFound($"No telemetry found for device with id {deviceId}.");

        return StandardResponse<LatestTelemetryDto>.SuccessOk(latest);
    }

    private async Task<string?> ResolveHardwareIdAsync(int deviceId, CancellationToken cancellationToken = default)
    {
        return await db.Devices
            .AsNoTracking()
            .Where(d => d.Id == deviceId)
            .Select(d => (string?)d.HardwareId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private IQueryable<TelemetryReading> BuildQuery(
        string hardwareId, DateTime? from, DateTime? to, IReadOnlyList<string>? keys)
    {
        var query = db.TelemetryReadings
            .AsNoTracking()
            .Where(r => r.HardwareId == hardwareId);

        if (from.HasValue)
            query = query.Where(r => r.Timestamp >= from.Value);

        if (to.HasValue)
            query = query.Where(r => r.Timestamp <= to.Value);

        if (keys is { Count: > 0 })
            query = query.Where(r => keys.Contains(r.Key));

        return query;
    }
}
