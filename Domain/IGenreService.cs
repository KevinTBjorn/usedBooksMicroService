using System.Threading.Tasks;

namespace Domain;

public interface IGenreService
{
    Task<GenreEnum.BookGenre> DetectGenreAsync(
        string title,
        string description,
        string? courseCode);
}