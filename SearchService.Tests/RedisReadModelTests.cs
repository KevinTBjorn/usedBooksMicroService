using Domain;
using Domain.Events;
using FluentAssertions;
using SearchService.Repositories;
using System.Text.Json;
using Xunit;

namespace SearchService.Tests;

public class RedisReadModelTests
{
    /// <summary>
    /// Fake in-memory implementation of ISearchRepository for testing
    /// </summary>
    private class FakeSearchRepository : ISearchRepository
    {
        private readonly Dictionary<string, string> _storage = new();

        public Task<List<SearchService.Models.SearchResult>> SearchAsync(string query)
        {
            var results = new List<SearchService.Models.SearchResult>();

            foreach (var kvp in _storage)
            {
                var book = JsonSerializer.Deserialize<Book>(kvp.Value);
                if (book == null) continue;

                if (string.IsNullOrEmpty(query) ||
                    book.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    book.Author.Contains(query, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(new SearchService.Models.SearchResult
                    {
                        BookId = book.Id,
                        Title = book.Title,
                        Author = book.Author,
                        ISBN = book.Isbn
                    });
                }
            }

            return Task.FromResult(results);
        }

        public Task AddOrUpdateMetadataAsync(Book book)
        {
            var key = $"bookmeta:{book.Id}";
            var payload = JsonSerializer.Serialize(book);
            _storage[key] = payload;
            return Task.CompletedTask;
        }

        public Task UpdateQuantityAsync(Guid bookId, int quantity)
        {
            // UpdateQuantityAsync no longer stores metadata; it's a no-op for metadata-only storage
            return Task.CompletedTask;
        }

        public Task<Book?> GetBookMetadataAsync(Guid bookId)
        {
            var key = $"bookmeta:{bookId}";
            if (_storage.TryGetValue(key, out var json))
            {
                return Task.FromResult(JsonSerializer.Deserialize<Book>(json));
            }
            return Task.FromResult<Book?>(null);
        }

        public string? Get(string key)
        {
            return _storage.TryGetValue(key, out var value) ? value : null;
        }
    }

    [Fact]
    public async Task AddOrUpdateMetadataAsync_StoresMetadataInRedis()
    {
        // Arrange
        var fakeRepo = new FakeSearchRepository();

        var bookId = Guid.NewGuid();
        var book = new Book(
            bookId,
            "Building Microservices",
            "Sam Newman",
            "978-1491950357"
        );

        // Act
        await fakeRepo.AddOrUpdateMetadataAsync(book);

        // Assert
        var stored = fakeRepo.Get($"bookmeta:{bookId}");
        stored.Should().NotBeNull("because AddOrUpdateMetadataAsync should cache book metadata");

        var deserializedBook = JsonSerializer.Deserialize<Book>(stored!);
        deserializedBook.Should().NotBeNull();
        deserializedBook!.Title.Should().Be("Building Microservices");
        deserializedBook.Author.Should().Be("Sam Newman");
    }

    [Fact]
    public async Task SearchAsync_ReturnsMatchingBooks()
    {
        // Arrange
        var fakeRepo = new FakeSearchRepository();
        var book1 = new Book(
            Guid.NewGuid(),
            "Clean Code",
            "Robert C. Martin",
            "978-0132350884"
        );
        var book2 = new Book(
            Guid.NewGuid(),
            "The Pragmatic Programmer",
            "Andy Hunt",
            "978-0135957059"
        );

        await fakeRepo.AddOrUpdateMetadataAsync(book1);
        await fakeRepo.AddOrUpdateMetadataAsync(book2);

        // Act
        var results = await fakeRepo.SearchAsync("Clean");

        // Assert
        results.Should().HaveCount(1);
        results.First().Title.Should().Be("Clean Code");
    }

    [Fact]
    public async Task SearchAsync_ReturnsEmpty_WhenNoMatches()
    {
        // Arrange
        var fakeRepo = new FakeSearchRepository();
        var book = new Book(
            Guid.NewGuid(),
            "Refactoring",
            "Martin Fowler",
            "978-0134757599"
        );
        await fakeRepo.AddOrUpdateMetadataAsync(book);

        // Act
        var results = await fakeRepo.SearchAsync("Nonexistent");

        // Assert
        results.Should().BeEmpty();
    }
}
