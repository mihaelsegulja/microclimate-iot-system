#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
BACKEND_DIR="$(cd -- "$SCRIPT_DIR/.." && pwd)"
PROJECT="$SCRIPT_DIR/MicroclimateIotSystem.Benchmark.csproj"
DLL="$SCRIPT_DIR/bin/Release/net8.0/MicroclimateIotSystem.Benchmark.dll"

API_URL="${BENCHMARK_API_URL:-https://localhost:7191}"
API_USERNAME="${BENCHMARK_API_USERNAME:-admin}"
API_PASSWORD="${BENCHMARK_API_PASSWORD:-admin}"
DEVICE_ID="${BENCHMARK_DEVICE_ID:-benchmark-device}"
DB_CONNECTION="${ConnectionStrings__Db:-Server=localhost,1433;Database=MicroclimateIotSystemBenchmarkDb2;User Id=sa;Password=SuperStrong!Passw0rd2024;TrustServerCertificate=True;}"
COOLDOWN_SECONDS="${BENCHMARK_COOLDOWN_SECONDS:-5}"
RUNS="${BENCHMARK_RUNS:-3}"
WARMUPS="${BENCHMARK_WARMUPS:-5}"
ITERATIONS="${BENCHMARK_ITERATIONS:-30}"
READINGS="${BENCHMARK_READINGS:-7}"
MAX_POINTS="${BENCHMARK_MAX_POINTS:-150}"
SPAN="${BENCHMARK_SPAN:-30d}"
OUTPUT_ROOT="${BENCHMARK_OUTPUT_ROOT:-$BACKEND_DIR/results/official-$(date -u +%Y%m%dT%H%M%SZ)}"
LOG_FILE="$OUTPUT_ROOT/benchmark.log"

SIZES=(10000 100000 500000 1000000)
PIPELINE_MESSAGES=(100 1000 5000)

export ConnectionStrings__Db="$DB_CONNECTION"

mkdir -p "$OUTPUT_ROOT"

log() {
  printf '[%s] %s\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)" "$*" | tee -a "$LOG_FILE"
}

run_benchmark() {
  local label="$1"
  shift
  log "START $label"
  dotnet "$DLL" "$@" 2>&1 | tee -a "$LOG_FILE"
  log "DONE  $label"
  sleep "$COOLDOWN_SECONDS"
}

for command in dotnet curl; do
  if ! command -v "$command" >/dev/null 2>&1; then
    log "Required command not found: $command"
    exit 1
  fi
done

if [[ "${BENCHMARK_ASSUME_YES:-0}" != "1" ]]; then
  cat <<EOF

Official benchmark run
  API:      $API_URL
  Database: MicroclimateIotSystemBenchmarkDb2
  Output:   $OUTPUT_ROOT

Before continuing, confirm that:
  - the benchmark database is fresh and migrations are applied
  - SQL Server, RabbitMQ and the Release backend are running
  - the backend uses MicroclimateIotSystemBenchmarkDb2
  - the ESP32 device and frontend are stopped
  - no other demanding workloads are running

EOF
  read -r -p "Continue? [y/N] " answer
  [[ "$answer" =~ ^[Yy]$ ]] || exit 0
fi

log "Official benchmark suite started"
log "Output directory: $OUTPUT_ROOT"
log "Runs=$RUNS Warmups=$WARMUPS Iterations=$ITERATIONS Readings=$READINGS Span=$SPAN"

login_status="$(curl --insecure --silent --show-error --output /dev/null --write-out '%{http_code}' \
  --header 'Content-Type: application/json' \
  --data "{\"username\":\"$API_USERNAME\",\"password\":\"$API_PASSWORD\"}" \
  "$API_URL/api/auth/login")"
if [[ "$login_status" != "200" ]]; then
  log "Backend login preflight failed with HTTP $login_status"
  exit 1
fi
log "Backend login preflight passed"

log "Building benchmark CLI in Release mode"
dotnet build "$PROJECT" --configuration Release 2>&1 | tee -a "$LOG_FILE"

for size in "${SIZES[@]}"; do
  size_dir="$(printf '%07d' "$size")"
  run_benchmark "seed-$size" \
    seed \
    --rows "$size" \
    --span "$SPAN" \
    --output "$OUTPUT_ROOT/$size_dir/seed"

  for run in $(seq 1 "$RUNS"); do
    run_benchmark "write-$size-run-$run" \
      write \
      --warmups "$WARMUPS" \
      --iterations "$ITERATIONS" \
      --readings "$READINGS" \
      --output "$OUTPUT_ROOT/$size_dir/write/run-$run"
  done

  for run in $(seq 1 "$RUNS"); do
    run_benchmark "read-$size-run-$run" \
      read \
      --device-id "$DEVICE_ID" \
      --username "$API_USERNAME" \
      --password "$API_PASSWORD" \
      --url "$API_URL" \
      --ranges 6h,24h,7d,30d \
      --max-points "$MAX_POINTS" \
      --warmups "$WARMUPS" \
      --iterations "$ITERATIONS" \
      --output "$OUTPUT_ROOT/$size_dir/read/run-$run"
  done
done

for messages in "${PIPELINE_MESSAGES[@]}"; do
  for run in $(seq 1 "$RUNS"); do
    run_benchmark "pipeline-$messages-run-$run" \
      pipeline \
      --messages "$messages" \
      --readings "$READINGS" \
      --timeout 5m \
      --output "$OUTPUT_ROOT/pipeline/$messages/run-$run"
  done
done

csv_count="$(find "$OUTPUT_ROOT" -type f -name '*.csv' | wc -l)"
log "Official benchmark suite completed successfully"
log "Generated CSV files: $csv_count"
log "Results: $OUTPUT_ROOT"
