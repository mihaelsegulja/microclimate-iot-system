using MicroclimateIotSystem.Application.DTOs;

namespace MicroclimateIotSystem.Application.Interfaces;

public interface IAlertEvaluator
{
    Task EvaluateAsync(int deviceId, int? roomId, string hardwareId, TelemetryReadingDto message, CancellationToken cancellationToken = default);
}