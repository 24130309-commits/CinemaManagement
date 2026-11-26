using CinemaManagement.Core.Entities;

namespace CinemaManagement.Core.Interfaces;

public interface IPaymentService
{
    List<Payment> GetByBookingId(int bookingId);

    Payment? GetByTransactionCode(string transactionCode);

    bool RefundBooking(int bookingId, string note);
}