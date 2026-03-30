using Domain.Events;
using Domain.MessagingDefinitions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Messaging;
using OrderService.Metrics;
using OrderService.Models;

namespace OrderService.Controllers;

[ApiController]
[Route("orders")]
public class OrdersController : ControllerBase
{
    private readonly OrderDbContext _db;
    private readonly IMessagePublisher _publisher;

    public OrdersController(OrderDbContext db, IMessagePublisher publisher)
    {
        _db = db;
        _publisher = publisher;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {

        var correlationId = Guid.NewGuid();

        // 1) Save order to DB
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            CustomerId = request.CustomerId,
            CreatedAt = DateTime.UtcNow,
            CorrelationId = correlationId,
            Status = OrderStatus.Pending
        };

        foreach (var item in request.Items)
        {
            order.Items.Add(new UserBook
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                UserId = request.UserId,
                BookId = item.BookId,
                Quantity = item.Quantity,
                Condition = item.Condition,
                Price = item.Price
            });
        }

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();


        OrderMetrics.OrdersCreated.Inc();


        // 3) Build warehouse validation request message
        var message = new OrderValidationRequestMessage
        {
            OrderId = order.Id,
            UserId = order.UserId,
            CustomerId = order.CustomerId,
            CorrelationId = correlationId,
            CreatedAt = DateTime.UtcNow,
            Items = order.Items.Select(i => new OrderItem
            {
                BookId = i.BookId,
                Quantity = i.Quantity,
                Condition = i.Condition,
                Price = i.Price
            }).ToList()
        };

        // 4) Publish message to RabbitMQ
        await _publisher.PublishAsync(
            exchange: RabbitMqOrderDomain.OrderExchange,
            routingKey: RabbitMqOrderDomain.OrderRequestValidationKey,
            message);

        // 5) Return response
        return Ok(new
        {
            OrderId = order.Id,
            CorrelationId = correlationId,
            Status = order.Status.ToString()
        });
    }

    [HttpGet("{orderId:guid}")]
    public async Task<IActionResult> GetOrderStatus(Guid orderId)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
            return NotFound(new { Message = "Order not found" });

        return Ok(new
        {
            order.Id,
            order.UserId,
            order.CustomerId,
            order.CreatedAt,
            Status = order.Status.ToString(),
            order.RejectionReasons,
            order.ValidatedAt,
            order.RejectedAt,
            Items = order.Items.Select(i => new
            {
                i.BookId,
                i.Quantity,
                i.Condition,
                i.Price
            })
        });
    }

}


