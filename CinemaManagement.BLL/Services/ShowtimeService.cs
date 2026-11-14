using System;
using System.Collections.Generic;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.BLL.Services
{
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
            _showtimeRepository = showtimeRepository;
            _movieRepository = movieRepository;
            _roomRepository = roomRepository;
        }

        public IEnumerable<Showtime> GetAllShowtimes() => _showtimeRepository.GetAll();

        public IEnumerable<Showtime> GetShowtimesByDate(DateTime date) => _showtimeRepository.GetByDate(date);

        public Showtime GetShowtimeById(int id) => _showtimeRepository.GetById(id);

        public void AddShowtime(Showtime showtime)
        {
            PrepareAndValidateShowtime(showtime, isUpdate: false);
            _showtimeRepository.Add(showtime);
        }

        public void UpdateShowtime(Showtime showtime)
        {
            PrepareAndValidateShowtime(showtime, isUpdate: true);
            _showtimeRepository.Update(showtime);
        }

        public void DeleteShowtime(int id)
        {
            _showtimeRepository.Delete(id);
        }

        private void PrepareAndValidateShowtime(Showtime showtime, bool isUpdate)
        {
            if (showtime == null)
                
                throw new BusinessException("Dữ liệu lịch chiếu không hợp lệ.");

            if (showtime.BasePrice < 0)
                throw new BusinessException("Giá vé cơ bản không được nhỏ hơn 0.");

            var movie = _movieRepository.GetById(showtime.MovieId);
            if (movie == null)
                throw new BusinessException("Phim được chọn không tồn tại.");

            var room = _roomRepository.GetById(showtime.RoomId);
            if (room == null || !room.IsActive)
                throw new BusinessException("Phòng chiếu không tồn tại hoặc đang ngưng hoạt động.");

            if (!isUpdate && showtime.StartTime <= DateTime.Now)
                throw new BusinessException("Thời gian bắt đầu suất chiếu phải ở tương lai.");

            // Tự động tính EndTime = StartTime + Thời lượng phim + 15 phút dọn dẹp
            showtime.EndTime = showtime.StartTime.AddMinutes(movie.Duration + CleaningBufferMinutes);

            // Kiểm tra trùng lịch chiếu trong cùng phòng
            int? excludeId = isUpdate ? showtime.Id : (int?)null;
            if (_showtimeRepository.HasConflict(showtime.RoomId, showtime.StartTime, showtime.EndTime, excludeId))
            {
                throw new ShowtimeConflictException(
                    $"Phòng {room.Name} đã có suất chiếu khác trong khung giờ {showtime.StartTime:HH:mm} - {showtime.EndTime:HH:mm}.");
            }
        }
    }
}