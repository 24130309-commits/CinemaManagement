using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Interfaces;
using Microsoft.Data.Sqlite;

namespace CinemaManagement.DAL.Repositories;

public class TicketRepository : ITicketRepository
{
    private readonly string _connectionString;

    public TicketRepository(string connectionString = "Data Source=cinema.db")
    {
        _connectionString = connectionString;
    }

    public List<Ticket> GetByBookingId(int bookingId)
    {
        var list = new List<Ticket>();
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, BookingId, ShowTimeId, SeatId, Price FROM Tickets WHERE BookingId = @BookingId";
        cmd.Parameters.AddWithValue("@BookingId", bookingId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(MapTicket(reader));
        }
        return list;
    }

    public List<Ticket> GetByShowTimeId(int showtimeId)
    {
        var list = new List<Ticket>();
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, BookingId, ShowTimeId, SeatId, Price FROM Tickets WHERE ShowTimeId = @ShowTimeId";
        cmd.Parameters.AddWithValue("@ShowTimeId", showtimeId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(MapTicket(reader));
        }
        return list;
    }

    public bool IsSeatBooked(int showtimeId, int seatId)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT COUNT(1)
            FROM Tickets t
            INNER JOIN Bookings b ON t.BookingId = b.Id
            WHERE t.ShowTimeId = @ShowTimeId AND t.SeatId = @SeatId AND b.Status != 'Canceled'";
        cmd.Parameters.AddWithValue("@ShowTimeId", showtimeId);
        cmd.Parameters.AddWithValue("@SeatId", seatId);

        long count = (long)(cmd.ExecuteScalar() ?? 0L);
        return count > 0;
    }

    private static Ticket MapTicket(SqliteDataReader reader)
    {
        return new Ticket
        {
            Id = reader.GetInt32(0),
            BookingId = reader.GetInt32(1),
            ShowTimeId = reader.GetInt32(2),
            SeatId = reader.GetInt32(3),
            Price = reader.GetDecimal(4)
        };
    }
}
