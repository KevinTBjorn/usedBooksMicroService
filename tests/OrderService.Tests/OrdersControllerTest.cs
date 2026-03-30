using Domain.Events;
using Domain.MessagingDefinitions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using OrderService.Controllers;
using OrderService.Data;
using OrderService.Messaging;
using OrderService.Models;

namespace OrderService.Tests;

public class OrdersControllerTest
{
    private OrderDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new OrderDbContext(options);
    }

    [Fact]
    public async Task CreateOrder_ShouldSaveOrder_AndPublishMessage()
    {
        // Arrange
        var db = CreateDbContext();

        var publisherMock = new Mock<IMessagePublisher>();

        var controller = new OrdersController(db, publisherMock.Object);

        var request = new CreateOrderRequest
        {
            UserId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Items =
            {
                new CreateOrderItem
                {
                    BookId = Guid.NewGuid(),
                    Quantity = 1,
                    Condition = "New",
                    Price = 100
                }
            }
        };

        // Act
        var result = await controller.CreateOrder(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);

        var orderInDb = await db.Orders.FirstOrDefaultAsync();
        Assert.NotNull(orderInDb);
        Assert.Equal(OrderStatus.Pending, orderInDb.Status);

        publisherMock.Verify(p =>
            p.PublishAsync(
                RabbitMqOrderDomain.OrderExchange,
                RabbitMqOrderDomain.OrderRequestValidationKey,
                It.IsAny<OrderValidationRequestMessage>()),
            Times.Once);
    }

    [Fact]
    public async Task GetOrderStatus_ShouldReturnOrder_WhenExists()
    {
        // Arrange
        var db = CreateDbContext();

        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            Status = OrderStatus.Pending
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var publisherMock = new Mock<IMessagePublisher>();
        var controller = new OrdersController(db, publisherMock.Object);

        // Act
        var result = await controller.GetOrderStatus(order.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
    }
}
