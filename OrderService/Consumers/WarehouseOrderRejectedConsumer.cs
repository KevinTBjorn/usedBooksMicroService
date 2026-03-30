using Domain.Events;
using Domain.MessagingDefinitions;
using OrderService.Data;
using OrderService.Messaging;
using OrderService.Metrics;
using OrderService.Models;
using RabbitMQ.Client;
using System.Text.Json;

namespace OrderService.Consumers;

public class WarehouseOrderRejectedConsumer : RabbitConsumerBase
{
    public WarehouseOrderRejectedConsumer(IConnectionFactory factory, IServiceProvider services)
            : base(factory, services, exchangeName: RabbitMqOrderDomain.OrderExchange, queueName: RabbitMqOrderDomain.OrderRejectedQueue, routingKey: RabbitMqOrderDomain.OrderRejectedKey) { }

    protected override async Task HandleMessageAsync(string json, IServiceScope scope)
    {
        var options = scope.ServiceProvider.GetRequiredService<JsonSerializerOptions>();
        var msg = JsonSerializer.Deserialize<WarehouseOrderRejectedMessage>(json, options)!;

        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        var order = await db.Orders.FindAsync(msg.OrderId);

        if (order == null)
            return;

        order.Status = OrderStatus.Rejected;
        order.RejectionReasons = string.Join("; ", msg.Reasons);
        order.RejectedAt = msg.RejectedAt;

        OrderMetrics.OrdersRejected.Inc();

        await db.SaveChangesAsync();
    }
}


