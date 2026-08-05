using MicroclimateIotSystem.Application.DTOs;

namespace MicroclimateIotSystem.Application.Interfaces;

public interface ISensorDataProcessor
{
    Task ProcessAsync(TelemetryReadingDto message, CancellationToken cancellationToken = default);
}
