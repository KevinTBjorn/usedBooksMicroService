using Domain.Events;
using Domain.MessagingDefinitions;
using OrderService.Data;
using OrderService.Messaging;
using OrderService.Metrics;
using OrderService.Models;
using RabbitMQ.Client;
using System.Text.Json;

namespace OrderService.Consumers;

public class WarehouseOrderValidatedConsumer : RabbitConsumerBase
{
    public WarehouseOrderValidatedConsumer(IConnectionFactory factory, IServiceProvider services)
            : base(factory, services, exchangeName: RabbitMqOrderDomain.OrderExchange, queueName: RabbitMqOrderDomain.OrderValidatedQueue, routingKey: RabbitMqOrderDomain.OrderValidatedKey) { }

    protected override async Task HandleMessageAsync(string json, IServiceScope scope)
    {
        var options = scope.ServiceProvider.GetRequiredService<JsonSerializerOptions>();
        var msg = JsonSerializer.Deserialize<WarehouseOrderValidatedMessage>(json, options)!;

        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        var order = await db.Orders.FindAsync(msg.OrderId);

        if (order == null)
            return;

        order.Status = OrderStatus.Validated;
        order.ValidatedAt = msg.ValidatedAt;

        OrderMetrics.OrdersValidated.Inc();

        await db.SaveChangesAsync();
    }
}


