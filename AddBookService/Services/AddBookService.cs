using AddBookService.Models;
using Domain;
using Domain.Events;
using MassTransit;
using Microsoft.AspNetCore.Components.Forms;
using System.Net;
using System.Net.Http;
using System.Text.Json;

namespace AddBookService.Services
{
    public class AddBookService : IAddBookService
    {
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IGenreService _genreService;
        private readonly HttpClient _httpClient;

        public AddBookService(
            IPublishEndpoint publishEndpoint,
            IGenreService genreService,
            HttpClient httpClient)
        {
            _publishEndpoint = publishEndpoint;
            _genreService = genreService;
            _httpClient = httpClient;
        }

        // ============================================================
        // SHARED LOGIC: Build a Book from ISBN (OpenLibrary + AI genre)
        // ============================================================
        private async Task<Book> BuildBookFromIsbnAsync(string isbn, Guid id)
        {
            // Fetch base book data from OpenLibrary
            var rawBook = await GetOpenLibraryIsbnData(isbn)
                ?? throw new Exception("Book not found in Open Library");

            // Fetch author names
            var authorNames = await FetchAuthorNames(rawBook.authors);

            // Fetch description from work (if any)
            string description = "";
            if (rawBook.works != null && rawBook.works.Count > 0)
            {
                var workKey = rawBook.works[0].key;
                description = await FetchDescription(workKey);
            }

            // Map OpenLibrary data to your internal Book model
            var book = new Domain.Book(
                        id: id,
                        title: rawBook.title,
                        isbn: isbn,
                        description: description,
                        edition: "1",
                        year: int.TryParse(rawBook.publish_date?.Split(' ').Last(), out var year) ? year : 0,
                        author: string.Join(", ", authorNames),
                        imageUrl: "",
                        genre: GenreEnum.BookGenre.Other
 );

            // Cover image (if any)
            string imageurl = BuildImageUrl(rawBook.covers);
            book.setImageUrl(imageurl);

            // Detect genre via your genre service (with fallback to Other)
            try
            {
                var detectedGenre = await _genreService.DetectGenreAsync(
                    book.Title,
                    book.Description,
                    null
                );
                book.Genre = detectedGenre;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AddBookService] Genre detection failed → {ex.Message}");
                Console.WriteLine($"[AddBookService] Using default genre: Other");
                book.Genre = GenreEnum.BookGenre.Other;
            }

            return book;
        }

        // ============================================================
        // STEP 1: PREVIEW (no events)
        // ============================================================
        public async Task<Book> PreviewBookAsync(string isbn)
        {
            // Only build and return the book – NO publishing to RabbitMQ
            return await BuildBookFromIsbnAsync(isbn, Guid.Empty);
        }

        // ============================================================
        // STEP 2: CREATE (with event)
        // ============================================================
        public async Task<Book> AddBookAsync(AddBookRequest bookrequest)
        {
            Guid id = Guid.NewGuid();
            // Build the enriched book (same as preview)
            var book = await BuildBookFromIsbnAsync(bookrequest.Isbn, id);

            // TODO later:
            // - store Condition, Price, Quantity in Listings table
            //   (right now they are only in AddBookRequest)

            // Publish an event to RabbitMQ
            var evt = new Domain.Events.BookAddedEvent
            {
                BookId = id,
                UserId = bookrequest.UserId,

                Title = book.Title,
                ISBN = book.Isbn,
                Description = book.Description,
                Edition = book.Edition,
                Year = book.Year,
                Author = book.Author,
                ImageUrl = book.ImageUrl,
                Genre = book.Genre,

                Condition = bookrequest.Condition,
                Price = (decimal)bookrequest.Price,
                InitialStock = bookrequest.Quantity
            };

            //await _publishEndpoint.Publish(evt, ctx =>
            //{
            //    ctx.SetRoutingKey("book.added.event");
            //});
            await _publishEndpoint.Publish(evt);


            return book;
        }

        // ============================================================
        // OPENLIBRARY FETCH + MAPPING  (unchanged from your version)
        // ============================================================

        private async Task<OpenLibraryIsbnResponse?> GetOpenLibraryIsbnData(string isbn)
        {
            try
            {
                var url = $"https://openlibrary.org/isbn/{isbn}.json";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[AddBookService] OpenLibrary returned {response.StatusCode} for ISBN {isbn}");
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<OpenLibraryIsbnResponse>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AddBookService] Error fetching ISBN data: {ex.Message}");
                return null;
            }
        }

        private async Task<List<string>> FetchAuthorNames(List<OpenLibraryAuthorRef> authorRefs)
        {
            var names = new List<string>();

            if (authorRefs == null)
                return names;

            foreach (var a in authorRefs)
            {
                try
                {
                    var url = $"https://openlibrary.org{a.key}.json";
                    var response = await _httpClient.GetAsync(url);

                    if (!response.IsSuccessStatusCode)
                        continue;

                    var json = await response.Content.ReadAsStringAsync();
                    var authorObj = JsonSerializer.Deserialize<OpenLibraryAuthorResponse>(json);

                    if (authorObj?.name != null)
                        names.Add(authorObj.name);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AddBookService] Error fetching author: {ex.Message}");
                    continue;
                }
            }

            return names;
        }

        private async Task<string> FetchDescription(string workKey)
        {
            try
            {
                var url = $"https://openlibrary.org{workKey}.json";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return "";

                var json = await response.Content.ReadAsStringAsync();
                var workObj = JsonSerializer.Deserialize<OpenLibraryWorkResponse>(json);

                if (workObj?.description == null)
                    return "";

                // description kan være string eller et objekt
                if (workObj.description is JsonElement element)
                {
                    if (element.ValueKind == JsonValueKind.String)
                        return element.GetString() ?? "";

                    if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty("value", out var valueProp))
                        return valueProp.GetString() ?? "";
                }

                return "";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AddBookService] Error fetching description: {ex.Message}");
                return "";
            }
        }

        private string BuildImageUrl(List<int> covers)
        {
            if (covers == null || covers.Count == 0)
                return "";

            int coverId = covers[0];    // Tag første cover-id
            return $"https://covers.openlibrary.org/b/id/{coverId}-L.jpg";
        }
    }

    // =====================================================================
    // DTO CLASSES (unchanged)
    // =====================================================================

    public class OpenLibraryIsbnResponse
    {
        public string title { get; set; }
        public List<OpenLibraryAuthorRef> authors { get; set; }
        public string publish_date { get; set; }
        public List<OpenLibraryWorkRef> works { get; set; }
        public List<int> covers { get; set; }
    }

    public class OpenLibraryWorkRef
    {
        public string key { get; set; } // "/works/OL82563W"
    }

    public class OpenLibraryWorkResponse
    {
        public object description { get; set; } // kan være string eller { value = "" }
    }

    public class OpenLibraryAuthorRef
    {
        public string key { get; set; }  // "/authors/OL23919A"
    }

    public class OpenLibraryAuthorResponse
    {
        public string name { get; set; }
    }

}
