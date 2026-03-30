using StackExchange.Redis;
using System.Text.Json;
using Domain;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using SearchService.Models;

namespace SearchService.Repositories
{
    public interface ISearchRepository
    {
        Task<List<SearchService.Models.SearchResult>> SearchAsync(string query);
        Task AddOrUpdateMetadataAsync(Book book);
        Task UpdateQuantityAsync(Guid bookId, int quantity);
        Task<Book?> GetBookMetadataAsync(Guid bookId);
    }

    public class SearchRepository : ISearchRepository
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _db;

        public SearchRepository(IConnectionMultiplexer redis)
        {
            Console.WriteLine("[REDIS-REPO] SearchRepository constructed");

            _redis = redis;            // store multiplexer
            _db = redis.GetDatabase(); // get DB
        }

        public async Task<List<SearchService.Models.SearchResult>> SearchAsync(string query)
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var keys = server.Keys(pattern: "bookmeta:*").ToArray();
            
            if (keys.Length == 0)
            {
                return new List<SearchService.Models.SearchResult>();
            }

            // Batch fetch all values in parallel using StringGetAsync with key array
            var values = await _db.StringGetAsync(keys);
            
            var results = new List<SearchService.Models.SearchResult>();

            for (int i = 0; i < keys.Length; i++)
            {
                var val = values[i];
                if (val.IsNullOrEmpty) continue;

                var book = JsonSerializer.Deserialize<Book>(val);
                if (book == null) continue;

                if (string.IsNullOrEmpty(query)
                    || book.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || book.Author.Contains(query, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(new SearchService.Models.SearchResult
                    {
                        BookId = book.Id,
                        Title = book.Title,
                        Author = book.Author,
                        ISBN = book.Isbn,
                    });
                }
            }

            return results;
        }

        public async Task AddOrUpdateMetadataAsync(Book book)
        {
            var key = $"bookmeta:{book.Id}";
            var payload = JsonSerializer.Serialize(book);

            await _db.StringSetAsync(key, payload);

            Console.WriteLine($"[REDIS] Stored metadata: {key}");
        }

        public async Task UpdateQuantityAsync(Guid bookId, int quantity)
        {
            // Not stored here ─ but required for interface compatibility.
            await Task.CompletedTask;
        }

        public async Task<Book?> GetBookMetadataAsync(Guid bookId)
        {
            var key = $"bookmeta:{bookId}";
            var val = await _db.StringGetAsync(key);

            if (val.IsNullOrEmpty)
                return null;

            return JsonSerializer.Deserialize<Book>(val);
        }
    }
}
