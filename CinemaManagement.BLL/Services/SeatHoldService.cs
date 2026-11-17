using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.BLL.Services;

public class SeatHoldService : ISeatHoldService
{
    private readonly ISeatHoldRepository _seatHoldRepo;
    private readonly ITicketRepository _ticketRepo;
    private const int HoldMinutes = 5;

    public SeatHoldService(ISeatHoldRepository seatHoldRepo, ITicketRepository ticketRepo)
    {
        _seatHoldRepo = seatHoldRepo;
        _ticketRepo = ticketRepo;
    }

    public bool IsSeatAvailable(int showtimeId, int seatId)
    {
        _seatHoldRepo.ReleaseExpiredHolds();

        if (_ticketRepo.IsSeatBooked(showtimeId, seatId))
            return false;

        var activeHolds = _seatHoldRepo.GetActiveHoldsByShowTime(showtimeId);
        return !activeHolds.Any(h => h.SeatId == seatId && !h.IsReleased && h.ExpiryTime > DateTime.Now);
    }

    public SeatHold HoldSeat(int showtimeId, int seatId, int customerId)
    {
        _seatHoldRepo.ReleaseExpiredHolds();

        if (_ticketRepo.IsSeatBooked(showtimeId, seatId))
            throw new BusinessException("Ghế này đã được đặt mua.");

        var activeHolds = _seatHoldRepo.GetActiveHoldsByShowTime(showtimeId);
        var existingHold = activeHolds.FirstOrDefault(h => h.SeatId == seatId && !h.IsReleased && h.ExpiryTime > DateTime.Now);

        if (existingHold != null)
        {
            if (existingHold.CustomerId == customerId)
            {
                existingHold.ExpiryTime = DateTime.Now.AddMinutes(HoldMinutes);
                _seatHoldRepo.Update(existingHold);
                return existingHold;
            }
            throw new BusinessException("Ghế này đang được khách hàng khác giữ chỗ.");
        }

        var newHold = new SeatHold
        {
            ShowtimeId = showtimeId,
            SeatId = seatId,
            CustomerId = customerId,
            HoldTime = DateTime.Now,
            ExpiryTime = DateTime.Now.AddMinutes(HoldMinutes),
            IsReleased = false
        };

        _seatHoldRepo.Add(newHold);
        return newHold;
    }

    public void ReleaseHold(int holdId)
    {
        var hold = _seatHoldRepo.GetById(holdId);
        if (hold == null) return;

        hold.IsReleased = true;
        _seatHoldRepo.Update(hold);
    }

    public void ReleaseAllCustomerHolds(int showtimeId, int customerId)
    {
        var activeHolds = _seatHoldRepo.GetActiveHoldsByShowTime(showtimeId);
        foreach (var hold in activeHolds.Where(h => h.CustomerId == customerId && !h.IsReleased))
        {
            hold.IsReleased = true;
            _seatHoldRepo.Update(hold);
        }
    }
}