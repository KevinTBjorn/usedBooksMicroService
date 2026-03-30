using Domain;
using Domain.Events;
using FluentAssertions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace WarehouseService.Tests;

public class SerializationTests
{
    [Fact]
    public void BookGenre_IsSerializedAsString_WithJsonStringEnumConverter()
    {
        // Arrange
        var book = new
        {
            Title = "Introduction to Algorithms",
            Genre = GenreEnum.BookGenre.Mathematics
        };

        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonStringEnumConverter());

        // Act
        var json = JsonSerializer.Serialize(book, options);

        // Assert
        json.Should().Contain("\"Mathematics\"", "because enum should serialize as string, not number");
        json.Should().NotContain("\"Genre\":1", "because numeric enum serialization breaks cross-service compatibility");
    }

    [Fact]
    public void BookAddedEvent_CanBeDeserialized_WithEnumAsString()
    {
        // Arrange
        var json = """
        {
            "BookId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
            "UserId": "11111111-2222-3333-4444-555555555555",
            "Title": "Clean Architecture",
            "ISBN": "978-0134494166",
            "Description": "Software structure",
            "Edition": "1st",
            "Year": 2017,
            "Author": "Robert C. Martin",
            "ImageUrl": "http://example.com/clean.jpg",
            "Genre": "ComputerScience",
            "Condition": "New",
            "Price": 49.99,
            "InitialStock": 10
        }
        """;

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        options.Converters.Add(new JsonStringEnumConverter());

        // Act
        var evt = JsonSerializer.Deserialize<BookAddedEvent>(json, options);

        // Assert
        evt.Should().NotBeNull();
        evt!.Genre.Should().Be(GenreEnum.BookGenre.ComputerScience);
        evt.Title.Should().Be("Clean Architecture");
    }

    [Fact]
    public void StockUpdatedEvent_IncludesMetadata_WithCorrectEnumSerialization()
    {
        // Arrange
        var evt = new StockUpdatedEvent
        {
            BookId = Guid.NewGuid(),
            Stock = 25,
            OccurredAt = DateTime.UtcNow,
            Title = "Design Patterns",
            ISBN = "978-0201633610",
            Description = "Elements of Reusable OO Software",
            Edition = "1st",
            Year = 1994,
            Author = "Gang of Four",
            ImageUrl = "http://example.com/gof.jpg",
            Genre = GenreEnum.BookGenre.ComputerScience
        };

        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonStringEnumConverter());

        // Act
        var json = JsonSerializer.Serialize(evt, options);
        var deserialized = JsonSerializer.Deserialize<StockUpdatedEvent>(json, options);

        // Assert
        json.Should().Contain("\"ComputerScience\"");
        deserialized.Should().NotBeNull();
        deserialized!.Genre.Should().Be(GenreEnum.BookGenre.ComputerScience);
    }

    [Theory]
    [InlineData(GenreEnum.BookGenre.Mathematics, "Mathematics")]
    [InlineData(GenreEnum.BookGenre.Physics, "Physics")]
    [InlineData(GenreEnum.BookGenre.FictionFantasy, "FictionFantasy")]
    [InlineData(GenreEnum.BookGenre.Business, "Business")]
    public void AllGenres_SerializeAsExpectedStrings(GenreEnum.BookGenre genre, string expectedString)
    {
        // Arrange
        var obj = new { Genre = genre };
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonStringEnumConverter());

        // Act
        var json = JsonSerializer.Serialize(obj, options);

        // Assert
        json.Should().Contain($"\"{expectedString}\"");
    }
}
