using CinemaManagement.Core.Entities;

namespace CinemaManagement.Core.Interfaces;

public interface ITicketRepository
{
    List<Ticket> GetByBookingId(int bookingId);

    List<Ticket> GetByShowtimeId(int showtimeId);

    bool IsSeatBooked(int showtimeId, int seatId);

    // Tương thích tên phương thức cũ.
    List<Ticket> GetByShowTimeId(int showtimeId)
    {
        return GetByShowtimeId(showtimeId);
    }
}