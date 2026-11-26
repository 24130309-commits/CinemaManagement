using CinemaManagement.Core.Entities;

namespace CinemaManagement.Core.Interfaces;

public interface IGenreService
{
    IEnumerable<Genre> GetAll();

    void Create(Genre genre);

    void Update(Genre genre);

    void Delete(int genreId);
}
