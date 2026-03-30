using System;
using System.Text;
using System.Text.Json;
using Domain.Events;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SearchService.Repositories;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using System.Threading.Tasks;

namespace SearchService.Messaging
{
    public class StockUpdatedListener : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly RabbitMqOptions _opts;
        private IConnection? _connection;
        private IModel? _channel;

        public StockUpdatedListener(IServiceProvider serviceProvider, IOptions<RabbitMqOptions> options)
        {
            _serviceProvider = serviceProvider;
            _opts = options.Value;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("SEARCH_LISTENER_STARTING");
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
                AutomaticRecoveryEnabled = false, // using custom retry loop
                DispatchConsumersAsync = true
            };

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Try connect
                    try
                    {
                        _connection = factory.CreateConnection();
                        Console.WriteLine($"SEARCH_CONNECTED Host={_opts.Host} User={_opts.UserName}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"SEARCH_RETRY_CONNECT error={ex.Message}");
                        // wait then retry
                        await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken).ContinueWith(_ => { });
                        continue;
                    }

                    // Create channel and declare topology
                    _channel = _connection.CreateModel();

                    _channel.ExchangeDeclare(_opts.StockUpdatedExchange, ExchangeType.Fanout, durable: true);
                    _channel.QueueDeclare(_opts.StockUpdatedQueue, durable: true, exclusive: false, autoDelete: false);
                    _channel.QueueBind(_opts.StockUpdatedQueue, _opts.StockUpdatedExchange, string.Empty);
                    _channel.BasicQos(0, _opts.PrefetchCount, false);

                    Console.WriteLine("SEARCH_QUEUE_DECLARED");

                    // Setup consumer
                    var consumer = new AsyncEventingBasicConsumer(_channel);
                    consumer.Received += async (model, ea) =>
                    {
                        var body = ea.Body.ToArray();
                        var json = Encoding.UTF8.GetString(body);
                        Console.WriteLine($"SEARCH_RECEIVED Raw={json}");

                        try
                        {
                            var options = new System.Text.Json.JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            };
                            options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

                            var evt = System.Text.Json.JsonSerializer.Deserialize<Domain.Events.StockUpdatedEvent>(json, options);
                            if (evt == null)
                            {
                                Console.WriteLine("SEARCH_DESERIALIZE_FAILED RawPayload=" + json);
                                try { _channel.BasicNack(ea.DeliveryTag, false, requeue: false); } catch { }
                                return;
                            }

                            Console.WriteLine($"SEARCH_DESERIALIZED BookId={evt.BookId} Stock={evt.Stock} Title={evt.Title}");

                            // Process update via repository
                            using var scope = _serviceProvider.CreateScope();
                            var repo = scope.ServiceProvider.GetRequiredService<ISearchRepository>();
                            
                            // Store/update book metadata (includes all book info from event)
                            var book = new Domain.Book(
                                evt.BookId,
                                evt.Title,
                                evt.ISBN,
                                evt.Description,
                                evt.Edition,
                                evt.Year,
                                evt.Author,
                                evt.ImageUrl,
                                evt.Genre
                            );
                            
                            await repo.AddOrUpdateMetadataAsync(book);
                            await repo.UpdateQuantityAsync(evt.BookId, evt.Stock);

                            Console.WriteLine($"SEARCH_UPDATED BookId={evt.BookId} Stock={evt.Stock} Title={evt.Title}");

                            try { _channel.BasicAck(ea.DeliveryTag, false); } catch (Exception ackEx) { Console.WriteLine($"SEARCH_ACK_ERROR: {ackEx}"); }
                        }
                        catch (Exception ex)
                        {
                            // full exception logging
                            Console.WriteLine($"SEARCH_PROCESS_ERROR: {ex.Message}\n{ex.StackTrace}");
                            try { _channel.BasicNack(ea.DeliveryTag, false, requeue: true); } catch (Exception nackEx) { Console.WriteLine($"SEARCH_NACK_ERROR: {nackEx}"); }
                        }
                    };

                    _channel.BasicConsume(queue: _opts.StockUpdatedQueue, autoAck: false, consumer: consumer);
                    Console.WriteLine("SEARCH_CONSUMER_STARTED");

                    // Monitor connection and channel; if closed, cleanup and retry
                    while (!stoppingToken.IsCancellationRequested && _connection != null && _connection.IsOpen)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken).ContinueWith(_ => { });
                    }

                    Console.WriteLine("SEARCH_CONNECTION_LOST — will retry");
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // graceful shutdown
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"SEARCH_UNEXPECTED_ERROR: {ex}");
                }
                finally
                {
                    try
                    {
                        _channel?.Close();
                    }
                    catch { }
                    try
                    {
                        _connection?.Close();
                    }
                    catch { }

                    _channel = null;
                    _connection = null;
                }

                // Wait before reconnect attempt
                try { await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken); } catch { }
            }

            // end ExecuteAsync
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("SEARCH_LISTENER_STOPPING");
            try
            {
                _channel?.Close();
                _connection?.Close();
            }
            catch { }
            return base.StopAsync(cancellationToken);
        }
    }
}
