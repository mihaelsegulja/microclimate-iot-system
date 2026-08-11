namespace MicroclimateIotSystem.Application.DTOs;

public record DeviceAggregatedSeriesDto(
    int DeviceId,
    string Name,
    string HardwareId,
    List<AggregatedChartPointDto> Points
);

public record DashboardSeriesDto(
    string Key,
    string? Unit,
    List<DeviceAggregatedSeriesDto> Devices
);

public record DashboardTelemetryDto(
    int RoomId,
    string RoomName,
    List<DashboardSeriesDto> Series
);