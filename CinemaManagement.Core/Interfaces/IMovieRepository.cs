using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;

namespace CinemaManagement.Core.Interfaces;

public interface IMovieRepository : IRepository<Movie>
{
    IEnumerable<Movie> GetMoviesByStatus(MovieStatus status);

    IEnumerable<Movie> Search(
        string keyword,
        int? genreId = null,
        MovieStatus? status = null
    );

    bool IsTitleExists(string title, int? excludeId = null);

    // Lấy danh sách mã thể loại của một phim
    IEnumerable<int> GetGenreIds(int movieId);

    // Cập nhật danh sách thể loại cho một phim
    void SetGenres(int movieId, IEnumerable<int> genreIds);
}