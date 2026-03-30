using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using StackExchange.Redis;
using System.Text.Json;
using Domain;
using SearchService.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VSDiagnostics;
using SearchResult = SearchService.Models.SearchResult;

namespace SearchService.Benchmarks
{
    [MemoryDiagnoser]
    [CPUUsageDiagnoser]
    public class SearchRepositoryBenchmark
    {
        private IConnectionMultiplexer _redis;
        private ISearchRepository _repository;
        private const string TestQuery = "Potter";
        private readonly List<RedisKey> _testKeys = new List<RedisKey>();

        [Params(100, 1000, 10000)]
        public int BookCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            // Connect to Redis
            var options = ConfigurationOptions.Parse("localhost:6379");
            options.AbortOnConnectFail = false;
            _redis = ConnectionMultiplexer.Connect(options);
            _repository = new SearchRepository(_redis);
            
            // Seed Redis with test data
            var db = _redis.GetDatabase();
            for (int i = 0; i < BookCount; i++)
            {
                var bookId = Guid.NewGuid();
                var title = i % 10 == 0 ? $"Harry Potter {i}" : $"Book Title {i}";
                var author = $"Author {i}";
                var isbn = $"ISBN-{i}";
                
                // Use 4-argument constructor then serialize the book
                var book = new Book(bookId, title, author, isbn);
                
                var key = $"bookmeta:{bookId}";
                _testKeys.Add(key);  // Track keys we create
                var payload = JsonSerializer.Serialize(book);
                db.StringSet(key, payload);
            }

            Console.WriteLine($"[BENCHMARK] Seeded Redis with {_testKeys.Count} test books");
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            // Clean up ONLY the test data we created
            var db = _redis.GetDatabase();
            foreach (var key in _testKeys)
            {
                db.KeyDelete(key);
            }

            _redis?.Dispose();
            Console.WriteLine($"[BENCHMARK] Cleaned up {_testKeys.Count} test keys");
        }

        /// <summary>
        /// CRITICAL: This benchmark demonstrates that SearchRepository.SearchAsync() 
        /// uses Redis KEYS command, which has O(N) complexity.
        /// 
        /// Expected results:
        /// - 100 books: ~5-10ms
        /// - 1,000 books: ~50-100ms
        /// - 10,000 books: ~500-1000ms
        /// 
        /// This proves that search performance degrades linearly with dataset size,
        /// violating the requirement: "regardless of how many books there are in the system".
        /// 
        /// Production solution: Replace KEYS with SCAN or use RediSearch module.
        /// </summary>
        [Benchmark]
        public async Task<List<SearchResult>> SearchAsync_Current()
        {
            return await _repository.SearchAsync(TestQuery);
        }
    }
}