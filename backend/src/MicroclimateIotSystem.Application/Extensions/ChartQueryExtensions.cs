using MicroclimateIotSystem.Application.DTOs;
using MicroclimateIotSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MicroclimateIotSystem.Application.Extensions;

public static class ChartQueryExtensions
{
    public static async Task<List<ChartSeriesDto>> ToChartSeriesAsync(
        this IQueryable<TelemetryReading> query,
        int maxPointsPerSeries = 2000,
        CancellationToken cancellationToken = default)
    {
        var rows = await query
            .AsNoTracking()
            .OrderBy(r => r.Timestamp)
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(r => r.Key)
            .Select(g => new ChartSeriesDto(
                g.Key,
                g.Select(r => r.Unit).FirstOrDefault(),
                g.Skip(Math.Max(0, g.Count() - maxPointsPerSeries))
                 .Select(r => new ChartPointDto(r.Timestamp, r.Value))
                 .ToList()))
            .OrderBy(s => s.Key)
            .ToList();
    }

    public static async Task<LatestTelemetryDto?> GetLatestGroupAsync(
        this IQueryable<TelemetryReading> query,
        CancellationToken cancellationToken = default)
    {
        var latestTimestamp = await query
            .AsNoTracking()
            .Select(r => (DateTime?)r.Timestamp)
            .OrderByDescending(t => t)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestTimestamp == null)
            return null;

        var ts = latestTimestamp.Value;
        var readings = await query
            .AsNoTracking()
            .Where(r => r.Timestamp == ts)
            .OrderBy(r => r.Key)
            .Select(r => new SensorReadingDto(r.Key, r.Value, r.Unit))
            .ToListAsync(cancellationToken);

        return new LatestTelemetryDto(latestTimestamp.Value, readings);
    }
}
