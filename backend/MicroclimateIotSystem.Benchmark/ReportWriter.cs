using System.Globalization;
using System.Text;
using System.Text.Json;

namespace MicroclimateIotSystem.Benchmark;

internal sealed class ReportWriter
{
    private readonly string _directory;
    public string RunId { get; } = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ", CultureInfo.InvariantCulture);

    public ReportWriter(string root, string command, IReadOnlyDictionary<string, object?> parameters)
    {
        _directory = Path.GetFullPath(root);
        Directory.CreateDirectory(_directory);
        var metadata = new
        {
            runId = RunId,
            command,
            startedAtUtc = DateTime.UtcNow,
            parameters,
            environment = new
            {
                machine = Environment.MachineName,
                operatingSystem = Environment.OSVersion.ToString(),
                framework = Environment.Version.ToString(),
                processorCount = Environment.ProcessorCount,
                workingSetBytes = Environment.WorkingSet
            }
        };
        File.WriteAllText(Path.Combine(_directory, $"run-{RunId}.json"),
            JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true }));
    }

    public string WriteCsv(string kind, IReadOnlyList<string> columns, IEnumerable<IReadOnlyDictionary<string, object?>> rows)
    {
        var path = Path.Combine(_directory, $"{kind}-{RunId}.csv");
        using var writer = new StreamWriter(path, false, new UTF8Encoding(false));
        writer.WriteLine(string.Join(',', columns.Select(Escape)));
        foreach (var row in rows)
            writer.WriteLine(string.Join(',', columns.Select(column => Escape(Format(row.GetValueOrDefault(column))))));
        return path;
    }

    public static Dictionary<string, object?> Summary(IEnumerable<double> samples)
    {
        var values = samples.OrderBy(x => x).ToArray();
        if (values.Length == 0) throw new ArgumentException("At least one sample is required.");
        return new Dictionary<string, object?>
        {
            ["samples"] = values.Length,
            ["mean_ms"] = values.Average(),
            ["median_ms"] = Percentile(values, 0.5),
            ["p95_ms"] = Percentile(values, 0.95),
            ["min_ms"] = values[0],
            ["max_ms"] = values[^1]
        };
    }

    private static double Percentile(double[] sorted, double percentile)
    {
        var position = (sorted.Length - 1) * percentile;
        var lower = (int)Math.Floor(position);
        var upper = (int)Math.Ceiling(position);
        return lower == upper ? sorted[lower] : sorted[lower] + (sorted[upper] - sorted[lower]) * (position - lower);
    }

    private static string Format(object? value) => value switch
    {
        null => "",
        DateTime date => date.ToString("O", CultureInfo.InvariantCulture),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? ""
    };

    private static string Escape(string value) =>
        value.IndexOfAny([',', '"', '\n', '\r']) >= 0 ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
}
