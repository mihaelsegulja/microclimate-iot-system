using MicroclimateIotSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MicroclimateIotSystem.Application.Interfaces;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Device> Devices { get; }
    DbSet<TelemetryReading> TelemetryReadings { get; }
    DbSet<Room> Rooms { get; }
    DbSet<AlertRule> AlertRules { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}