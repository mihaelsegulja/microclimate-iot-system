using System.Globalization;

namespace MicroclimateIotSystem.Benchmark;

internal sealed class CliArguments
{
    private readonly Dictionary<string, string> _values = new(StringComparer.OrdinalIgnoreCase);
    public string? Command { get; }

    public CliArguments(string[] args)
    {
        Command = args.FirstOrDefault()?.ToLowerInvariant();
        for (var i = 1; i < args.Length; i++)
        {
            if (!args[i].StartsWith("--", StringComparison.Ordinal))
                throw new ArgumentException($"Unexpected argument '{args[i]}'. Options must start with --.");
            var key = args[i][2..];
            _values[key] = i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal)
                ? args[++i]
                : "true";
        }
    }

    public string Get(string key, string defaultValue) => _values.GetValueOrDefault(key, defaultValue);
    public string Require(string key) => _values.TryGetValue(key, out var value)
        ? value
        : throw new ArgumentException($"Missing required option --{key}.");
    public int GetInt(string key, int defaultValue) =>
        int.Parse(Get(key, defaultValue.ToString(CultureInfo.InvariantCulture)), CultureInfo.InvariantCulture);
    public TimeSpan GetDuration(string key, TimeSpan defaultValue) => ParseDuration(Get(key, FormatDuration(defaultValue)));
    public IReadOnlyList<TimeSpan> GetDurations(string key, string defaultValue) =>
        Get(key, defaultValue).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ParseDuration).ToArray();

    public static string FormatDuration(TimeSpan value) => value.TotalDays >= 1
        ? $"{value.TotalDays:0.##}d"
        : value.TotalHours >= 1
            ? $"{value.TotalHours:0.##}h"
            : value.TotalMinutes >= 1
                ? $"{value.TotalMinutes:0.##}m"
                : $"{value.TotalSeconds:0.##}s";

    private static TimeSpan ParseDuration(string value)
    {
        if (value.Length < 2 || !double.TryParse(value[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var amount))
            throw new ArgumentException($"Invalid duration '{value}'. Use values such as 30s, 6h or 7d.");
        return char.ToLowerInvariant(value[^1]) switch
        {
            's' => TimeSpan.FromSeconds(amount),
            'm' => TimeSpan.FromMinutes(amount),
            'h' => TimeSpan.FromHours(amount),
            'd' => TimeSpan.FromDays(amount),
            _ => throw new ArgumentException($"Invalid duration unit in '{value}'. Use s, m, h or d.")
        };
    }
}
