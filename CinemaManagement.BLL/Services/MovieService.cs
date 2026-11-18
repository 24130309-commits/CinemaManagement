using CinemaManagement.BLL.Validators;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.BLL.Services;

public sealed class MovieService : IMovieService
{
    private readonly IMovieRepository _movies;
    private readonly IShowtimeRepository _showtimes;
    private readonly MovieValidator _validator;

    public MovieService(
        IMovieRepository movies,
        IShowtimeRepository showtimes,
        MovieValidator validator)
    {
        _movies = movies;
        _showtimes = showtimes;
        _validator = validator;
    }

    public IEnumerable<Movie> GetAllMovies()
    {
        return _movies.GetAll();
    }

    public Movie GetMovieById(int id)
    {
        return _movies.GetById(id)
            ?? throw new BusinessException("Không tìm thấy phim.");
    }

    public void AddMovie(Movie movie)
    {
        _validator.Validate(movie);

        if (_movies.IsTitleExists(movie.Title))
            throw new BusinessException("Tên phim đã tồn tại.");

        movie.Id = _movies.Insert(movie);

        if (movie.Id <= 0)
            throw new BusinessException("Không thể thêm phim.");
    }

    public void UpdateMovie(Movie movie)
    {
        _validator.Validate(movie);

        if (movie.Id <= 0)
            throw new BusinessException("Mã phim không hợp lệ.");

        if (_movies.IsTitleExists(movie.Title, movie.Id))
            throw new BusinessException("Tên phim đã tồn tại.");

        if (!_movies.Update(movie))
            throw new BusinessException("Không thể cập nhật phim.");
    }

    public IEnumerable<int> GetGenreIds(int movieId)
    {
        return _movies.GetGenreIds(movieId);
    }

    public void SetGenres(int movieId, IEnumerable<int> genreIds)
    {
        if (_movies.GetById(movieId) == null)
            throw new BusinessException("Không tìm thấy phim.");

        var ids = genreIds.Distinct().ToList();

        if (ids.Any(id => id <= 0))
            throw new BusinessException("Thể loại không hợp lệ.");

        _movies.SetGenres(movieId, ids);
    }

    public void DeleteMovie(int id)
    {
        if (id <= 0)
            throw new BusinessException("Mã phim không hợp lệ.");

        if (_showtimes.GetByMovieId(id).Any())
            throw new BusinessException(
                "Không thể xóa phim đã có lịch chiếu trong hệ thống.");

        if (!_movies.Delete(id))
            throw new BusinessException("Không thể xóa phim.");
    }
}