using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using MicroclimateIotSystem.Application.DTOs;
using MicroclimateIotSystem.Application.Interfaces;
using MicroclimateIotSystem.Application.Services;
using MicroclimateIotSystem.Domain.Entities;
using MicroclimateIotSystem.Infrastructure;
using MicroclimateIotSystem.Infrastructure.Messaging;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Client;

namespace MicroclimateIotSystem.Benchmark;

internal static class BenchmarkApp
{
    private const string DefaultConnection = "Server=localhost,1433;Database=MicroclimateIotSystemBenchmarkDb2;User Id=sa;Password=SuperStrong!Passw0rd2024;TrustServerCertificate=True;";
    private static readonly SensorReadingDto[] Sensors =
    [
        new("temperature", 22.5, "degreeCelsius"),
        new("humidity", 45.0, "%"),
        new("gas_resistance", 111000, "ohm"),
        new("pressure", 1013.2, "hPa"),
        new("co2", 650, "ppm"),
        new("tvoc", 75, "ppb"),
        new("aqi", 2, null)
    ];

    public static async Task<int> RunAsync(string[] args)
    {
        try
        {
            var cli = new CliArguments(args);
            switch (cli.Command)
            {
                case "seed": await SeedAsync(cli); break;
                case "write": await WriteAsync(cli); break;
                case "read": await ReadAsync(cli); break;
                case "pipeline": await PipelineAsync(cli); break;
                default: PrintHelp(); return cli.Command is null or "help" or "--help" ? 0 : 1;
            }
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static async Task SeedAsync(CliArguments cli)
    {
        var connectionString = ConnectionString(cli);
        var hardwareId = cli.Get("hardware-id", "benchmark-device");
        var targetRows = cli.GetInt("rows", 100_000);
        var batchSize = cli.GetInt("batch-size", 10_000);
        var span = cli.GetDuration("span", TimeSpan.FromDays(30));
        ValidateHardwareId(hardwareId);
        ValidatePositive(targetRows, "rows");
        ValidatePositive(batchSize, "batch-size");
        if (span <= TimeSpan.Zero) throw new ArgumentOutOfRangeException("span", "Value must be greater than zero.");

        await using var db = CreateDb(connectionString);
        await EnsureDeviceAsync(db, hardwareId);
        var existing = await db.TelemetryReadings.LongCountAsync(r => r.HardwareId == hardwareId);
        var missing = Math.Max(0, targetRows - existing);
        var report = new ReportWriter(cli.Get("output", "results"), "seed", new Dictionary<string, object?>
        {
            ["hardwareId"] = hardwareId, ["targetRows"] = targetRows, ["batchSize"] = batchSize,
            ["span"] = CliArguments.FormatDuration(span)
        });

        var stopwatch = Stopwatch.StartNew();
        if (missing > 0)
            await BulkInsertAsync(connectionString, hardwareId, existing, missing, batchSize, targetRows, span);
        stopwatch.Stop();
        var finalRows = await db.TelemetryReadings.LongCountAsync(r => r.HardwareId == hardwareId);
        var row = new Dictionary<string, object?>
        {
            ["run_id"] = report.RunId, ["hardware_id"] = hardwareId, ["rows_before"] = existing,
            ["rows_inserted"] = missing, ["rows_after"] = finalRows, ["duration_ms"] = stopwatch.Elapsed.TotalMilliseconds
        };
        var path = report.WriteCsv("seed", row.Keys.ToArray(), [row]);
        Console.WriteLine($"Seed complete: {finalRows:N0} rows for {hardwareId}. Report: {path}");
    }

    private static async Task WriteAsync(CliArguments cli)
    {
        var connectionString = ConnectionString(cli);
        var iterations = cli.GetInt("iterations", 30);
        var warmups = cli.GetInt("warmups", 5);
        var readingCount = cli.GetInt("readings", Sensors.Length);
        var hardwareId = cli.Get("hardware-id", $"bench-write-{DateTime.UtcNow:MMddHHmmss}");
        ValidateHardwareId(hardwareId);
        ValidatePositive(iterations, "iterations");
        ValidatePositive(readingCount, "readings");
        if (warmups < 0) throw new ArgumentOutOfRangeException("warmups", "Value cannot be negative.");

        await using (var setupDb = CreateDb(connectionString))
            await EnsureDeviceAsync(setupDb, hardwareId);

        var cache = new BenchmarkCache();
        var report = new ReportWriter(cli.Get("output", "results"), "write", new Dictionary<string, object?>
        {
            ["hardwareId"] = hardwareId, ["iterations"] = iterations, ["warmups"] = warmups,
            ["readingsPerMessage"] = readingCount
        });
        var raw = new List<IReadOnlyDictionary<string, object?>>();

        for (var i = -warmups; i < iterations; i++)
        {
            await using var db = CreateDb(connectionString);
            var evaluator = new AlertEvaluator(db, cache, new NullAlertBroadcaster(), NullLogger<AlertEvaluator>.Instance);
            var processor = new SensorDataProcessor(db, cache, new NullTelemetryBroadcaster(), evaluator,
                NullLogger<SensorDataProcessor>.Instance);
            var databaseRows = await db.TelemetryReadings.LongCountAsync();
            var stopwatch = Stopwatch.StartNew();
            await processor.ProcessAsync(CreateMessage(hardwareId, readingCount, DateTime.UtcNow.AddSeconds(i)));
            stopwatch.Stop();

            raw.Add(new Dictionary<string, object?>
            {
                ["run_id"] = report.RunId, ["scenario"] = "sensor_data_processor", ["database_rows"] = databaseRows,
                ["iteration"] = i < 0 ? i + warmups + 1 : i + 1, ["warmup"] = i < 0,
                ["readings_per_message"] = readingCount, ["duration_ms"] = stopwatch.Elapsed.TotalMilliseconds,
                ["success"] = true, ["error"] = null
            });
            Console.Write($"\rWrite {i + warmups + 1}/{iterations + warmups}");
        }

        Console.WriteLine();
        var measured = raw.Where(r => Equals(r["warmup"], false)).ToArray();
        var summary = ReportWriter.Summary(measured.Select(r => Convert.ToDouble(r["duration_ms"], CultureInfo.InvariantCulture)));
        summary["run_id"] = report.RunId;
        summary["scenario"] = "sensor_data_processor";
        summary["readings_per_message"] = readingCount;
        var rawPath = report.WriteCsv("raw", raw[0].Keys.ToArray(), raw);
        var summaryPath = report.WriteCsv("summary", summary.Keys.ToArray(), [summary]);
        Console.WriteLine($"Write benchmark complete. Raw: {rawPath}\nSummary: {summaryPath}");
    }

    private static async Task ReadAsync(CliArguments cli)
    {
        var baseUrl = cli.Get("url", "http://localhost:5000").TrimEnd('/');
        var deviceArgument = cli.Require("device-id");
        var deviceId = await ResolveDeviceIdAsync(deviceArgument, ConnectionString(cli));
        var iterations = cli.GetInt("iterations", 30);
        var warmups = cli.GetInt("warmups", 5);
        var maxPoints = cli.GetInt("max-points", 150);
        var ranges = cli.GetDurations("ranges", "6h,24h,7d,30d");
        ValidatePositive(iterations, "iterations");
        if (warmups < 0) throw new ArgumentOutOfRangeException("warmups", "Value cannot be negative.");

        using var http = new HttpClient { BaseAddress = new Uri(baseUrl) };
        var token = cli.Get("token", "");
        if (string.IsNullOrWhiteSpace(token))
            token = await LoginAsync(http, cli.Require("username"), cli.Require("password"));
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var report = new ReportWriter(cli.Get("output", "results"), "read", new Dictionary<string, object?>
        {
            ["url"] = baseUrl, ["device"] = deviceArgument, ["resolvedDeviceId"] = deviceId, ["iterations"] = iterations,
            ["warmups"] = warmups, ["ranges"] = string.Join(',', ranges.Select(CliArguments.FormatDuration)), ["maxPoints"] = maxPoints
        });
        var raw = new List<IReadOnlyDictionary<string, object?>>();

        foreach (var scenario in new[] { "latest", "raw", "aggregate" })
        foreach (var range in scenario == "latest" ? [TimeSpan.Zero] : ranges)
        for (var i = -warmups; i < iterations; i++)
        {
            var path = BuildReadPath(deviceId, scenario, range, maxPoints);
            var stopwatch = Stopwatch.StartNew();
            using var response = await http.GetAsync(path, HttpCompletionOption.ResponseHeadersRead);
            var body = await response.Content.ReadAsByteArrayAsync();
            stopwatch.Stop();
            raw.Add(new Dictionary<string, object?>
            {
                ["run_id"] = report.RunId, ["scenario"] = scenario,
                ["range"] = range == TimeSpan.Zero ? "" : CliArguments.FormatDuration(range),
                ["iteration"] = i < 0 ? i + warmups + 1 : i + 1, ["warmup"] = i < 0,
                ["status_code"] = (int)response.StatusCode, ["duration_ms"] = stopwatch.Elapsed.TotalMilliseconds,
                ["response_bytes"] = body.Length, ["returned_points"] = response.IsSuccessStatusCode ? CountPoints(body) : 0,
                ["success"] = response.IsSuccessStatusCode,
                ["error"] = response.IsSuccessStatusCode ? null : Encoding.UTF8.GetString(body)
            });
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"HTTP {(int)response.StatusCode} for {path}: {Encoding.UTF8.GetString(body)}");
        }

        var summaries = raw.Where(r => Equals(r["warmup"], false))
            .GroupBy(r => new { Scenario = r["scenario"]?.ToString(), Range = r["range"]?.ToString() })
            .Select(group =>
            {
                var summary = ReportWriter.Summary(group.Select(r => Convert.ToDouble(r["duration_ms"], CultureInfo.InvariantCulture)));
                summary["run_id"] = report.RunId;
                summary["scenario"] = group.Key.Scenario;
                summary["range"] = group.Key.Range;
                summary["mean_response_bytes"] = group.Average(r => Convert.ToDouble(r["response_bytes"], CultureInfo.InvariantCulture));
                summary["mean_returned_points"] = group.Average(r => Convert.ToDouble(r["returned_points"], CultureInfo.InvariantCulture));
                return summary;
            }).ToArray();
        var rawPath = report.WriteCsv("raw", raw[0].Keys.ToArray(), raw);
        var summaryPath = report.WriteCsv("summary", summaries[0].Keys.ToArray(), summaries);
        Console.WriteLine($"Read benchmark complete. Raw: {rawPath}\nSummary: {summaryPath}");
    }

    private static async Task PipelineAsync(CliArguments cli)
    {
        var connectionString = ConnectionString(cli);
        var messages = cli.GetInt("messages", 1_000);
        var readingCount = cli.GetInt("readings", Sensors.Length);
        var timeout = cli.GetDuration("timeout", TimeSpan.FromMinutes(5));
        var hardwareId = $"bench-p-{DateTime.UtcNow:MMddHHmmss}";
        ValidatePositive(messages, "messages");
        ValidatePositive(readingCount, "readings");

        await using (var db = CreateDb(connectionString))
            await EnsureDeviceAsync(db, hardwareId);

        var host = cli.Get("rabbit-host", "localhost");
        var port = cli.GetInt("rabbit-port", 5672);
        var exchange = cli.Get("exchange", "microclimate.events");
        var report = new ReportWriter(cli.Get("output", "results"), "pipeline", new Dictionary<string, object?>
        {
            ["hardwareId"] = hardwareId, ["messages"] = messages, ["readingsPerMessage"] = readingCount,
            ["rabbitHost"] = host, ["rabbitPort"] = port, ["exchange"] = exchange, ["timeout"] = timeout.ToString()
        });
        var factory = new ConnectionFactory
        {
            HostName = host, Port = port, UserName = cli.Get("rabbit-username", "iot_admin"),
            Password = cli.Get("rabbit-password", "iot_admin")
        };
        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        var properties = new BasicProperties { Persistent = true, ContentType = "application/json" };
        var expectedRows = (long)messages * readingCount;
        var started = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        for (var i = 0; i < messages; i++)
        {
            var body = JsonSerializer.SerializeToUtf8Bytes(CreateMessage(hardwareId, readingCount, started.AddMilliseconds(i)),
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            await channel.BasicPublishAsync(exchange, $"devices.{hardwareId}.telemetry", false, properties, body);
        }

        long insertedRows;
        do
        {
            await Task.Delay(50);
            await using var db = CreateDb(connectionString);
            insertedRows = await db.TelemetryReadings.LongCountAsync(r => r.HardwareId == hardwareId);
        } while (insertedRows < expectedRows && stopwatch.Elapsed < timeout);
        stopwatch.Stop();

        var success = insertedRows == expectedRows;
        var row = new Dictionary<string, object?>
        {
            ["run_id"] = report.RunId, ["scenario"] = "rabbitmq_to_database", ["hardware_id"] = hardwareId,
            ["message_count"] = messages, ["readings_per_message"] = readingCount, ["expected_rows"] = expectedRows,
            ["inserted_rows"] = insertedRows, ["duration_ms"] = stopwatch.Elapsed.TotalMilliseconds,
            ["messages_per_second"] = messages / stopwatch.Elapsed.TotalSeconds,
            ["rows_per_second"] = insertedRows / stopwatch.Elapsed.TotalSeconds, ["success"] = success,
            ["error"] = success ? null : "Timed out before all expected rows were stored."
        };
        var path = report.WriteCsv("pipeline", row.Keys.ToArray(), [row]);
        Console.WriteLine($"Pipeline benchmark {(success ? "complete" : "timed out")}. Report: {path}");
        if (!success) throw new TimeoutException($"Stored {insertedRows:N0} of {expectedRows:N0} expected rows in {timeout}.");
    }

    private static async Task BulkInsertAsync(
        string connectionString,
        string hardwareId,
        long existing,
        long missing,
        int batchSize,
        int targetRows,
        TimeSpan span)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        var newCycles = (long)Math.Ceiling(missing / (double)Sensors.Length);
        var lastTimestamp = DateTime.UtcNow;
        var firstTimestamp = lastTimestamp.Subtract(span);
        long inserted = 0;

        while (inserted < missing)
        {
            var count = (int)Math.Min(batchSize, missing - inserted);
            var table = CreateTelemetryTable();
            for (var j = 0; j < count; j++)
            {
                var rowNumber = existing + inserted + j;
                var sensor = Sensors[(int)(rowNumber % Sensors.Length)];
                var newRowNumber = inserted + j;
                var cycle = newRowNumber / Sensors.Length;
                var progress = newCycles <= 1 ? 1d : cycle / (double)(newCycles - 1);
                var timestamp = firstTimestamp.AddTicks((long)(span.Ticks * progress));
                table.Rows.Add(hardwareId, timestamp, sensor.Key,
                    sensor.Value + Math.Sin(cycle / 30d), sensor.Unit is null ? DBNull.Value : sensor.Unit);
            }
            using var bulk = new SqlBulkCopy(connection)
            {
                DestinationTableName = "TelemetryReadings", BatchSize = count, BulkCopyTimeout = 120
            };
            foreach (DataColumn column in table.Columns) bulk.ColumnMappings.Add(column.ColumnName, column.ColumnName);
            await bulk.WriteToServerAsync(table);
            inserted += count;
            Console.Write($"\rSeeded {existing + inserted:N0}/{targetRows:N0}");
        }
        Console.WriteLine();
    }

    private static DataTable CreateTelemetryTable()
    {
        var table = new DataTable();
        table.Columns.Add("HardwareId", typeof(string));
        table.Columns.Add("Timestamp", typeof(DateTime));
        table.Columns.Add("Key", typeof(string));
        table.Columns.Add("Value", typeof(double));
        table.Columns.Add("Unit", typeof(string));
        return table;
    }

    private static TelemetryReadingDto CreateMessage(string hardwareId, int readingCount, DateTime timestamp)
    {
        var readings = Enumerable.Range(0, readingCount).Select(i =>
        {
            var template = Sensors[i % Sensors.Length];
            return template with { Key = readingCount <= Sensors.Length ? template.Key : $"{template.Key}_{i}" };
        }).ToList();
        return new TelemetryReadingDto(hardwareId, timestamp, readings);
    }

    private static async Task EnsureDeviceAsync(AppDbContext db, string hardwareId)
    {
        var device = await db.Devices.SingleOrDefaultAsync(d => d.HardwareId == hardwareId);
        if (device is null)
            db.Devices.Add(new Device { HardwareId = hardwareId, Name = hardwareId, IsActive = true, TelemetryIntervalSeconds = 60 });
        else
            device.IsActive = true;
        await db.SaveChangesAsync();
    }

    private static async Task<int> ResolveDeviceIdAsync(string value, string connectionString)
    {
        if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var deviceId))
        {
            ValidatePositive(deviceId, "device-id");
            return deviceId;
        }

        await using var db = CreateDb(connectionString);
        var resolved = await db.Devices
            .AsNoTracking()
            .Where(d => d.HardwareId == value)
            .Select(d => (int?)d.Id)
            .SingleOrDefaultAsync();
        return resolved ?? throw new ArgumentException($"Device with hardware ID '{value}' was not found in the benchmark database.");
    }

    private static AppDbContext CreateDb(string connectionString) => new(
        new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connectionString).Options);

    private static string ConnectionString(CliArguments cli) => cli.Get("connection",
        Environment.GetEnvironmentVariable("ConnectionStrings__Db") ?? DefaultConnection);

    private static async Task<string> LoginAsync(HttpClient http, string username, string password)
    {
        using var response = await http.PostAsJsonAsync("/api/auth/login", new { username, password });
        var body = await response.Content.ReadAsByteArrayAsync();
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Login failed with HTTP {(int)response.StatusCode}: {Encoding.UTF8.GetString(body)}");
        using var json = JsonDocument.Parse(body);
        return json.RootElement.GetProperty("data").GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("Login response did not contain an access token.");
    }

    private static string BuildReadPath(int deviceId, string scenario, TimeSpan range, int maxPoints)
    {
        if (scenario == "latest") return $"/api/devices/{deviceId}/telemetry/latest";
        var now = DateTime.UtcNow;
        var from = Uri.EscapeDataString(now.Subtract(range).ToString("O", CultureInfo.InvariantCulture));
        var to = Uri.EscapeDataString(now.ToString("O", CultureInfo.InvariantCulture));
        var suffix = scenario == "aggregate" ? $"/aggregate?from={from}&to={to}&maxPoints={maxPoints}" : $"?from={from}&to={to}";
        return $"/api/devices/{deviceId}/telemetry{suffix}";
    }

    private static int CountPoints(byte[] body)
    {
        using var document = JsonDocument.Parse(body);
        var count = 0;
        Visit(document.RootElement);
        return count;

        void Visit(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object)
                foreach (var property in element.EnumerateObject())
                    if ((property.NameEquals("points") || property.NameEquals("readings")) && property.Value.ValueKind == JsonValueKind.Array)
                        count += property.Value.GetArrayLength();
                    else
                        Visit(property.Value);
            else if (element.ValueKind == JsonValueKind.Array)
                foreach (var item in element.EnumerateArray()) Visit(item);
        }
    }

    private static void ValidateHardwareId(string hardwareId)
    {
        if (string.IsNullOrWhiteSpace(hardwareId) || hardwareId.Length > 32)
            throw new ArgumentException("Hardware ID must contain 1 to 32 characters.");
    }

    private static void ValidatePositive(int value, string name)
    {
        if (value <= 0) throw new ArgumentOutOfRangeException(name, "Value must be greater than zero.");
    }

    private static void PrintHelp() => Console.WriteLine("""
        Microclimate IoT benchmark CLI

          seed     --rows 100000 [--span 30d] [--hardware-id benchmark-device] [--batch-size 10000]
          write    [--iterations 30] [--warmups 5] [--readings 7]
          read     --device-id 1|HARDWARE_ID (--token TOKEN | --username USER --password PASS)
                   [--url http://localhost:5000] [--ranges 6h,24h,7d,30d] [--iterations 30]
          pipeline [--messages 1000] [--readings 7] [--timeout 5m]

        Common:   --connection CONNECTION_STRING --output results
        RabbitMQ: --rabbit-host localhost --rabbit-port 5672 --rabbit-username iot_admin
                  --rabbit-password iot_admin --exchange microclimate.events
        """);

    private sealed class BenchmarkCache : ICacheService
    {
        private readonly Dictionary<string, object> _values = new();
        public bool TryGet<T>(string key, out T value)
        {
            if (_values.TryGetValue(key, out var stored) && stored is T typed) { value = typed; return true; }
            value = default!;
            return false;
        }
        public void Set<T>(string key, T value, TimeSpan ttl) => _values[key] = value!;
        public void Remove(string key) => _values.Remove(key);
    }

    private sealed class NullTelemetryBroadcaster : ITelemetryBroadcaster
    {
        public Task BroadcastAsync(TelemetryReadingDto message, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class NullAlertBroadcaster : IAlertBroadcaster
    {
        public Task BroadcastAsync(AlertEventDto alert, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
