using Domain.Events;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using WarehouseService.Data;
using WarehouseService.Messaging;
using WarehouseService.Models;
using Xunit;

namespace WarehouseService.Tests;

public class EventPublishingTests
{
    private WarehouseDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<WarehouseDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new WarehouseDbContext(options);
    }

    [Fact]
    public async Task HandleBookAddedAsync_PublishesStockUpdatedEvent_WithCorrectTotalQuantity()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var mockPublisher = new Mock<IEventPublisher>();
        var service = new WarehouseServiceImpl(dbContext, mockPublisher.Object);

        var bookId = Guid.NewGuid();
        var evt = new BookAddedEvent
        {
            BookId = bookId,
            UserId = Guid.NewGuid(),
            Title = "Enterprise Integration Patterns",
            ISBN = "978-0321200686",
            Description = "Designing messaging solutions",
            Edition = "1st",
            Year = 2003,
            Author = "Gregor Hohpe",
            ImageUrl = "http://example.com/eip.jpg",
            Genre = Domain.GenreEnum.BookGenre.ComputerScience,
            Condition = "New",
            Price = 59.99m,
            InitialStock = 15
        };

        // Act
        await service.HandleBookAddedAsync(evt);

        // Assert
        mockPublisher.Verify(
            p => p.PublishStockUpdatedAsync(It.Is<StockUpdatedEvent>(
                e => e.BookId == bookId && e.Stock == 15
            )),
            Times.Once,
            "because HandleBookAddedAsync should publish StockUpdatedEvent with total stock"
        );
    }

    [Fact]
    public async Task HandleBookAddedAsync_PublishesStockUpdatedEvent_WithFullMetadata()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var mockPublisher = new Mock<IEventPublisher>();
        var service = new WarehouseServiceImpl(dbContext, mockPublisher.Object);

        var bookId = Guid.NewGuid();
        var evt = new BookAddedEvent
        {
            BookId = bookId,
            UserId = Guid.NewGuid(),
            Title = "Site Reliability Engineering",
            ISBN = "978-1491929124",
            Description = "How Google runs production systems",
            Edition = "1st",
            Year = 2016,
            Author = "Betsy Beyer",
            ImageUrl = "http://example.com/sre.jpg",
            Genre = Domain.GenreEnum.BookGenre.ComputerScience,
            Condition = "New",
            Price = 49.99m,
            InitialStock = 8
        };

        // Act
        await service.HandleBookAddedAsync(evt);

        // Assert
        mockPublisher.Verify(
            p => p.PublishStockUpdatedAsync(It.Is<StockUpdatedEvent>(
                e => e.Title == "Site Reliability Engineering" &&
                     e.Author == "Betsy Beyer" &&
                     e.Genre == Domain.GenreEnum.BookGenre.ComputerScience &&
                     e.ISBN == "978-1491929124"
            )),
            Times.Once,
            "because StockUpdatedEvent should include full book metadata for SearchService"
        );
    }

    [Fact]
    public async Task HandleBookAddedAsync_CalculatesCorrectTotalStock_WhenMultipleUsersHaveSameBook()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var mockPublisher = new Mock<IEventPublisher>();
        var service = new WarehouseServiceImpl(dbContext, mockPublisher.Object);

        var bookId = Guid.NewGuid();
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();

        // Seed book and first user's stock
        var book = new Book(
            bookId,
            "Kubernetes in Action",
            "978-1617293726",
            "Container orchestration",
            "2nd",
            2020,
            "Marko Luksa",
            "http://example.com/k8s.jpg",
            Domain.GenreEnum.BookGenre.ComputerScience
        );
        dbContext.Books.Add(book);
        dbContext.UserBooks.Add(new UserBook(bookId, user1, "New", 10, 59.99m));
        await dbContext.SaveChangesAsync();

        // Clear previous invocations
        mockPublisher.Invocations.Clear();

        // Act - Add stock from second user
        var evt = new BookAddedEvent
        {
            BookId = bookId,
            UserId = user2,
            Title = "Kubernetes in Action",
            ISBN = "978-1617293726",
            Description = "Container orchestration",
            Edition = "2nd",
            Year = 2020,
            Author = "Marko Luksa",
            ImageUrl = "http://example.com/k8s.jpg",
            Genre = Domain.GenreEnum.BookGenre.ComputerScience,
            Condition = "Used",
            Price = 45.00m,
            InitialStock = 7
        };

        await service.HandleBookAddedAsync(evt);

        // Assert - Total stock should be 10 + 7 = 17
        mockPublisher.Verify(
            p => p.PublishStockUpdatedAsync(It.Is<StockUpdatedEvent>(
                e => e.BookId == bookId && e.Stock == 17
            )),
            Times.Once,
            "because total stock should aggregate across all users"
        );
    }
}
