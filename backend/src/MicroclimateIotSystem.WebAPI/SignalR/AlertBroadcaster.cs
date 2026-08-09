using MicroclimateIotSystem.Application.DTOs;
using MicroclimateIotSystem.Application.Interfaces;
using MicroclimateIotSystem.Domain.Enums;
using MicroclimateIotSystem.WebAPI.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace MicroclimateIotSystem.WebAPI.SignalR;

public class AlertBroadcaster(IHubContext<NotificationHub> hubContext) : IAlertBroadcaster
{
    public Task BroadcastAsync(AlertEventDto alert, CancellationToken cancellationToken = default)
    {
        var method = alert.Status == AlertStatus.Active ? "AlertTriggered" : "AlertCleared";
        return hubContext.Clients
            .Group(NotificationHub.AlertsGroup)
            .SendAsync(method, alert, cancellationToken);
    }
}