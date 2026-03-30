using System;
using static Domain.GenreEnum;

namespace Domain.Events
{
    /// <summary>
    /// Event emitted by WarehouseService when book stock changes
    /// Contains full book metadata so SearchService can index it
    /// </summary>
    public class StockUpdatedEvent
    {
        public Guid BookId { get; set; }
        public int Stock { get; set; }
        public DateTime? OccurredAt { get; set; }

        // Book metadata for SearchService indexing
        public string Title { get; set; } = string.Empty;
        public string ISBN { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Edition { get; set; } = string.Empty;
        public int Year { get; set; }
        public string Author { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public BookGenre Genre { get; set; }
    }
}
