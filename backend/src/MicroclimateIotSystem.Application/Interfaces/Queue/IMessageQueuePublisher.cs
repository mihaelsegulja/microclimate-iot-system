namespace MicroclimateIotSystem.Application.Interfaces.Queue;

public interface IMessageQueuePublisher
{
    Task PublishAsync<T>(string routingKey, T message);
}
