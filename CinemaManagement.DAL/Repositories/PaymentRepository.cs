
using Dapper;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.DAL.Database;

namespace CinemaManagement.DAL.Repositories;

public sealed class PaymentRepository : IPaymentRepository
{
    private readonly SqliteConnectionFactory _factory;

    public PaymentRepository(SqliteConnectionFactory factory)
    {
        _factory = factory
            ?? throw new ArgumentNullException(nameof(factory));
    }

    public List<Payment> GetByBookingId(int bookingId)
    {
        if (bookingId <= 0)
            throw new ArgumentOutOfRangeException(nameof(bookingId));

        using var connection = _factory.CreateConnection();

        return connection.Query<Payment>(
            """
            SELECT
                Id,
                BookingId,
                TransactionCode,
                PaymentMethod,
                Amount,
                Status,
                PaidAt,
                ProviderReference,
                Note
            FROM Payments
            WHERE BookingId = @BookingId
            ORDER BY PaidAt DESC, Id DESC;
            """,
            new { BookingId = bookingId }).AsList();
    }

    public Payment? GetByTransactionCode(string transactionCode)
    {
        if (string.IsNullOrWhiteSpace(transactionCode))
            return null;

        using var connection = _factory.CreateConnection();

        return connection.QuerySingleOrDefault<Payment>(
            """
            SELECT
                Id,
                BookingId,
                TransactionCode,
                PaymentMethod,
                Amount,
                Status,
                PaidAt,
                ProviderReference,
                Note
            FROM Payments
            WHERE TransactionCode = @TransactionCode;
            """,
            new { TransactionCode = transactionCode.Trim() });
    }

    public bool RefundBooking(int bookingId, string note)
    {
        if (bookingId <= 0)
            throw new ArgumentOutOfRangeException(nameof(bookingId));

        if (string.IsNullOrWhiteSpace(note))
            throw new ArgumentException(
                "Vui lòng nhập lý do hoàn tiền.",
                nameof(note));

        using var connection = _factory.CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            var booking = connection.QuerySingleOrDefault<Booking>(
                """
                SELECT Id, Status, Total
                FROM Bookings
                WHERE Id = @BookingId;
                """,
                new { BookingId = bookingId },
                transaction);

            if (booking == null || booking.Status != BookingStatus.Paid)
            {
                transaction.Rollback();
                return false;
            }

            var paymentCount = connection.ExecuteScalar<int>(
                """
                SELECT COUNT(*)
                FROM Payments
                WHERE BookingId = @BookingId
                  AND Status = @Success;
                """,
                new
                {
                    BookingId = bookingId,
                    Success = (int)PaymentStatus.Success
                },
                transaction);

            var paidAmount = connection.ExecuteScalar<decimal?>(
                """
                SELECT SUM(Amount)
                FROM Payments
                WHERE BookingId = @BookingId
                  AND Status = @Success;
                """,
                new
                {
                    BookingId = bookingId,
                    Success = (int)PaymentStatus.Success
                },
                transaction) ?? 0m;

            if (paymentCount == 0 || paidAmount != booking.Total)
            {
                transaction.Rollback();
                return false;
            }

            var paymentUpdated = connection.Execute(
                """
                UPDATE Payments
                SET Status = @Refunded,
                    Note = CASE
                        WHEN COALESCE(Note, '') = '' THEN @Note
                        ELSE Note || ' | ' || @Note
                    END
                WHERE BookingId = @BookingId
                  AND Status = @Success;
                """,
                new
                {
                    BookingId = bookingId,
                    Refunded = (int)PaymentStatus.Refunded,
                    Success = (int)PaymentStatus.Success,
                    Note = note.Trim()
                },
                transaction);

            if (paymentUpdated != paymentCount)
                throw new InvalidOperationException(
                    "Không thể cập nhật đầy đủ trạng thái thanh toán.");

            var bookingUpdated = connection.Execute(
                """
                UPDATE Bookings
                SET Status = @Refunded
                WHERE Id = @BookingId
                  AND Status = @Paid;
                """,
                new
                {
                    BookingId = bookingId,
                    Refunded = (int)BookingStatus.Refunded,
                    Paid = (int)BookingStatus.Paid
                },
                transaction);

            if (bookingUpdated != 1)
                throw new InvalidOperationException(
                    "Không thể cập nhật trạng thái hóa đơn.");

            connection.Execute(
                """
                UPDATE Tickets
                SET Status = 'Refunded'
                WHERE BookingId = @BookingId
                  AND Status = 'Active';
                """,
                new { BookingId = bookingId },
                transaction);

            transaction.Commit();

            return true;
        }
        catch
        {
            try
            {
                transaction.Rollback();
            }
            catch
            {
            }

            throw;
        }
    }
}
