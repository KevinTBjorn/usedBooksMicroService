using Domain.Events;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SearchService.Repositories;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SearchService.Messaging
{
    /// <summary>
    /// Manual RabbitMQ listener that consumes BookAddedEvent from MassTransit exchange
    /// Works alongside MassTransit by connecting to the same exchange but with its own queue
    /// </summary>
    public class BookAddedListener : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly RabbitMqOptions _opts;
        private IConnection? _connection;
        private IModel? _channel;

        public BookAddedListener(IServiceProvider serviceProvider, IOptions<RabbitMqOptions> options)
        {
            _serviceProvider = serviceProvider;
            _opts = options.Value;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("SEARCH_BOOK_LISTENER_STARTING");
            return base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory()
            {
                HostName = _opts.Host,
                Port = _opts.Port,
                UserName = _opts.UserName,
                Password = _opts.Password,
                DispatchConsumersAsync = true
            };

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    try
                    {
                        _connection = factory.CreateConnection();
                        Console.WriteLine($"SEARCH_BOOK_CONNECTED Host={_opts.Host} User={_opts.UserName}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"SEARCH_BOOK_RETRY_CONNECT error={ex.Message}");
                        await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken).ContinueWith(_ => { });
                        continue;
                    }

                    _channel = _connection.CreateModel();

                    // MassTransit uses: {namespace}:{class} format
                    var massTransitExchange = "Domain.Events:BookAddedEvent";

                    _channel.ExchangeDeclare(
                        exchange: massTransitExchange,
                        type: ExchangeType.Fanout, // MassTransit uses fanout for events
                        durable: true,
                        autoDelete: false);

                    // Create our own queue for SearchService
                    _channel.QueueDeclare(
                        queue: _opts.BookAddedQueue,
                        durable: true,
                        exclusive: false,
                        autoDelete: false);

                    // Bind to MassTransit exchange
                    _channel.QueueBind(
                        queue: _opts.BookAddedQueue,
                        exchange: massTransitExchange,
                        routingKey: string.Empty); // Fanout ignores routing keys

                    _channel.BasicQos(0, _opts.PrefetchCount, false);

                    Console.WriteLine($"SEARCH_BOOK_QUEUE_DECLARED Queue={_opts.BookAddedQueue} Exchange={massTransitExchange}");

                    var consumer = new AsyncEventingBasicConsumer(_channel);
                    consumer.Received += async (model, ea) =>
                    {
                        var body = ea.Body.ToArray();
                        var json = Encoding.UTF8.GetString(body);
                        Console.WriteLine($"SEARCH_BOOK_RECEIVED Raw={json.Substring(0, Math.Min(200, json.Length))}...");

                        try
                        {
                            var options = new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            };
                            options.Converters.Add(new JsonStringEnumConverter());

                            // Try to deserialize as MassTransit envelope first
                            BookAddedEvent? evt = null;

                            try
                            {
                                // Check if this is a MassTransit wrapped message
                                using var jsonDoc = JsonDocument.Parse(json);
                                if (jsonDoc.RootElement.TryGetProperty("message", out var messageProperty))
                                {
                                    // This is a MassTransit message - extract the inner message
                                    var innerJson = messageProperty.GetRawText();
                                    Console.WriteLine($"SEARCH_BOOK_MASSTRANSIT_DETECTED Extracting inner message");
                                    evt = JsonSerializer.Deserialize<BookAddedEvent>(innerJson, options);
                                }
                                else
                                {
                                    // This is a plain message (direct)
                                    evt = JsonSerializer.Deserialize<BookAddedEvent>(json, options);
                                }
                            }
                            catch (JsonException)
                            {
                                // Fallback: try direct deserialization
                                evt = JsonSerializer.Deserialize<BookAddedEvent>(json, options);
                            }

                            if (evt == null)
                            {
                                Console.WriteLine("SEARCH_BOOK_DESERIALIZE_FAILED");
                                try { _channel.BasicNack(ea.DeliveryTag, false, requeue: false); } catch { }
                                return;
                            }

                            Console.WriteLine($"SEARCH_BOOK_DESERIALIZED BookId={evt.BookId} Title={evt.Title} Genre={evt.Genre}");

                            using var scope = _serviceProvider.CreateScope();
                            var repo = scope.ServiceProvider.GetRequiredService<ISearchRepository>();

                            // Map to Domain.Book metadata using public constructor
                            var book = new Domain.Book(
                                evt.BookId,
                                evt.Title,
                                evt.ISBN,
                                evt.Description,
                                evt.Edition,
                                evt.Year,
                                evt.Author,
                                evt.ImageUrl,
                                evt.Genre);

                            await repo.AddOrUpdateMetadataAsync(book);
                            Console.WriteLine($"SEARCH_BOOK_METADATA_STORED BookId={evt.BookId}");

                            try { _channel.BasicAck(ea.DeliveryTag, false); } catch { }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"SEARCH_BOOK_PROCESS_ERROR: {ex.Message}");
                            Console.WriteLine($"SEARCH_BOOK_STACK_TRACE: {ex.StackTrace}");
                            try { _channel.BasicNack(ea.DeliveryTag, false, requeue: true); } catch { }
                        }
                    };

                    _channel.BasicConsume(queue: _opts.BookAddedQueue, autoAck: false, consumer: consumer);
                    Console.WriteLine("SEARCH_BOOK_CONSUMER_STARTED");

                    while (!stoppingToken.IsCancellationRequested && _connection != null && _connection.IsOpen)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken).ContinueWith(_ => { });
                    }

                    Console.WriteLine("SEARCH_BOOK_CONNECTION_LOST — will retry");
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"SEARCH_BOOK_UNEXPECTED_ERROR: {ex.Message}");
                }
                finally
                {
                    try { _channel?.Close(); } catch { }
                    try { _connection?.Close(); } catch { }
                    _channel = null;
                    _connection = null;
                }

                try { await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken); } catch { }
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("SEARCH_BOOK_LISTENER_STOPPING");
            try { _channel?.Close(); _connection?.Close(); } catch { }
            return base.StopAsync(cancellationToken);
        }
    }
}