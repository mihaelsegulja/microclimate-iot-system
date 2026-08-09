using MicroclimateIotSystem.Domain.Enums;

namespace MicroclimateIotSystem.Application.DTOs;

public record AlertEventDto(
    int Id,
    int AlertRuleId,
    string RuleName,
    int DeviceId,
    string HardwareId,
    string TelemetryKey,
    string? Unit,
    double Value,
    double ThresholdValue,
    AlertRuleOperator Operator,
    AlertStatus Status,
    DateTime Timestamp
);

public record AlertResponseDto(
    int Id,
    int AlertRuleId,
    string RuleName,
    int DeviceId,
    string DeviceName,
    string HardwareId,
    string TelemetryKey,
    string? Unit,
    double Value,
    double ThresholdValue,
    AlertRuleOperator Operator,
    AlertStatus Status,
    DateTime TriggeredAt,
    DateTime? ClearedAt
);