using AddBookService.Models;
using Domain;

namespace AddBookService.Services
{
    public interface IAddBookService
    {
        Task<Book> PreviewBookAsync(string isbn);
        
        Task<Book> AddBookAsync(AddBookRequest bookrequest);
    }
}