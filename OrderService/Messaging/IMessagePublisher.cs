namespace OrderService.Messaging;

public interface IMessagePublisher
{
    Task PublishAsync(string exchange, string routingKey, object message);
}
