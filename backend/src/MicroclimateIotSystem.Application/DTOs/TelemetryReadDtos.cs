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
