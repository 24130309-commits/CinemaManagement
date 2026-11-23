using CinemaManagement.Core.Entities;

namespace CinemaManagement.Core.Interfaces;

public interface ISeatHoldRepository
{
    List<SeatHold> GetActiveHoldsByShowtime(int showtimeId);

    List<SeatHold> GetActiveHoldsByShowTime(int showtimeId)
    {
        return GetActiveHoldsByShowtime(showtimeId);
    }

    SeatHold? GetById(int id);

    void Add(SeatHold seatHold);

    void Update(SeatHold seatHold);

    void ReleaseExpiredHolds();
}