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
    private readonly SemaphoreSlim _channelLock = new(1, 1);

    public RabbitMqPublisher(IConnection connection, IOptions<RabbitMqOptions> options)
    {
        _connection = connection;
        _options = options.Value;
    }

    public async Task PublishAsync<T>(string routingKey, T message)
    {
        var channel = await GetChannelAsync();

        var body = JsonSerializer.SerializeToUtf8Bytes(message);

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json"
        };

        await channel.BasicPublishAsync(
            exchange: _options.ExchangeName,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: body);
    }

    private async Task<IChannel> GetChannelAsync()
    {
        if (_channel is { IsOpen: false })
        {
            await _channel.CloseAsync();
            _channel = null;
        }

        if (_channel is null)
        {
            await _channelLock.WaitAsync();
            try
            {
                if (_channel is null)
                    _channel = await _connection.CreateChannelAsync();
            }
            finally
            {
                _channelLock.Release();
            }
        }

        return _channel;
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
            await _channel.CloseAsync();
    }
}
