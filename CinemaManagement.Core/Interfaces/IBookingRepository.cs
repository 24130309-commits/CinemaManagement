using CinemaManagement.Core.Entities;

namespace CinemaManagement.Core.Interfaces;

public interface IBookingRepository
{
    List<Booking> GetAll();

    List<Booking> GetByCustomerId(int customerId);

    Booking? GetById(int id);

    List<BookingProduct> GetProductsByBookingId(int bookingId);

    int Add(
        Booking booking,
        List<Ticket> tickets,
        List<BookingProduct> products,
        int customerId,
        string sessionId,
        int? promotionId = null);

}
