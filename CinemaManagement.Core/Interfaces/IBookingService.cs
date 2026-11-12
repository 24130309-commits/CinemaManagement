using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;
namespace CinemaManagement.Core.Interfaces;

public interface IBookingService
{
    int CreateBooking(int customerId, int showtimeId, List<int> seatIds, List<BookingProduct> products,
        PaymentMethod paymentMethod);

    List<Booking> GetBookingHistory(int customerId);
    Booking? GetBookingDetails(int bookingId);
    
}