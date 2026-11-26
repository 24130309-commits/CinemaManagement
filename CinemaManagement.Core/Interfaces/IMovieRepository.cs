using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;

namespace CinemaManagement.Core.Interfaces;

public interface IMovieRepository : IRepository<Movie>
{
    IEnumerable<Movie> GetMoviesByStatus(MovieStatus status);

    IEnumerable<Movie> Search(
        string keyword,
        int? genreId = null,
        MovieStatus? status = null);

    bool IsTitleExists(string title, int? excludeId = null);

    IEnumerable<int> GetGenreIds(int movieId);

    void SetGenres(int movieId, IEnumerable<int> genreIds);

    int SaveWithGenres(Movie movie, IEnumerable<int>? genreIds)
    {
        ArgumentNullException.ThrowIfNull(movie);

        var id = movie.Id <= 0
            ? Insert(movie)
            : Update(movie) ? movie.Id : 0;

        if (id <= 0)
            return 0;

        if (genreIds is not null)
            SetGenres(id, genreIds);

        movie.Id = id;
        return id;
    }
}