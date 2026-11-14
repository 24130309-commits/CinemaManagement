using CinemaManagement.Core.Entities;

namespace CinemaManagement.Core.Interfaces;

public interface IBookingRepository
{
    List<Booking>  GetAll();
    List<Booking> GetByCustomerId(int customerId);
    Booking? GetById(int id);
    int Add(Booking booking, List<Ticket> tickets, List<BookingProduct> products);
    void UpdateStatus(int bookingId, string status);
}