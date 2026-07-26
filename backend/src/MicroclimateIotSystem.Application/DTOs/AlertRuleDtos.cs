using MicroclimateIotSystem.Domain.Enums;

namespace MicroclimateIotSystem.Application.DTOs;

public record AlertRuleResponseDto(
    int Id,
    string Name,
    string TelemetryKey,
    AlertOperator Operator,
    double ThresholdValue,
    bool IsActive,
    int? RoomId,
    string? RoomName,
    int? DeviceId,
    string? DeviceName
);

public record CreateAlertRuleRequestDto(
    string Name,
    string TelemetryKey,
    AlertOperator Operator,
    double ThresholdValue,
    int? RoomId,
    int? DeviceId
);
