using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Exceptions;

namespace CinemaManagement.BLL.Validators;

public sealed class MovieValidator
{
    public void Validate(Movie movie)
    {
        if (movie == null)
            throw new BusinessException(
                "Thông tin phim không hợp lệ.");

        if (string.IsNullOrWhiteSpace(movie.Title))
            throw new BusinessException(
                "Tên phim không được để trống.");

        if (movie.DurationMinutes <= 0)
            throw new BusinessException(
                "Thời lượng phim phải lớn hơn 0.");

        if (!Enum.IsDefined(movie.Status))
            throw new BusinessException(
                "Trạng thái phim không hợp lệ.");

        if (movie.AgeLimit < 0 || movie.AgeLimit > 21)
            throw new BusinessException(
                "Giới hạn tuổi không hợp lệ.");

        if (movie.ReleaseDate == default)
            throw new BusinessException(
                "Vui lòng chọn ngày khởi chiếu.");
    }
}