using CinemaManagement.Core.Entities;

namespace CinemaManagement.Core.Interfaces;

public interface ITicketRepository
{
    List<Ticket> GetByBookingId(int bookingId);
    List<Ticket> GetByShowTimeId(int showtimeId);
    bool IsSeatBooked(int showtimeId, int seatId);
}