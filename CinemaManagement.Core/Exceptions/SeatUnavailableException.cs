namespace CinemaManagement.Core.Exceptions;

public class SeatUnavailableException : BusinessException
{
    public SeatUnavailableException()
        : base("Ghế này đã được giữ hoặc đã được đặt bởi người khác.")
    {
    }

    public SeatUnavailableException(string message)
        : base(message)
    {
    }
}

// Tương thích tên lớp bị viết sai trong code cũ.
[Obsolete("Hãy sử dụng SeatUnavailableException.")]
public class SeatUnavailbleExcption : SeatUnavailableException
{
    public SeatUnavailbleExcption()
    {
    }

    public SeatUnavailbleExcption(string message)
        : base(message)
    {
    }
}