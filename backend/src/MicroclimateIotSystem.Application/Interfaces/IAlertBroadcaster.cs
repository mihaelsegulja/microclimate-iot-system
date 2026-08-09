using MicroclimateIotSystem.Application.DTOs;

namespace MicroclimateIotSystem.Application.Interfaces;

public interface IAlertBroadcaster
{
    Task BroadcastAsync(AlertEventDto alert, CancellationToken cancellationToken = default);
}