using System;

namespace SearchService.Models
{
    public class SearchResult
    {
        public Guid BookId { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string ISBN { get; set; }
    }
}
