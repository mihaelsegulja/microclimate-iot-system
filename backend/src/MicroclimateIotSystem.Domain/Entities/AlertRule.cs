using MicroclimateIotSystem.Domain.Abstractions;
using MicroclimateIotSystem.Domain.Enums;

namespace MicroclimateIotSystem.Domain.Entities;

public class AlertRule : BaseEntity
{
    public string Name { get; set; }
    public string TelemetryKey { get; set; }
    public AlertRuleOperator Operator { get; set; }
    public double ThresholdValue { get; set; }
    public bool IsActive { get; set; }
    public int? RoomId { get; set; }
    public int? DeviceId { get; set; }
    
    public Room? Room { get; set; }
    public Device? Device { get; set; }
}