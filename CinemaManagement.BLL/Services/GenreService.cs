using CinemaManagement.BLL.Session;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.BLL.Services;

public sealed class GenreService : IGenreService
{
    private readonly IGenreRepository _genres;
    private readonly CurrentUserSession _session;

    public GenreService(IGenreRepository genres, CurrentUserSession session)
    {
        _genres = genres ?? throw new ArgumentNullException(nameof(genres));
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    public IEnumerable<Genre> GetAll() => _genres.GetAll();

    public void Create(Genre genre)
    {
        AuthorizationGuard.EnsureAdministrator(_session, "quản lý thể loại phim");
        Validate(genre);
        genre.Name = genre.Name.Trim();

        if (_genres.IsNameExists(genre.Name))
            throw new BusinessException("Tên thể loại đã tồn tại.");

        try
        {
            genre.Id = _genres.Insert(genre);
        }
        catch (InvalidOperationException ex)
        {
            throw new BusinessException(ex.Message, ex);
        }

        if (genre.Id <= 0)
            throw new BusinessException("Không thể thêm thể loại phim.");
    }

    public void Update(Genre genre)
    {
        AuthorizationGuard.EnsureAdministrator(_session, "quản lý thể loại phim");
        Validate(genre);

        if (genre.Id <= 0)
            throw new BusinessException("Mã thể loại không hợp lệ.");

        genre.Name = genre.Name.Trim();

        if (_genres.GetById(genre.Id) is null)
            throw new BusinessException("Không tìm thấy thể loại phim.");

        if (_genres.IsNameExists(genre.Name, genre.Id))
            throw new BusinessException("Tên thể loại đã tồn tại.");

        try
        {
            if (!_genres.Update(genre))
                throw new BusinessException("Không thể cập nhật thể loại phim.");
        }
        catch (InvalidOperationException ex)
        {
            throw new BusinessException(ex.Message, ex);
        }
    }

    public void Delete(int genreId)
    {
        AuthorizationGuard.EnsureAdministrator(_session, "quản lý thể loại phim");

        if (genreId <= 0)
            throw new BusinessException("Mã thể loại không hợp lệ.");

        if (_genres.GetById(genreId) is null)
            throw new BusinessException("Không tìm thấy thể loại phim.");

        if (_genres.IsInUse(genreId))
            throw new BusinessException(
                "Không thể xóa thể loại đang được gán cho phim.");

        try
        {
            if (!_genres.Delete(genreId))
                throw new BusinessException("Không thể xóa thể loại phim.");
        }
        catch (InvalidOperationException ex)
        {
            throw new BusinessException(ex.Message, ex);
        }
    }

    private static void Validate(Genre genre)
    {
        if (genre is null || string.IsNullOrWhiteSpace(genre.Name))
            throw new BusinessException("Tên thể loại không được để trống.");

        if (genre.Name.Trim().Length > 100)
            throw new BusinessException("Tên thể loại không được vượt quá 100 ký tự.");
    }
}
