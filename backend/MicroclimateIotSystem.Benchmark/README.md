# Microclimate IoT benchmark CLI

Run from the `backend` directory in Release mode:

```bash
dotnet run -c Release --project MicroclimateIotSystem.Benchmark -- help
```

Typical sequence:

```bash
dotnet run -c Release --project MicroclimateIotSystem.Benchmark -- seed --rows 100000 --span 30d
dotnet run -c Release --project MicroclimateIotSystem.Benchmark -- write --iterations 30 --warmups 5 --readings 7
dotnet run -c Release --project MicroclimateIotSystem.Benchmark -- read --device-id benchmark-device --username admin --password PASSWORD
dotnet run -c Release --project MicroclimateIotSystem.Benchmark -- pipeline --messages 1000 --readings 7
```

`seed` fills the selected device up to the requested number of telemetry rows; it does not delete existing data. Every newly added layer is distributed across the requested `--span` (30 days by default), so incremental targets such as 10,000, 100,000, 500,000 and 1,000,000 rows all contain data across the complete test period. `write` measures the real `SensorDataProcessor` without RabbitMQ or connected SignalR clients. `read` measures complete authenticated HTTP responses. `pipeline` requires the backend consumer to be running and measures a RabbitMQ-to-database batch.

For `read`, `--device-id` accepts either the numeric database ID or the device hardware ID. Resolving a hardware ID uses the connection selected by `--connection` or `ConnectionStrings__Db`.

Each command writes CSV results and a JSON run manifest to `results` by default. Use `--output PATH` to change the directory and `--connection CONNECTION_STRING` (or `ConnectionStrings__Db`) to select the database.

## Complete official run

With the Release backend, SQL Server and RabbitMQ already running, execute from the `backend` directory:

```bash
./MicroclimateIotSystem.Benchmark/run-official-benchmarks.sh
```

The script incrementally seeds 10,000, 100,000, 500,000 and 1,000,000 rows. At each size it runs the write and read benchmarks three times, then runs the RabbitMQ pipeline with 100, 1,000 and 5,000 messages three times each. It defaults to `MicroclimateIotSystemBenchmarkDb2`, `https://localhost:7191` and `admin/admin`.

Defaults can be overridden without editing the script:

```bash
BENCHMARK_API_PASSWORD='password' \
BENCHMARK_COOLDOWN_SECONDS=10 \
BENCHMARK_OUTPUT_ROOT="$PWD/results/official-custom" \
./MicroclimateIotSystem.Benchmark/run-official-benchmarks.sh
```

Set `BENCHMARK_ASSUME_YES=1` only for unattended execution after independently confirming the preflight checklist.
