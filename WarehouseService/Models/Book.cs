using Domain;

namespace WarehouseService.Models
{
    public class Book
    {
        public Guid Id { get; private set; }
        public string Title { get; private set; }
        public string ISBN { get; private set; }
        public string Description { get; private set; }
        public string Edition { get; private set; }
        public int Year { get; private set; }
        public string Author { get; private set; }
        public string ImageUrl { get; private set; }
        public GenreEnum.BookGenre Genre { get; private set; }

        public ICollection<UserBook> UserBooks { get; private set; } = new List<UserBook>();

        private Book() { }

        public Book(Guid id, string title, string isbn, string description, string edition, int year, string author, string imageUrl, GenreEnum.BookGenre genre)
        {
            Id = id;
            Title = title;
            ISBN = isbn;
            Description = description;
            Edition = edition;
            Year = year;
            Author = author;
            ImageUrl = imageUrl;
            Genre = genre;
        }
    }
}
