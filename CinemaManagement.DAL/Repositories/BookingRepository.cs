using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;
using CinemaManagement.Core.Interfaces;
using Microsoft.Data.Sqlite;

namespace CinemaManagement.DAL.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly string _connectionString;

    public BookingRepository(string connectionString = "Data Source=cinema.db")
    {
        _connectionString = connectionString;
    }

    public List<Booking> GetAll()
    {
        var list = new List<Booking>();

        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT Id, CustomerId, BookingTime, TotalAmount, Status, PaymentMethod
            FROM Bookings
            ORDER BY BookingTime DESC
            """;

        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            list.Add(MapBooking(reader));
        }

        return list;
    }

    public List<Booking> GetByCustomerId(int customerId)
    {
        var list = new List<Booking>();

        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT Id, CustomerId, BookingTime, TotalAmount, Status, PaymentMethod
            FROM Bookings
            WHERE CustomerId = @CustomerId
            ORDER BY BookingTime DESC
            """;

        cmd.Parameters.AddWithValue("@CustomerId", customerId);

        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            list.Add(MapBooking(reader));
        }

        return list;
    }

    public Booking? GetById(int id)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT Id, CustomerId, BookingTime, TotalAmount, Status, PaymentMethod
            FROM Bookings
            WHERE Id = @Id
            """;

        cmd.Parameters.AddWithValue("@Id", id);

        using var reader = cmd.ExecuteReader();

        return reader.Read() ? MapBooking(reader) : null;
    }

    public int Add(
        Booking booking,
        List<Ticket> tickets,
        List<BookingProduct> products)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var transaction = conn.BeginTransaction();

        try
        {
            var bookingCmd = conn.CreateCommand();
            bookingCmd.Transaction = transaction;

            bookingCmd.CommandText = """
                INSERT INTO Bookings
                    (CustomerId, BookingTime, TotalAmount, Status, PaymentMethod)
                VALUES
                    (@CustomerId, @BookingTime, @TotalAmount, @Status, @PaymentMethod);

                SELECT last_insert_rowid();
                """;

            bookingCmd.Parameters.AddWithValue(
                "@CustomerId",
                booking.CustomerId);

            bookingCmd.Parameters.AddWithValue(
                "@BookingTime",
                booking.BookingTime.ToString("o"));

            bookingCmd.Parameters.AddWithValue(
                "@TotalAmount",
                booking.TotalAmount);

            bookingCmd.Parameters.AddWithValue(
                "@Status",
                booking.Status.ToString());

            bookingCmd.Parameters.AddWithValue(
                "@PaymentMethod",
                booking.PaymentMethod.ToString());

            int bookingId =
                Convert.ToInt32(bookingCmd.ExecuteScalar());

            booking.Id = bookingId;

            foreach (var ticket in tickets)
            {
                var ticketCmd = conn.CreateCommand();
                ticketCmd.Transaction = transaction;

                ticketCmd.CommandText = """
                    INSERT INTO Tickets
                        (BookingId, ShowTimeId, SeatId, Price)
                    VALUES
                        (@BookingId, @ShowTimeId, @SeatId, @Price);
                    """;

                ticketCmd.Parameters.AddWithValue(
                    "@BookingId",
                    bookingId);

                ticketCmd.Parameters.AddWithValue(
                    "@ShowTimeId",
                    ticket.ShowTimeId);

                ticketCmd.Parameters.AddWithValue(
                    "@SeatId",
                    ticket.SeatId);

                ticketCmd.Parameters.AddWithValue(
                    "@Price",
                    ticket.Price);

                ticketCmd.ExecuteNonQuery();
            }

            foreach (var item in products)
            {
                var productCmd = conn.CreateCommand();
                productCmd.Transaction = transaction;

                productCmd.CommandText = """
                    INSERT INTO BookingProducts
                        (BookingId, ProductId, Quantity, Price)
                    VALUES
                        (@BookingId, @ProductId, @Quantity, @Price);
                    """;

                productCmd.Parameters.AddWithValue(
                    "@BookingId",
                    bookingId);

                productCmd.Parameters.AddWithValue(
                    "@ProductId",
                    item.ProductId);

                productCmd.Parameters.AddWithValue(
                    "@Quantity",
                    item.Quantity);

                productCmd.Parameters.AddWithValue(
                    "@Price",
                    item.Price);

                productCmd.ExecuteNonQuery();
            }

            transaction.Commit();

            return bookingId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public void UpdateStatus(int bookingId, string status)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        var cmd = conn.CreateCommand();

        cmd.CommandText = """
            UPDATE Bookings
            SET Status = @Status
            WHERE Id = @Id
            """;

        cmd.Parameters.AddWithValue("@Status", status);
        cmd.Parameters.AddWithValue("@Id", bookingId);

        cmd.ExecuteNonQuery();
    }

    private static Booking MapBooking(SqliteDataReader reader)
    {
        return new Booking
        {
            Id = reader.GetInt32(0),
            CustomerId = reader.GetInt32(1),
            BookingTime = DateTime.Parse(reader.GetString(2)),
            TotalAmount = reader.GetDecimal(3),

            Status = Enum.Parse<BookingStatus>(
                reader.GetString(4),
                ignoreCase: true),

            PaymentMethod = Enum.Parse<PaymentMethod>(
                reader.GetString(5),
                ignoreCase: true)
        };
    }
}