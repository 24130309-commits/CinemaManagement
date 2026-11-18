using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Exceptions;

namespace CinemaManagement.BLL.Validators;

public sealed class ShowtimeValidator
{
    public void Validate(Showtime showtime)
    {
        if (showtime == null)
            throw new BusinessException(
                "Thông tin suất chiếu không hợp lệ.");

        if (showtime.MovieId <= 0 || showtime.RoomId <= 0)
            throw new BusinessException(
                "Vui lòng chọn phim và phòng chiếu.");

        if (showtime.StartTime <= DateTime.Now)
            throw new BusinessException(
                "Giờ chiếu phải ở tương lai.");

        if (showtime.BasePrice < 0)
            throw new BusinessException(
                "Giá vé không hợp lệ.");
    }
}