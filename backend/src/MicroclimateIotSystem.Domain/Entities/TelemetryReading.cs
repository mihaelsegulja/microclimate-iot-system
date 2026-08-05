namespace MicroclimateIotSystem.Domain.Entities;

public class TelemetryReading
{
    public long Id { get; set; }
    public string HardwareId { get; set; }
    public DateTime Timestamp { get; set; }
    public string Key { get; set; }
    public double Value { get; set; }
    public string? Unit { get; set; }
}