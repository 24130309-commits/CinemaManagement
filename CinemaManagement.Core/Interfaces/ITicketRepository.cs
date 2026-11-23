using CinemaManagement.Core.Entities;

namespace CinemaManagement.Core.Interfaces;

public interface ITicketRepository
{
    List<Ticket> GetByBookingId(int bookingId);

    List<Ticket> GetByShowtimeId(int showtimeId)
    {
        return GetByShowTimeId(showtimeId);
    }

    List<Ticket> GetByShowTimeId(int showtimeId)
    {
        return GetByShowtimeId(showtimeId);
    }

    bool IsSeatBooked(int showtimeId, int seatId);
}