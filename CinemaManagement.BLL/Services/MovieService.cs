
using CinemaManagement.BLL.Validators;
using CinemaManagement.BLL.Session;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.BLL.Services;

public sealed class MovieService : IMovieService
{
    private readonly IMovieRepository _movies;
    private readonly IShowtimeRepository _showtimes;
    private readonly MovieValidator _validator;
    private readonly CurrentUserSession _session;

    public MovieService(
        IMovieRepository movies,
        IShowtimeRepository showtimes,
        MovieValidator validator,
        CurrentUserSession session)
    {
        _movies = movies
            ?? throw new ArgumentNullException(nameof(movies));

        _showtimes = showtimes
            ?? throw new ArgumentNullException(nameof(showtimes));

        _validator = validator
            ?? throw new ArgumentNullException(nameof(validator));
        _session = session
            ?? throw new ArgumentNullException(nameof(session));
    }

    public IEnumerable<Movie> GetAllMovies()
    {
        return _movies.GetAll();
    }

    public Movie GetMovieById(int id)
    {
        if (id <= 0)
            throw new BusinessException("Mã phim không hợp lệ.");

        return _movies.GetById(id)
            ?? throw new BusinessException("Không tìm thấy phim.");
    }

    public void AddMovie(Movie movie)
    {
        AuthorizationGuard.EnsureAuthenticated(_session);
        _validator.Validate(movie);

        movie.Title = movie.Title.Trim();

        if (_movies.IsTitleExists(movie.Title))
            throw new BusinessException("Tên phim đã tồn tại.");

        var id = _movies.Insert(movie);

        if (id <= 0)
            throw new BusinessException("Không thể thêm phim.");

        movie.Id = id;
    }

    public void UpdateMovie(Movie movie)
    {
        AuthorizationGuard.EnsureAuthenticated(_session);
        _validator.Validate(movie);

        if (movie.Id <= 0)
            throw new BusinessException("Mã phim không hợp lệ.");

        movie.Title = movie.Title.Trim();

        var existing = _movies.GetById(movie.Id)
            ?? throw new BusinessException("Không tìm thấy phim.");

        if (_movies.IsTitleExists(movie.Title, movie.Id))
            throw new BusinessException("Tên phim đã tồn tại.");

        var hasShowtimes = _showtimes.GetByMovieId(movie.Id).Any();

        if (hasShowtimes &&
            movie.DurationMinutes != existing.DurationMinutes)
        {
            throw new BusinessException(
                "Không thể thay đổi thời lượng phim khi phim đã có lịch chiếu. " +
                "Hãy xử lý các suất chiếu liên quan trước.");
        }

        if (!_movies.Update(movie))
            throw new BusinessException("Không thể cập nhật phim.");
    }

    public IEnumerable<int> GetGenreIds(int movieId)
    {
        if (movieId <= 0 || _movies.GetById(movieId) is null)
            throw new BusinessException("Không tìm thấy phim.");

        return _movies.GetGenreIds(movieId);
    }

    public void SetGenres(int movieId, IEnumerable<int> genreIds)
    {
        AuthorizationGuard.EnsureAuthenticated(_session);
        if (movieId <= 0 || _movies.GetById(movieId) is null)
            throw new BusinessException("Không tìm thấy phim.");

        if (genreIds is null)
            throw new BusinessException("Danh sách thể loại không hợp lệ.");

        var ids = genreIds.Distinct().ToList();

        if (ids.Any(id => id <= 0))
            throw new BusinessException("Thể loại được chọn không hợp lệ.");

        _movies.SetGenres(movieId, ids);
    }

    public void SaveMovie(Movie movie, IEnumerable<int>? genreIds)
    {
        AuthorizationGuard.EnsureAuthenticated(_session);
        _validator.Validate(movie);

        movie.Title = movie.Title.Trim();

        var ids = genreIds?.Distinct().ToList();

        if (ids?.Any(id => id <= 0) == true)
        {
            throw new BusinessException(
                "Thể loại được chọn không hợp lệ.");
        }

        if (movie.Id > 0)
        {
            var existing = _movies.GetById(movie.Id)
                ?? throw new BusinessException("Không tìm thấy phim.");

            var hasShowtimes = _showtimes.GetByMovieId(movie.Id).Any();

            if (hasShowtimes &&
                movie.DurationMinutes != existing.DurationMinutes)
            {
                throw new BusinessException(
                    "Không thể thay đổi thời lượng phim khi phim đã có lịch chiếu.");
            }
        }

        if (_movies.IsTitleExists(
            movie.Title,
            movie.Id > 0 ? movie.Id : null))
        {
            throw new BusinessException("Tên phim đã tồn tại.");
        }

        var savedId = _movies.SaveWithGenres(movie, ids);

        if (savedId <= 0)
            throw new BusinessException("Không thể lưu thông tin phim.");

        movie.Id = savedId;
    }

    public void DeleteMovie(int id)
    {
        AuthorizationGuard.EnsureAuthenticated(_session);
        if (id <= 0)
            throw new BusinessException("Mã phim không hợp lệ.");

        if (_movies.GetById(id) is null)
            throw new BusinessException("Không tìm thấy phim.");

        if (_showtimes.GetByMovieId(id).Any())
        {
            throw new BusinessException(
                "Không thể xóa phim đã có lịch chiếu trong hệ thống.");
        }

        if (!_movies.Delete(id))
            throw new BusinessException("Không thể xóa phim.");
    }
}
