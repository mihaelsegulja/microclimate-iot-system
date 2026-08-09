namespace MicroclimateIotSystem.Application.Constants;

public static class CacheKeys
{
    public static string DeviceActive(string hardwareId) => $"device-active:{hardwareId}";

    public static string ActiveAlertRules() => "alert-rules:active";
}
