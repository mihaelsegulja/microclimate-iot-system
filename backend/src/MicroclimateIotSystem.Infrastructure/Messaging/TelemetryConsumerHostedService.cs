using System.Text.Json;
using MicroclimateIotSystem.Application.Configurations;
using MicroclimateIotSystem.Application.DTOs;
using MicroclimateIotSystem.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MicroclimateIotSystem.Infrastructure.Messaging;

public class TelemetryConsumerHostedService : BackgroundService
{
    private readonly IConnection _connection;
    private readonly RabbitMqOptions _options;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TelemetryConsumerHostedService> _logger;

    public TelemetryConsumerHostedService(
        IConnection connection,
        IOptions<RabbitMqOptions> options,
        IServiceProvider serviceProvider,
        ILogger<TelemetryConsumerHostedService> logger)
    {
        _connection = connection;
        _options = options.Value;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += HandleMessageAsync;

        await channel.BasicConsumeAsync(
            queue: _options.TelemetryQueue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken);

        _logger.LogInformation("Listening on queue: {Queue}", _options.TelemetryQueue);

        await Task.Delay(Timeout.Infinite, cancellationToken);
    }

    private async Task HandleMessageAsync(object sender, BasicDeliverEventArgs eventArgs)
    {
        var channel = ((AsyncEventingBasicConsumer)sender).Channel;
        var body = eventArgs.Body.ToArray();

        try
        {
            var message = JsonSerializer.Deserialize<TelemetryReadingDto>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (message is null)
            {
                _logger.LogWarning("Received null message, discarding");
                await channel.BasicNackAsync(eventArgs.DeliveryTag, false, false);
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<ISensorDataProcessor>();
            await processor.ProcessAsync(message);

            await channel.BasicAckAsync(eventArgs.DeliveryTag, false);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize message, sending to DLQ");
            await channel.BasicNackAsync(eventArgs.DeliveryTag, false, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message, sending to DLQ");
            await channel.BasicNackAsync(eventArgs.DeliveryTag, false, false);
        }
    }
}
