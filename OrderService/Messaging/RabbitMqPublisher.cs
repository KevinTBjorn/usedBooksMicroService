using Domain.MessagingDefinitions;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace OrderService.Messaging;

public class RabbitMqPublisher : IMessagePublisher
{
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public RabbitMqPublisher(IConnectionFactory factory)
    {
        _connection = RabbitMqConnectionHelper.CreateConnectionWithRetry(factory);
        _channel = _connection.CreateModel();

        _channel.ExchangeDeclare(
            exchange: RabbitMqOrderDomain.OrderExchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null);

    }

    public Task PublishAsync(string exchange, string routingKey, object message)
    {
        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var props = _channel.CreateBasicProperties();
        props.ContentType = "application/json";
        props.DeliveryMode = 2;

        _channel.BasicPublish(
            exchange: exchange,
            routingKey: routingKey,
            basicProperties: props,
            body: body);

        return Task.CompletedTask;
    }
    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
