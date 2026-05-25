using MicroclimateIotSystem.Domain.Abstractions;
using MicroclimateIotSystem.Domain.Enums;

namespace MicroclimateIotSystem.Domain.Entities;

public class User : BaseEntity
{
    public string Username { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string PasswordSalt { get; set; } = null!;
    public Roles Role { get; set; } = Roles.User;
    
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
