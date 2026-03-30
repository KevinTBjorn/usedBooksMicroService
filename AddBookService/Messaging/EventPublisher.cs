//using System.Text;
//using System.Text.Json;
//using AddBookService.Services;
//using Microsoft.Extensions.Options;
//using RabbitMQ.Client;

//namespace AddBookService.Messaging;

//public sealed class EventPublisher : IAsyncDisposable
//{
//    private readonly IConnection _connection;
//    private readonly IChannel _channel;
//    private readonly RabbitMqOptions _opts;

//    public EventPublisher(IOptions<RabbitMqOptions> options)
//    {
//        _opts = options.Value;

//        var factory = new ConnectionFactory
//        {
//            HostName = _opts.Host,
//            Port = _opts.Port,
//            UserName = _opts.UserName,
//            Password = _opts.Password
//        };

//        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
//        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

//        _channel.ExchangeDeclareAsync(
//            exchange: _opts.BookAddedExchange,
//            type: ExchangeType.Fanout,
//            durable: true
//        ).GetAwaiter().GetResult();
//    }

//    public async Task PublishBookAddedAsync(BookAddedEvent evt)
//    {
//        var json = JsonSerializer.Serialize(evt);
//        var body = Encoding.UTF8.GetBytes(json);

//        var props = new BasicProperties
//        {
//            ContentType = "application/json",
//            DeliveryMode = DeliveryModes.Persistent
//        };

//        await _channel.BasicPublishAsync(
//            exchange: _opts.BookAddedExchange,
//            routingKey: string.Empty,
//            basicProperties: props,
//            body: body
//        );
//    }

//    public async ValueTask DisposeAsync()
//    {
//        await _channel.DisposeAsync();
//        await _connection.DisposeAsync();
//    }

//}
