using System.Threading.Tasks;
using System.Collections.Generic;
using Domain.Events;
using System;

namespace Domain {
    public interface ISearchService {
        Task<List<SearchResult>> SearchAsync(SearchQuery query);
        Task HandleStockUpdatedAsync(StockUpdatedEvent evt);
    }

    public class SearchQuery {
        public string Title { get; set; }
        public string Author { get; set; }
    }

    public class SearchResult {
        public Guid BookId { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public int Stock { get; set; }
    }
}