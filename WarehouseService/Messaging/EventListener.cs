using Domain;
using Domain.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Domain.MessagingDefinitions;

namespace WarehouseService.Messaging
{
    public class EventListener : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly RabbitMqOptions _opts;

        private IConnection? _connection;
        private IChannel? _channel;

        public EventListener(IServiceProvider serviceProvider, IOptions<RabbitMqOptions> options)
        {
            _serviceProvider = serviceProvider;
            _opts = options.Value;
        }

        // ------------------------------
        // RabbitMQ Connection w/ Retry
        // ------------------------------
        private async Task<IConnection> CreateConnectionAsync(ConnectionFactory factory)
        {
            var retries = _opts.RetryCount;
            var delay = _opts.RetryDelayMs;

            while (retries > 0)
            {
                try
                {
                    Console.WriteLine($"[RabbitMQ] Trying to connect... Retries left: {retries}");
                    var conn = await factory.CreateConnectionAsync();
                    Console.WriteLine("[RabbitMQ] Connected successfully.");
                    return conn;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[RabbitMQ] Connection failed: {ex.Message}");
                    retries--;
                    await Task.Delay(delay);
                }
            }

            throw new Exception("[RabbitMQ] Failed to connect after retries.");
        }

        // ------------------------------
        // StartAsync - Declare exchanges/queues
        // ------------------------------
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            var factory = new ConnectionFactory
            {
                HostName = _opts.Host,
                Port = _opts.Port,
                UserName = _opts.UserName,
                Password = _opts.Password
            };

            _connection = await CreateConnectionAsync(factory);
            _channel = await _connection.CreateChannelAsync();

            // --- BookAdded Fanout setup ---
            await _channel.ExchangeDeclareAsync(
                exchange: _opts.BookAddedExchange,
                type: ExchangeType.Fanout,
                durable: true,
                autoDelete: false,
                arguments: null);

            await _channel.QueueDeclareAsync(
                queue: _opts.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            await _channel.QueueBindAsync(
                queue: _opts.QueueName,
                exchange: _opts.BookAddedExchange,
                routingKey: string.Empty,
                arguments: null);

            try
            {
                await _channel.BasicQosAsync(0, _opts.PrefetchCount, false);
            }
            catch { }

            // --- Order Validation Topic setup ---
            await _channel.ExchangeDeclareAsync(
                exchange: RabbitMqOrderDomain.OrderExchange,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                arguments: null);

            await _channel.QueueDeclareAsync(
                queue: RabbitMqOrderDomain.OrderRequestValidationQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            await _channel.QueueBindAsync(
                queue: RabbitMqOrderDomain.OrderRequestValidationQueue,
                exchange: RabbitMqOrderDomain.OrderExchange,
                routingKey: RabbitMqOrderDomain.OrderRequestValidationKey,
                arguments: null);

            await base.StartAsync(cancellationToken);
        }

        // ------------------------------
        // ExecuteAsync - Consumers
        // ------------------------------
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_channel == null) return;

            // -----------------------
            // BOOK.ADDED CONSUMER
            // -----------------------
            var bookConsumer = new AsyncEventingBasicConsumer(_channel);

            bookConsumer.ReceivedAsync += async (sender, ea) =>
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());

                try
                {
                    var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    opts.Converters.Add(new JsonStringEnumConverter());

                    var evt = JsonSerializer.Deserialize<BookAddedEvent>(json, opts);

                    if (evt != null)
                    {
                        Console.WriteLine($"WAREHOUSE_RECEIVED BookAddedEvent BookId={evt.BookId} InitialStock={evt.InitialStock}");

                        using var scope = _serviceProvider.CreateScope();
                        var svc = scope.ServiceProvider.GetRequiredService<IWarehouseService>();

                        await svc.HandleBookAddedAsync(evt);

                        Console.WriteLine($"WAREHOUSE_PROCESSED BookAddedEvent BookId={evt.BookId}");

                        await _channel.BasicAckAsync(ea.DeliveryTag, false);
                    }
                    else
                    {
                        await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to process BookAddedEvent: {ex}");
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
                }
            };

            await _channel.BasicConsumeAsync(
                queue: _opts.QueueName,
                autoAck: false,
                consumer: bookConsumer);

            // -----------------------
            // ORDER.VALIDATION REQUEST CONSUMER
            // -----------------------
            var orderConsumer = new AsyncEventingBasicConsumer(_channel);

            orderConsumer.ReceivedAsync += async (sender, ea) =>
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());

                try
                {
                    var req = JsonSerializer.Deserialize<OrderValidationRequestMessage>(json);

                    if (req != null)
                    {
                        Console.WriteLine($"WAREHOUSE_RECEIVED OrderValidationRequest OrderId={req.OrderId} CorrelationId={req.CorrelationId}");

                        using var scope = _serviceProvider.CreateScope();
                        var svc = scope.ServiceProvider.GetRequiredService<IWarehouseService>();
                        var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

                        var result = await svc.ValidateOrderAsync(req);

                        if (result.IsValid)
                        {
                            var ok = new WarehouseOrderValidatedMessage
                            {
                                OrderId = req.OrderId,
                                CorrelationId = req.CorrelationId,
                                ValidatedAt = DateTime.UtcNow
                            };

                            await publisher.PublishOrderValidatedAsync(ok);
                            Console.WriteLine($"WAREHOUSE_PUBLISHED OrderValidated OrderId={req.OrderId}");
                        }
                        else
                        {
                            var rej = new WarehouseOrderRejectedMessage
                            {
                                OrderId = req.OrderId,
                                CorrelationId = req.CorrelationId,
                                Reasons = result.Reasons,
                                RejectedAt = DateTime.UtcNow
                            };

                            await publisher.PublishOrderRejectedAsync(rej);
                            Console.WriteLine($"WAREHOUSE_PUBLISHED OrderRejected OrderId={req.OrderId}");
                        }

                        await _channel.BasicAckAsync(ea.DeliveryTag, false);
                    }
                    else
                    {
                        await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to process OrderValidationRequest: {ex}");
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
                }
            };

            await _channel.BasicConsumeAsync(
                queue: RabbitMqOrderDomain.OrderRequestValidationQueue,
                autoAck: false,
                consumer: orderConsumer);

            // Keep service alive
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        // ------------------------------
        // StopAsync - Cleanup
        // ------------------------------
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (_channel != null)
                {
                    try { await _channel.CloseAsync(); } catch { }
                    try { _channel.Dispose(); } catch { }
                }
                if (_connection != null)
                {
                    try { await _connection.CloseAsync(); } catch { }
                    try { _connection.Dispose(); } catch { }
                }
            }
            catch { }

            await base.StopAsync(cancellationToken);
        }
    }
}
