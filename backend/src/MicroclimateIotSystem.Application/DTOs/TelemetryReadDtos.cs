namespace MicroclimateIotSystem.Application.DTOs;

public record ChartPointDto(
    DateTime Timestamp,
    double Value
);

public record ChartSeriesDto(
    string Key,
    string? Unit,
    List<ChartPointDto> Points
);

public record LatestTelemetryDto(
    DateTime Timestamp,
    List<SensorReadingDto> Readings
);

public record AggregatedChartPointDto(
    DateTime Timestamp,
    double Average,
    double Min,
    double Max
);

public record AggregatedSeriesDto(
    string Key,
    string? Unit,
    List<AggregatedChartPointDto> Points
);
