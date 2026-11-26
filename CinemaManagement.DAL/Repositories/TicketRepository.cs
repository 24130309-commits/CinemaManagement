
using Dapper;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.DAL.Database;

namespace CinemaManagement.DAL.Repositories;

public sealed class TicketRepository : ITicketRepository
{
    private readonly SqliteConnectionFactory _factory;

    public TicketRepository(SqliteConnectionFactory factory)
    {
        _factory = factory
            ?? throw new ArgumentNullException(nameof(factory));
    }

    public List<Ticket> GetByBookingId(int bookingId)
    {
        if (bookingId <= 0)
            throw new ArgumentOutOfRangeException(nameof(bookingId));

        using var connection = _factory.CreateConnection();

        return connection.Query<Ticket>(
            """
            SELECT
                Id,
                BookingId,
                ShowtimeId,
                SeatId,
                Price,
                Status,
                QrCode
            FROM Tickets
            WHERE BookingId = @BookingId
            ORDER BY Id;
            """,
            new { BookingId = bookingId }).AsList();
    }

    public List<Ticket> GetByShowTimeId(int showtimeId)
    {
        if (showtimeId <= 0)
            throw new ArgumentOutOfRangeException(nameof(showtimeId));

        using var connection = _factory.CreateConnection();

        return connection.Query<Ticket>(
            """
            SELECT
                t.Id,
                t.BookingId,
                t.ShowtimeId,
                t.SeatId,
                t.Price,
                t.Status,
                t.QrCode
            FROM Tickets t
            INNER JOIN Bookings b ON b.Id = t.BookingId
            WHERE t.ShowtimeId = @ShowtimeId
              AND t.Status = 'Active'
              AND b.Status = @PaidStatus
            ORDER BY t.SeatId;
            """,
            new
            {
                ShowtimeId = showtimeId,
                PaidStatus = (int)BookingStatus.Paid
            }).AsList();
    }

    public bool IsSeatBooked(int showtimeId, int seatId)
    {
        if (showtimeId <= 0 || seatId <= 0)
            return false;

        using var connection = _factory.CreateConnection();

        return connection.ExecuteScalar<bool>(
            """
            SELECT EXISTS
            (
                SELECT 1
                FROM Tickets t
                INNER JOIN Bookings b ON b.Id = t.BookingId
                WHERE t.ShowtimeId = @ShowtimeId
                  AND t.SeatId = @SeatId
                  AND t.Status = 'Active'
                  AND b.Status = @PaidStatus
            );
            """,
            new
            {
                ShowtimeId = showtimeId,
                SeatId = seatId,
                PaidStatus = (int)BookingStatus.Paid
            });
    }
}
