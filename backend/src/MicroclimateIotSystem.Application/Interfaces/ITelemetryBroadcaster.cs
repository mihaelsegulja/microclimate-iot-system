using MicroclimateIotSystem.Application.DTOs;

namespace MicroclimateIotSystem.Application.Interfaces;

public interface ITelemetryBroadcaster
{
    Task BroadcastAsync(TelemetryReadingDto message, CancellationToken cancellationToken = default);
}
