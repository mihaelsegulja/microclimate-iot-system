using MicroclimateIotSystem.Domain.Abstractions;
using MicroclimateIotSystem.Domain.Enums;

namespace MicroclimateIotSystem.Domain.Entities;

public class Alert : BaseEntity
{
    public int AlertRuleId { get; set; }
    public int DeviceId { get; set; }
    public string HardwareId { get; set; }
    public string TelemetryKey { get; set; }
    public string? Unit { get; set; }
    public double Value { get; set; }
    public double ThresholdValue { get; set; }
    public AlertRuleOperator Operator { get; set; }
    public AlertStatus Status { get; set; }
    public DateTime TriggeredAt { get; set; }
    public DateTime? ClearedAt { get; set; }

    public AlertRule AlertRule { get; set; }
    public Device Device { get; set; }
}