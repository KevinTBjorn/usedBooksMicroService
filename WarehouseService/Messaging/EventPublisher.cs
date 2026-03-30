using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Domain.Events;
using System.Threading.Tasks;
using Domain.MessagingDefinitions;
using Prometheus;

namespace WarehouseService.Messaging
{
    public class EventPublisher : IEventPublisher, IDisposable
    {
        private readonly RabbitMqOptions _opts;
        private readonly IConnection _connection;
        private readonly IChannel _channel;

        private static readonly Counter ConnectionRetryCounter = Metrics.CreateCounter("rabbitmq_connection_retry_total", "Total RabbitMQ connection retry attempts");
        private static readonly Counter ConnectionSuccessCounter = Metrics.CreateCounter("rabbitmq_connection_success_total", "Successful RabbitMQ connections established");

        private async Task<IConnection> CreateConnectionAsync(ConnectionFactory factory)
        {
            var retries = _opts.RetryCount;
            var delay = _opts.RetryDelayMs;

            while (retries > 0)
            {
                try
                {
                    Console.WriteLine("[Publisher] Connecting to RabbitMQ...");
                    var conn = await factory.CreateConnectionAsync();
                    Console.WriteLine("[Publisher] Connected.");
                    ConnectionSuccessCounter.Inc();
                    return conn;
                }
                catch (Exception ex)
                {
                    ConnectionRetryCounter.Inc();
                    Console.WriteLine($"[Publisher] Connection failed: {ex.Message}. Retries left: {retries}");
                    retries--;
                    await Task.Delay(delay);
                }
            }

            throw new Exception("[Publisher] Unable to connect after retries");
        }

        public EventPublisher(IOptions<RabbitMqOptions> options)
        {
            _opts = options.Value;

            var factory = new ConnectionFactory
            {
                HostName = _opts.Host,
                Port = _opts.Port,
                UserName = _opts.UserName,
                Password = _opts.Password
            };

            _connection = CreateConnectionAsync(factory).GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

            // ensure exchanges exist (use async declarations)
            try { _channel.ExchangeDeclareAsync(exchange: RabbitMqOrderDomain.OrderExchange, type: ExchangeType.Topic, durable: true, autoDelete: false, arguments: null).GetAwaiter().GetResult(); } catch { }
            try { _channel.ExchangeDeclareAsync(exchange: "book.events", type: ExchangeType.Fanout, durable: true, autoDelete: false, arguments: null).GetAwaiter().GetResult(); } catch { }
            try { _channel.ExchangeDeclareAsync(exchange: _opts.StockUpdatedExchange, type: ExchangeType.Fanout, durable: true, autoDelete: false, arguments: null).GetAwaiter().GetResult(); } catch { }
        }

        public async Task PublishStockUpdatedAsync(StockUpdatedEvent evt)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = false };
            options.Converters.Add(new JsonStringEnumConverter());

            var json = JsonSerializer.Serialize(evt, options);
            var body = Encoding.UTF8.GetBytes(json);

            var props = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent
            };

            try
            {
                Console.WriteLine("EVENTPUBLISHER_BEFORE_BASICPUBLISH");
                await _channel.BasicPublishAsync(exchange: _opts.StockUpdatedExchange, routingKey: string.Empty, mandatory: false, basicProperties: props, body: body);
                Console.WriteLine("EVENTPUBLISHER_AFTER_BASICPUBLISH");
                Console.WriteLine($"EVENT_PUBLISHED StockUpdatedEvent BookId={evt.BookId} Stock={evt.Stock}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EVENTPUBLISHER_ERROR: {ex}");
                throw;
            }
        }

        public async Task PublishOrderValidatedAsync(WarehouseOrderValidatedMessage msg)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = false };
            options.Converters.Add(new JsonStringEnumConverter());

            var json = JsonSerializer.Serialize(msg, options);
            var body = Encoding.UTF8.GetBytes(json);
            
            var props = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent
            };

            await _channel.BasicPublishAsync(exchange: RabbitMqOrderDomain.OrderExchange, routingKey: RabbitMqOrderDomain.OrderValidatedKey, mandatory: false, basicProperties: props, body: body);
            Console.WriteLine($"PUBLISHED_WAREHOUSE_ORDER_VALIDATED OrderId={msg.OrderId}");
        }

        public async Task PublishOrderRejectedAsync(WarehouseOrderRejectedMessage msg)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = false };
            options.Converters.Add(new JsonStringEnumConverter());

            var json = JsonSerializer.Serialize(msg, options);
            var body = Encoding.UTF8.GetBytes(json);
            
            var props = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent
            };

            await _channel.BasicPublishAsync(exchange: RabbitMqOrderDomain.OrderExchange, routingKey: RabbitMqOrderDomain.OrderRejectedKey, mandatory: false, basicProperties: props, body: body);
            Console.WriteLine($"PUBLISHED_WAREHOUSE_ORDER_REJECTED OrderId={msg.OrderId}");
        }

        public void Dispose()
        {
            try
            {
                try { _channel?.CloseAsync().GetAwaiter().GetResult(); } catch { }
                try { _channel?.Dispose(); } catch { }
                try { _connection?.CloseAsync().GetAwaiter().GetResult(); } catch { }
                try { _connection?.Dispose(); } catch { }
            }
            catch { }
        }
    }
}
