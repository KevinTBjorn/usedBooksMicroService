using Domain;
using FluentAssertions;
using Xunit;

namespace WarehouseService.Tests;

public class DomainModelTests
{
    [Fact]
    public void Book_Constructor_InitializesPropertiesCorrectly()
    {
        // Arrange & Act
        var bookId = Guid.NewGuid();
        var book = new Book(
            bookId,
            "The Phoenix Project",
            "978-0988262508",
            "A Novel About IT, DevOps, and Helping Your Business Win",
            "1st",
            2013,
            "Gene Kim",
            "http://example.com/phoenix.jpg",
            GenreEnum.BookGenre.Business
        );

        // Assert
        book.Id.Should().Be(bookId);
        book.Title.Should().Be("The Phoenix Project");
        book.Isbn.Should().Be("978-0988262508");
        book.Author.Should().Be("Gene Kim");
        book.Genre.Should().Be(GenreEnum.BookGenre.Business);
    }

    [Fact]
    public void Book_SetImageUrl_UpdatesImageUrlProperty()
    {
        // Arrange
        var book = new Book(
            Guid.NewGuid(),
            "Test Book",
            "123-456",
            "Test Description",
            "1st",
            2024,
            "Test Author",
            "",
            GenreEnum.BookGenre.Other
        );

        // Act
        book.setImageUrl("http://newimage.com/cover.jpg");

        // Assert
        book.ImageUrl.Should().Be("http://newimage.com/cover.jpg");
    }

    [Theory]
    [InlineData("Mathematics", GenreEnum.BookGenre.Mathematics)]
    [InlineData("FictionFantasy", GenreEnum.BookGenre.FictionFantasy)]
    [InlineData("Business", GenreEnum.BookGenre.Business)]
    [InlineData("Physics", GenreEnum.BookGenre.Physics)]
    public void BookGenre_AllEnumValues_AreParseable(string genreName, GenreEnum.BookGenre expectedGenre)
    {
        // Act
        var canParse = Enum.TryParse<GenreEnum.BookGenre>(genreName, out var result);

        // Assert
        canParse.Should().BeTrue($"because {genreName} should be a valid BookGenre");
        result.Should().Be(expectedGenre, $"because {genreName} should parse to {expectedGenre}");
    }

    [Fact]
    public void UserBook_Constructor_InitializesPropertiesCorrectly()
    {
        // Arrange & Act
        var bookId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var userBook = new WarehouseService.Models.UserBook(
            bookId,
            userId,
            "Good",
            5,
            29.99m
        );

        // Assert
        userBook.BookId.Should().Be(bookId);
        userBook.UserId.Should().Be(userId);
        userBook.Condition.Should().Be("Good");
        userBook.Quantity.Should().Be(5);
        userBook.Price.Should().Be(29.99m);
    }
}
