using MicroclimateIotSystem.Domain.Abstractions;

namespace MicroclimateIotSystem.Domain.Entities;

public class Room : BaseEntity
{
    public string Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    
    public ICollection<Device> Devices { get; set; } = new List<Device>();
}