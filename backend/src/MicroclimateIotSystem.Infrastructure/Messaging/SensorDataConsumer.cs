using System.Text;
using System.Text.Json;
using MicroclimateIotSystem.Application.Configurations;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MicroclimateIotSystem.Infrastructure.Messaging;

public class SensorDataConsumer : BackgroundService
{
    private readonly IConnection _connection;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<SensorDataConsumer> _logger;

    public SensorDataConsumer(
        IConnection connection,
        IOptions<RabbitMqOptions> options,
        ILogger<SensorDataConsumer> logger)
    {
        _connection = connection;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        // TODO: Define queue name and binding keys in configuration
        const string queueName = "sensor_data.ingest";
        const string routingKey = "sensor.reading.*";

        var channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(
            exchange: _options.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: queueName,
            exchange: _options.ExchangeName,
            routingKey: routingKey,
            cancellationToken: cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            try
            {
                var body = eventArgs.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                // TODO: Deserialize to SensorReadingMessage and call ISensorDataProcessor
                // var sensorData = JsonSerializer.Deserialize<SensorReadingMessage>(body);
                // await processor.ProcessAsync(sensorData, stoppingToken);

                _logger.LogInformation("Received sensor reading: {Message}", message);

                // TODO: Handle processing errors with retry/dead-letter
                await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing sensor reading message");
                // TODO: Route to dead-letter queue after retries
                await channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: true, cancellationToken: cancellationToken);
                await Task.Delay(1000, cancellationToken);
            }
        };

        await channel.BasicConsumeAsync(
            queue: queueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken);

        _logger.LogInformation("SensorDataConsumer listening on queue: {QueueName}", queueName);

        await Task.Delay(Timeout.Infinite, cancellationToken);
    }
}
