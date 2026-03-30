using System.Text.Json.Serialization;

namespace Domain {
    public class Book {
        public Guid Id { get; private set; }
        public string Title { get; private set; }
        public string Isbn { get; private set; }
        public string Description { get; private set; } = "";
        public string Edition { get; private set; }
        public int Year { get; private set; }
        public string Author { get; private set; }
        public string ImageUrl { get; private set; } = "";
        public GenreEnum.BookGenre Genre { get; set; }

        // parameterless constructor for EF
        private Book() { }

        [JsonConstructor]
        public Book(Guid id, string title, string isbn, string description, string edition, int year, string author, string imageUrl, GenreEnum.BookGenre genre)
        {
            Id = id;
            Title = title;
            Isbn = isbn;
            Description = description;
            Edition = edition;
            Year = year;
            Author = author;
            ImageUrl = imageUrl;
            Genre = genre;
        }

         public Book(Guid id, string title, string author, string isbn)
        {
            Id = id;
            Title = title;
            Author = author;
            Isbn = isbn;
        }

        public void setImageUrl (string imageUrl)
        {
            ImageUrl = imageUrl;
        }
    }
}