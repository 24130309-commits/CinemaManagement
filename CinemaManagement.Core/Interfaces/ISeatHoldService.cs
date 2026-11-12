using CinemaManagement.Core.Entities;
namespace CinemaManagement.Core.Interfaces;
public interface ISeatHoldService
{
    SeatHold HoldSeat(int showtimeId, int seatId, int customerId);
    void ReleaseHold(int holdId);
    void ReleaseAllCustomerHolds(int showtimeId, int customerId);
    bool IsSeatAvailable(int showtimeId, int seatId);
}