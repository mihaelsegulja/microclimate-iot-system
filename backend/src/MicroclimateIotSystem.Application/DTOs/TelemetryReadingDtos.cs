namespace MicroclimateIotSystem.Application.DTOs;

public record TelemetryReadingDto(
    string HardwareId,
    DateTime Timestamp,
    List<SensorReadingDto> Readings
);

public record SensorReadingDto(
    string Key,
    double Value,
    string? Unit
);