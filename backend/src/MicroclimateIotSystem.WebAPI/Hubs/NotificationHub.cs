using Microsoft.AspNetCore.SignalR;

namespace MicroclimateIotSystem.WebAPI.Hubs;

public class NotificationHub : Hub
{
    private const string DeviceGroupPrefix = "device:";

    public const string AlertsGroup = "alert:all";

    public Task JoinDevice(string hardwareId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, DeviceGroup(hardwareId));
    }

    public Task LeaveDevice(string hardwareId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, DeviceGroup(hardwareId));
    }

    public Task JoinAlerts()
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, AlertsGroup);
    }

    public Task LeaveAlerts()
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, AlertsGroup);
    }

    public static string DeviceGroup(string hardwareId) => $"{DeviceGroupPrefix}{hardwareId}";
}
