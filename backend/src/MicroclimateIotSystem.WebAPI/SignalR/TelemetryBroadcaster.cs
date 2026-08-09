using MicroclimateIotSystem.Application.DTOs;
using MicroclimateIotSystem.Application.Interfaces;
using MicroclimateIotSystem.WebAPI.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace MicroclimateIotSystem.WebAPI.SignalR;

public class TelemetryBroadcaster(IHubContext<NotificationHub> hubContext) : ITelemetryBroadcaster
{
    public Task BroadcastAsync(TelemetryReadingDto message, CancellationToken cancellationToken = default)
    {
        return hubContext.Clients
            .Group(NotificationHub.DeviceGroup(message.HardwareId))
            .SendAsync("TelemetryReceived", message, cancellationToken);
    }
}