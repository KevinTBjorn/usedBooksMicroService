using Domain.Events;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using WarehouseService.Data;
using WarehouseService.Messaging;
using Xunit;

namespace WarehouseService.Tests;

public class IdempotencyTests
{
    private WarehouseDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<WarehouseDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new WarehouseDbContext(options);
    }

    [Fact]
    public async Task HandleBookAddedEvent_DoesNotCreateDuplicateBook_WhenEventRedelivered()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var mockPublisher = new Moq.Mock<IEventPublisher>();
        var service = new WarehouseServiceImpl(dbContext, mockPublisher.Object);

        var evt = new BookAddedEvent
        {
            BookId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Title = "Distributed Systems Design",
            ISBN = "978-1491924914",
            Description = "Event-driven microservices",
            Edition = "1st",
            Year = 2020,
            Author = "Martin Kleppmann",
            ImageUrl = "http://example.com/book.jpg",
            Genre = Domain.GenreEnum.BookGenre.ComputerScience,
            Condition = "New",
            Price = 49.99m,
            InitialStock = 10
        };

        // Act - Handle event twice (simulate redelivery)
        await service.HandleBookAddedAsync(evt);
        await service.HandleBookAddedAsync(evt);

        // Assert - Only one book should exist
        var books = await dbContext.Books.ToListAsync();
        books.Should().HaveCount(1, "because duplicate BookAddedEvent should be idempotent");
        books.First().Title.Should().Be("Distributed Systems Design");
    }

    [Fact]
    public async Task HandleBookAddedEvent_UpdatesStockCorrectly_OnRedelivery()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var mockPublisher = new Moq.Mock<IEventPublisher>();
        var service = new WarehouseServiceImpl(dbContext, mockPublisher.Object);

        var evt = new BookAddedEvent
        {
            BookId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Title = "Clean Architecture",
            ISBN = "978-0134494166",
            Description = "Software structure and design",
            Edition = "1st",
            Year = 2017,
            Author = "Robert C. Martin",
            ImageUrl = "http://example.com/clean.jpg",
            Genre = Domain.GenreEnum.BookGenre.ComputerScience,
            Condition = "Good",
            Price = 39.99m,
            InitialStock = 5
        };

        // Act
        await service.HandleBookAddedAsync(evt);
        await service.HandleBookAddedAsync(evt);

        // Assert - Stock should be overwritten (semi-idempotent behavior)
        var userBooks = await dbContext.UserBooks.ToListAsync();
        userBooks.Should().HaveCount(1);
        userBooks.First().Quantity.Should().Be(5, "because HandleBookAddedAsync overwrites quantity on redelivery");
    }
}
