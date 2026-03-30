using Domain;
using SearchService.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using SearchService.Models;

namespace SearchService {
    public class SearchServiceImpl : ISearchService {
        private readonly ISearchRepository _repo;

        public SearchServiceImpl(ISearchRepository repo) {
            _repo = repo;
        }

        // Implement Domain.ISearchService signature explicitly using Domain.SearchResult
        public async Task<List<Domain.SearchResult>> SearchAsync(SearchQuery query) {
            var q = query?.Title ?? string.Empty;
            var metadataResults = await _repo.SearchAsync(q);

            // Map metadata-only results to Domain.SearchResult (Stock left default)
            var domainResults = new List<Domain.SearchResult>();
            foreach (var m in metadataResults)
            {
                domainResults.Add(new Domain.SearchResult
                {
                    BookId = m.BookId,
                    Title = m.Title,
                    Author = m.Author,
                    // Stock intentionally omitted (default 0) — SearchService does not provide stock
                });
            }

            return domainResults;
        }

        public async Task HandleStockUpdatedAsync(Domain.Events.StockUpdatedEvent evt) {
            await _repo.UpdateQuantityAsync(evt.BookId, evt.Stock);
        }
    }
}