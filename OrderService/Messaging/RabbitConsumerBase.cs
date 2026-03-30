using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace OrderService.Messaging;

public abstract class RabbitConsumerBase : BackgroundService
{
    protected readonly IConnection _connection;
    protected readonly IModel _channel;
    protected readonly IServiceProvider _services;

    private readonly string _exchangeName;
    private readonly string _queueName;
    private readonly string _routingKey;



    protected RabbitConsumerBase(IConnectionFactory factory, IServiceProvider services, string exchangeName, string queueName, string routingKey)
    {
        _services = services;
        _exchangeName = exchangeName;
        _queueName = queueName;
        _routingKey = routingKey;

        _connection = RabbitMqConnectionHelper.CreateConnectionWithRetry(factory);
        _channel = _connection.CreateModel();

        var deadLetterExchange = $"{_exchangeName}.dlx";
        var deadLetterQueue = $"{_queueName}.dlq";

        // Declare your exchange
        _channel.ExchangeDeclare(
            exchange: _exchangeName,
            type: ExchangeType.Topic,
            durable: true);

        _channel.ExchangeDeclare(
            exchange: deadLetterExchange,
            type: ExchangeType.Topic,
            durable: true);

        // Declare your queue
        var queueArgs = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", deadLetterExchange },
            { "x-dead-letter-routing-key", deadLetterQueue }
        };

        _channel.QueueDeclare(
            queue: _queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: queueArgs);

        _channel.QueueDeclare(
            queue: deadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        // Bind queue to exchange
        _channel.QueueBind(
            queue: _queueName,
            exchange: _exchangeName,
            routingKey: _routingKey);

        _channel.QueueBind(
            queue: deadLetterQueue,
            exchange: deadLetterExchange,
            routingKey: deadLetterQueue);

        _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
    }

    protected abstract Task HandleMessageAsync(string body, IServiceScope scope);

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += async (_, ea) =>
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());

            using var scope = _services.CreateScope();
            try
            {
                await HandleMessageAsync(json, scope);

                _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            }
            catch (Exception)
            {
                _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        _channel.BasicConsume(
            queue: _queueName,
            autoAck: false,
            consumer: consumer);

        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
        base.Dispose();
    }
}

