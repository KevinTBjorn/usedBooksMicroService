using System;

namespace Domain.Dto
{
    public class BookWithStockDto
    {
        public Guid BookId { get; set; }
        public string Title { get; set; }
        public string ISBN { get; set; }
        public string Description { get; set; }
        public string Edition { get; set; }
        public int Year { get; set; }
        public string Author { get; set; }
        public string ImageUrl { get; set; }
        public GenreEnum.BookGenre Genre { get; set; }

        public int TotalStock { get; set; }
    }
}
