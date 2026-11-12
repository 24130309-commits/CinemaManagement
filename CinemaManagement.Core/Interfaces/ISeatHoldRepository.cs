using CinemaManagement.Core.Entities;

namespace CinemaManagement.Core.Interfaces;

public interface ISeatHoldRepository
{
    List<SeatHold> GetActiveHoldsByShowTime(int showtimeId);
    SeatHold? GetById(int id);
    void Add(SeatHold seatHold);
    void Update(SeatHold seatHold);
    void ReleaseExpiredHolds();
}