namespace MicroclimateIotSystem.Application.DTOs;

public record DeviceResponseDto(
    int Id,
    string Name,
    string HardwareId,
    bool IsActive,
    int TelemetryIntervalSeconds,
    int? RoomId,
    string? RoomName
);

public record CreateDeviceRequestDto(
    string Name,
    string HardwareId,
    int? RoomId
);

public record UpdateDeviceRequestDto(
    int Id,
    string Name,
    string HardwareId,
    bool IsActive,
    int TelemetryIntervalSeconds,
    int? RoomId
);
