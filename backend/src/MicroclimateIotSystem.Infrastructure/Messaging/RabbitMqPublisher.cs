using System.Text;
using System.Text.Json;
using MicroclimateIotSystem.Application.Configurations;
using MicroclimateIotSystem.Application.Interfaces.Queue;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace MicroclimateIotSystem.Infrastructure.Messaging;

public class RabbitMqPublisher : IMessageQueuePublisher, IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly RabbitMqOptions _options;
    private IChannel? _channel;

    public RabbitMqPublisher(IConnection connection, IOptions<RabbitMqOptions> options)
    {
        _connection = connection;
        _options = options.Value;
    }

    public async Task PublishAsync<T>(string routingKey, T message)
    {
        // TODO: Implement channel pooling / lazy channel creation
        _channel ??= await _connection.CreateChannelAsync();

        // TODO: Move exchange/queue topology setup to a startup configuration
        await _channel.ExchangeDeclareAsync(
            exchange: _options.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);

        var body = JsonSerializer.SerializeToUtf8Bytes(message);

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json"
        };

        await _channel.BasicPublishAsync(
            exchange: _options.ExchangeName,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: body);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
            await _channel.CloseAsync();
    }
}
