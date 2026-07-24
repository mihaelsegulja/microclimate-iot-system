using MicroclimateIotSystem.Domain.Abstractions;

namespace MicroclimateIotSystem.Domain.Entities;

public class Device : BaseEntity
{
    public string HardwareId { get; set; }
    public string Name { get; set; }
    public bool IsActive { get; set; }
    public int TelemetryIntervalSeconds { get; set; }
    public int? RoomId { get; set; }
    
    public Room? Room { get; set; }
}