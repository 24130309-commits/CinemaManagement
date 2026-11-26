using CinemaManagement.Core.Entities;

namespace CinemaManagement.Core.Interfaces;

public interface IPaymentRepository
{
    List<Payment> GetByBookingId(int bookingId);

    Payment? GetByTransactionCode(string transactionCode);

    bool RefundBooking(int bookingId, string note);
}