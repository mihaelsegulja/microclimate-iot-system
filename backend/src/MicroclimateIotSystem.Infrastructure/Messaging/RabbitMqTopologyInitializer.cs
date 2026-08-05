using MicroclimateIotSystem.Application.Configurations;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace MicroclimateIotSystem.Infrastructure.Messaging;

public class RabbitMqTopologyInitializer : IHostedService
{
    private readonly IConnection _connection;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqTopologyInitializer> _logger;

    public RabbitMqTopologyInitializer(
        IConnection connection,
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqTopologyInitializer> logger)
    {
        _connection = connection;
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

            await channel.ExchangeDeclareAsync(
                exchange: _options.ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                cancellationToken: cancellationToken);

            await channel.ExchangeDeclareAsync(
                exchange: _options.DlxExchange,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false,
                cancellationToken: cancellationToken);

            var queueArgs = new Dictionary<string, object?>
            {
                { "x-dead-letter-exchange", _options.DlxExchange }
            };

            await channel.QueueDeclareAsync(
                queue: _options.TelemetryQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: queueArgs,
                cancellationToken: cancellationToken);

            await channel.QueueBindAsync(
                queue: _options.TelemetryQueue,
                exchange: _options.ExchangeName,
                routingKey: _options.TelemetryRoutingPattern,
                cancellationToken: cancellationToken);

            await channel.QueueDeclareAsync(
                queue: _options.TelemetryDlq,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: cancellationToken);

            _logger.LogInformation("RabbitMQ topology initialized");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ topology");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
