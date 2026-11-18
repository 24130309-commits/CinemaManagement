
using System;
using System.Collections.Generic;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.BLL.Services;

public class ShowtimeService : IShowtimesService
{
    private readonly IShowtimeRepository _showtimeRepository;
    private readonly IMovieRepository _movieRepository;
    private readonly IRoomRepository _roomRepository;

    private const int CleaningBufferMinutes = 15;

    public ShowtimeService(
        IShowtimeRepository showtimeRepository,
        IMovieRepository movieRepository,
        IRoomRepository roomRepository)
    {
        _showtimeRepository = showtimeRepository
            ?? throw new ArgumentNullException(nameof(showtimeRepository));

        _movieRepository = movieRepository
            ?? throw new ArgumentNullException(nameof(movieRepository));

        _roomRepository = roomRepository
            ?? throw new ArgumentNullException(nameof(roomRepository));
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

        return _showtimeRepository.GetById(id);
    }

    public void AddShowtime(Showtime showtime)
    {
        PrepareAndValidateShowtime(showtime, false);

        if (_showtimeRepository.Insert(showtime) <= 0)
            throw new BusinessException("Không thể thêm suất chiếu.");
    }

    public void UpdateShowtime(Showtime showtime)
    {
        PrepareAndValidateShowtime(showtime, true);

        if (!_showtimeRepository.Update(showtime))
            throw new BusinessException("Không thể cập nhật suất chiếu.");
    }

    public void DeleteShowtime(int id)
    {
        if (id <= 0)
            throw new BusinessException("Mã suất chiếu không hợp lệ.");

        if (!_showtimeRepository.Delete(id))
            throw new BusinessException("Không thể xóa suất chiếu.");
    }

    private void PrepareAndValidateShowtime(
        Showtime showtime,
        bool isUpdate)
    {
        if (showtime == null)
            throw new BusinessException(
                "Dữ liệu lịch chiếu không hợp lệ.");

        if (showtime.BasePrice < 0)
            throw new BusinessException(
                "Giá vé cơ bản không được nhỏ hơn 0.");

        if (showtime.StartTime <= DateTime.Now)
            throw new BusinessException(
                "Thời gian bắt đầu suất chiếu phải ở tương lai.");

        var movie = _movieRepository.GetById(showtime.MovieId);

        if (movie == null)
            throw new BusinessException(
                "Phim được chọn không tồn tại.");

        var room = _roomRepository.GetById(showtime.RoomId);

        if (room == null || !room.IsActive)
            throw new BusinessException(
                "Phòng chiếu không tồn tại hoặc đang ngưng hoạt động.");

        showtime.EndTime = showtime.StartTime.AddMinutes(
            movie.DurationMinutes + CleaningBufferMinutes);

        int? excludeId = isUpdate ? showtime.Id : null;

        if (_showtimeRepository.HasConflict(
            showtime.RoomId,
            showtime.StartTime,
            showtime.EndTime,
            excludeId))
        {
            throw new ShowtimeConflictException(
                $"Phòng {room.Name} đã có suất chiếu khác trong " +
                $"khung giờ {showtime.StartTime:HH:mm} - " +
                $"{showtime.EndTime:HH:mm}.");
        }
    }
}
