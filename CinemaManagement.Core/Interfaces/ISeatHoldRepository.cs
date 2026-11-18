using CinemaManagement.Core.Entities;

namespace CinemaManagement.Core.Interfaces;

public interface ISeatHoldRepository
{
    List<SeatHold> GetActiveHoldsByShowtime(int showtimeId);

    SeatHold? GetById(int id);

    void Add(SeatHold seatHold);

    void Update(SeatHold seatHold);

    void ReleaseExpiredHolds();

    // Tương thích tên phương thức cũ.
    List<SeatHold> GetActiveHoldsByShowTime(int showtimeId)
    {
        return GetActiveHoldsByShowtime(showtimeId);
    }
}