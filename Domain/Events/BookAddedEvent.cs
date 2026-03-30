using System;
using static Domain.GenreEnum;

namespace Domain.Events
{
    public class BookAddedEvent
    {
        public Guid BookId { get; set; }
        public Guid UserId { get; set; }

        public string Title { get; set; }
        public string ISBN { get; set; }
        public string Description { get; set; }
        public string Edition { get; set; }
        public int Year { get; set; }
        public string Author { get; set; }
        public string ImageUrl { get; set; }
        public BookGenre Genre { get; set; }
        public string Condition { get; set; }
        public decimal Price { get; set; }

        public int InitialStock { get; set; }
    }
}
