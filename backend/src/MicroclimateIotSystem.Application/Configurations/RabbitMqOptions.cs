namespace MicroclimateIotSystem.Application.Configurations;

public class RabbitMqOptions
{
    public string Host { get; set; }
    public int Port { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string VirtualHost { get; set; }
    public string ExchangeName { get; set; }
    public string DlxExchange { get; set; }
    public string TelemetryQueue { get; set; }
    public string TelemetryDlq { get; set; }
    public string TelemetryRoutingPattern { get; set; }
    public string CommandRoutingPrefix { get; set; }
}
