
using CinemaManagement.Core.Constants;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.BLL.Session;

namespace CinemaManagement.BLL.Services;

public class ShowtimeService : IShowtimesService
{
    private readonly IShowtimeRepository _showtimeRepository;
    private readonly IMovieRepository _movieRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly ISettingService? _settingService;
    private readonly CurrentUserSession _session;

    public ShowtimeService(
        IShowtimeRepository showtimeRepository,
        IMovieRepository movieRepository,
        IRoomRepository roomRepository,
        ISettingService? settingService,
        CurrentUserSession session)
    {
        _showtimeRepository = showtimeRepository
            ?? throw new ArgumentNullException(nameof(showtimeRepository));

        _movieRepository = movieRepository
            ?? throw new ArgumentNullException(nameof(movieRepository));

        _roomRepository = roomRepository
            ?? throw new ArgumentNullException(nameof(roomRepository));

        _settingService = settingService;
        _session = session
            ?? throw new ArgumentNullException(nameof(session));
    }

    public IEnumerable<Showtime> GetAllShowtimes()
    {
        return _showtimeRepository.GetAll();
    }

    public IEnumerable<Showtime> GetShowtimesByDate(DateTime date)
    {
        return _showtimeRepository.GetByDate(date.Date);
    }

    public Showtime GetShowtimeById(int id)
    {
        if (id <= 0)
            throw new BusinessException("Mã suất chiếu không hợp lệ.");

        return _showtimeRepository.GetById(id)
            ?? throw new BusinessException("Không tìm thấy suất chiếu.");
    }

    public void AddShowtime(Showtime showtime)
    {
        AuthorizationGuard.EnsureAdministrator(_session, "quản lý suất chiếu");
        PrepareAndValidateShowtime(showtime, false);

        var id = _showtimeRepository.Insert(showtime);

        if (id <= 0)
            throw new BusinessException("Không thể thêm suất chiếu.");

        showtime.Id = id;
    }

    public void UpdateShowtime(Showtime showtime)
    {
        AuthorizationGuard.EnsureAdministrator(_session, "quản lý suất chiếu");
        if (showtime is null)
            throw new BusinessException("Dữ liệu lịch chiếu không hợp lệ.");

        if (showtime.Id <= 0)
            throw new BusinessException("Mã suất chiếu không hợp lệ.");

        var existing = _showtimeRepository.GetById(showtime.Id)
            ?? throw new BusinessException(
                "Không tìm thấy suất chiếu cần cập nhật.");

        bool hasTickets = _showtimeRepository.HasTickets(showtime.Id);

        // Suất chiếu đã có vé chỉ được phép ngừng hoạt động.
        // Không cho thay đổi phim, phòng, thời gian hoặc giá vé.
        if (hasTickets)
        {
            bool onlyDeactivating =
                !showtime.IsActive &&
                existing.IsActive &&
                showtime.MovieId == existing.MovieId &&
                showtime.RoomId == existing.RoomId &&
                showtime.StartTime == existing.StartTime &&
                showtime.EndTime == existing.EndTime &&
                showtime.BasePrice == existing.BasePrice;

            if (!onlyDeactivating)
            {
                throw new BusinessException(
                    "Suất chiếu đã phát sinh vé. Chỉ được phép ngừng hoạt động, " +
                    "không được thay đổi phim, phòng, thời gian hoặc giá vé.");
            }

            if (!_showtimeRepository.Update(showtime))
            {
                throw new BusinessException(
                    "Không thể ngừng hoạt động suất chiếu.");
            }

            return;
        }

        PrepareAndValidateShowtime(showtime, true);

        if (!_showtimeRepository.Update(showtime))
            throw new BusinessException("Không thể cập nhật suất chiếu.");
    }

    public void DeleteShowtime(int id)
    {
        AuthorizationGuard.EnsureAdministrator(_session, "quản lý suất chiếu");
        if (id <= 0)
            throw new BusinessException("Mã suất chiếu không hợp lệ.");

        if (_showtimeRepository.GetById(id) is null)
        {
            throw new BusinessException(
                "Không tìm thấy suất chiếu cần xóa.");
        }

        if (_showtimeRepository.HasTickets(id))
        {
            throw new BusinessException(
                "Không thể xóa suất chiếu đã có vé. " +
                "Hãy chuyển suất chiếu sang trạng thái ngừng hoạt động.");
        }

        if (!_showtimeRepository.Delete(id))
            throw new BusinessException("Không thể xóa suất chiếu.");
    }

    private void PrepareAndValidateShowtime(
        Showtime showtime,
        bool isUpdate)
    {
        if (showtime is null)
            throw new BusinessException("Dữ liệu lịch chiếu không hợp lệ.");

        if (isUpdate && showtime.Id <= 0)
            throw new BusinessException("Mã suất chiếu không hợp lệ.");

        if (showtime.MovieId <= 0)
            throw new BusinessException("Phim được chọn không hợp lệ.");

        if (showtime.RoomId <= 0)
            throw new BusinessException("Phòng chiếu không hợp lệ.");

        if (showtime.BasePrice < 0)
            throw new BusinessException(
                "Giá vé cơ bản không được nhỏ hơn 0.");

        if (showtime.StartTime <= DateTime.Now)
        {
            throw new BusinessException(
                "Thời gian bắt đầu suất chiếu phải ở tương lai.");
        }

        var movie = _movieRepository.GetById(showtime.MovieId)
            ?? throw new BusinessException(
                "Phim được chọn không tồn tại.");

        if (movie.Status == MovieStatus.Ended)
        {
            throw new BusinessException(
                "Không thể lập suất chiếu mới cho phim đã kết thúc.");
        }

        if (movie.DurationMinutes <= 0)
            throw new BusinessException("Thời lượng phim không hợp lệ.");

        var room = _roomRepository.GetById(showtime.RoomId);

        if (room is null || !room.IsActive)
        {
            throw new BusinessException(
                "Phòng chiếu không tồn tại hoặc đang ngừng hoạt động.");
        }

        var cleaningMinutes = _settingService?.GetCleaningMinutes()
            ?? AppConstants.DefaultCleaningMinutes;

        if (cleaningMinutes < 0)
        {
            throw new BusinessException(
                "Thời gian vệ sinh phòng chiếu không hợp lệ.");
        }

        try
        {
            showtime.EndTime = showtime.StartTime.AddMinutes(
                movie.DurationMinutes + cleaningMinutes);
        }
        catch (ArgumentOutOfRangeException)
        {
            throw new BusinessException(
                "Thời gian kết thúc suất chiếu không hợp lệ.");
        }

        if (showtime.EndTime <= showtime.StartTime)
        {
            throw new BusinessException(
                "Thời gian kết thúc phải sau thời gian bắt đầu.");
        }

        int? excludeId = isUpdate ? showtime.Id : null;

        if (showtime.IsActive &&
            _showtimeRepository.HasConflict(
                showtime.RoomId,
                showtime.StartTime,
                showtime.EndTime,
                excludeId))
        {
            throw new ShowtimeConflictException(
                $"Phòng {room.Name} đã có suất chiếu khác trong " +
                $"khung giờ {showtime.StartTime:HH:mm} - {showtime.EndTime:HH:mm}.");
        }
    }
}
