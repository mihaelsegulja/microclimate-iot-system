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

    public static int ComputeBucketSeconds(TimeSpan range, int maxPoints = 150)
    {
        if (range <= TimeSpan.Zero || maxPoints <= 0)
            return 1;

        var totalSeconds = Math.Max(1, Math.Ceiling(range.TotalSeconds));
        var rawBucket = (long)Math.Ceiling(totalSeconds / maxPoints);

        // "nice" bucket sizes in seconds (up to 1 day)
        int[] niceBuckets =
        {
            1, 5, 10, 15, 30, 60, 120, 300, 600, 900, 1800, 3600,
            7200, 14400, 21600, 43200, 86400
        };

        foreach (var bucket in niceBuckets)
        {
            if (bucket >= rawBucket)
                return bucket;
        }

        // ranges longer than ~5.6 years: scale by whole days
        return (int)Math.Min(int.MaxValue, ((rawBucket + 86399) / 86400) * 86400);
    }

    public static async Task<List<AggregatedSeriesDto>> ToAggregatedSeriesAsync(
        this IQueryable<TelemetryReading> query,
        int bucketSeconds,
        CancellationToken cancellationToken = default)
    {
        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var buckets = await query
            .AsNoTracking()
            .GroupBy(r => new { r.Key, Bucket = EF.Functions.DateDiffSecond(epoch, r.Timestamp) / bucketSeconds })
            .Select(g => new
            {
                g.Key.Key,
                g.Key.Bucket,
                Timestamp = g.Min(r => r.Timestamp),
                Average = g.Average(r => r.Value),
                Min = g.Min(r => r.Value),
                Max = g.Max(r => r.Value)
            })
            .OrderBy(x => x.Key)
            .ThenBy(x => x.Bucket)
            .ToListAsync(cancellationToken);

        var unitByKey = await query
            .AsNoTracking()
            .GroupBy(r => r.Key)
            .Select(g => new { g.Key, Unit = g.Select(r => r.Unit).FirstOrDefault() })
            .ToListAsync(cancellationToken);
        var unitLookup = unitByKey.ToDictionary(u => u.Key, u => u.Unit);

        return buckets
            .GroupBy(b => b.Key)
            .Select(g => new AggregatedSeriesDto(
                g.Key,
                unitLookup.GetValueOrDefault(g.Key),
                g.Select(b => new AggregatedChartPointDto(b.Timestamp, b.Average, b.Min, b.Max)).ToList()
            ))
            .ToList();
    }
}
