using Domain.Events;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WarehouseService.Data;
using WarehouseService.Messaging;
using WarehouseService.Models;
using Xunit;

namespace WarehouseService.Tests;

public class OrderValidationTests
{
    private WarehouseDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<WarehouseDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new WarehouseDbContext(options);
    }

    [Fact]
    public async Task ValidateOrder_ReturnsValid_WhenStockIsSufficient()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var mockPublisher = new Moq.Mock<IEventPublisher>();
        var service = new WarehouseServiceImpl(dbContext, mockPublisher.Object);

        var bookId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Seed database with book and stock
        var book = new Book(
            bookId,
            "Microservices Patterns",
            "978-1617294549",
            "Building scalable systems",
            "1st",
            2018,
            "Chris Richardson",
            "http://example.com/micro.jpg",
            Domain.GenreEnum.BookGenre.ComputerScience
        );
        dbContext.Books.Add(book);

        var userBook = new UserBook(bookId, userId, "New", 20, 59.99m);
        dbContext.UserBooks.Add(userBook);
        await dbContext.SaveChangesAsync();

        var request = new OrderValidationRequestMessage
        {
            OrderId = Guid.NewGuid(),
            UserId = userId,
            CorrelationId = Guid.NewGuid(),
            Items = new List<OrderItem>
            {
                new OrderItem { BookId = bookId, Quantity = 5 }
            }
        };

        // Act
        var result = await service.ValidateOrderAsync(request);

        // Assert
        result.IsValid.Should().BeTrue("because stock (20) is sufficient for order quantity (5)");
        result.Reasons.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateOrder_ReturnsRejected_WhenStockIsInsufficient()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var mockPublisher = new Moq.Mock<IEventPublisher>();
        var service = new WarehouseServiceImpl(dbContext, mockPublisher.Object);

        var bookId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Seed with insufficient stock
        var book = new Book(
            bookId,
            "Domain-Driven Design",
            "978-0321125217",
            "Tackling complexity",
            "1st",
            2003,
            "Eric Evans",
            "http://example.com/ddd.jpg",
            Domain.GenreEnum.BookGenre.ComputerScience
        );
        dbContext.Books.Add(book);

        var userBook = new UserBook(bookId, userId, "Used", 3, 49.99m);
        dbContext.UserBooks.Add(userBook);
        await dbContext.SaveChangesAsync();

        var request = new OrderValidationRequestMessage
        {
            OrderId = Guid.NewGuid(),
            UserId = userId,
            CorrelationId = Guid.NewGuid(),
            Items = new List<OrderItem>
            {
                new OrderItem { BookId = bookId, Quantity = 10 }
            }
        };

        // Act
        var result = await service.ValidateOrderAsync(request);

        // Assert
        result.IsValid.Should().BeFalse("because stock (3) is insufficient for order quantity (10)");
        result.Reasons.Should().Contain(r => r.Contains("out of stock"));
    }

    [Fact]
    public async Task ValidateOrder_ReturnsRejected_WhenBookNotInUserInventory()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var mockPublisher = new Moq.Mock<IEventPublisher>();
        var service = new WarehouseServiceImpl(dbContext, mockPublisher.Object);

        var bookId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var request = new OrderValidationRequestMessage
        {
            OrderId = Guid.NewGuid(),
            UserId = userId,
            CorrelationId = Guid.NewGuid(),
            Items = new List<OrderItem>
            {
                new OrderItem { BookId = bookId, Quantity = 1 }
            }
        };

        // Act
        var result = await service.ValidateOrderAsync(request);

        // Assert
        result.IsValid.Should().BeFalse("because book is not in user's inventory");
        result.Reasons.Should().Contain(r => r.Contains("not found in user inventory"));
    }
}
