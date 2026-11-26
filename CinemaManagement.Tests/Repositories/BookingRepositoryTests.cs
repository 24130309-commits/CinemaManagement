using CinemaManagement.Core.Interfaces;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;
using CinemaManagement.DAL.Database;
using CinemaManagement.DAL.Repositories;
using Dapper;
using Microsoft.Data.Sqlite;

namespace CinemaManagement.Tests.Repositories;

[TestFixture]
public sealed class BookingRepositoryTests
{
    [Test]
    public void BookingPaymentTicketAndRefundWorkflow_UsesAtomicDatabaseTransitions()
    {
        using var database = new TemporaryBookingDatabase();
        var factory = new SqliteConnectionFactory(database.Path);
        var bookings = new BookingRepository(factory);
        var booking = NewBooking();
        var tickets = new List<Ticket>
        {
            new()
            {
                ShowtimeId = 11,
                SeatId = 21,
                Price = 120000,
                Status = "Active",
                QrCode = "test-ticket-qr"
            }
        };
        var products = new List<BookingProduct>
        {
            new() { ProductId = 31, Quantity = 1, Price = 30000 }
        };

        booking.Subtotal = 150000;
        booking.Total = 150000;
        var bookingId = bookings.Add(
            booking,
            tickets,
            products,
            customerId: 7,
            sessionId: "checkout-session");

        using (var connection = factory.CreateConnection())
        {
            Assert.Multiple(() =>
            {
                Assert.That(bookings.GetById(bookingId)?.Status, Is.EqualTo(BookingStatus.Paid));
                Assert.That(bookings.GetByCustomerId(7).Single().Id, Is.EqualTo(bookingId));
                Assert.That(connection.QuerySingle<int>(
                    "SELECT COUNT(*) FROM Payments WHERE BookingId = @bookingId AND Status = @status",
                    new { bookingId, status = (int)PaymentStatus.Success }), Is.EqualTo(1));
                Assert.That(connection.QuerySingle<decimal>(
                    "SELECT Amount FROM Payments WHERE BookingId = @bookingId",
                    new { bookingId }), Is.EqualTo(150000));
                Assert.That(bookings.GetProductsByBookingId(bookingId).Single().Quantity, Is.EqualTo(1));
                Assert.That(connection.QuerySingle<string>(
                    "SELECT Status FROM Tickets WHERE BookingId = @bookingId",
                    new { bookingId }), Is.EqualTo("Active"));
                Assert.That(connection.QuerySingle<int>(
                    "SELECT IsReleased FROM SeatHolds WHERE SeatId = 21"), Is.EqualTo(1));
            });
        }

        var payments = new PaymentRepository(factory);
        Assert.That(payments.RefundBooking(bookingId, "test refund"), Is.True);

        using var verification = factory.CreateConnection();
        Assert.Multiple(() =>
        {
            Assert.That(verification.QuerySingle<int>(
                "SELECT Status FROM Bookings WHERE Id = @bookingId",
                new { bookingId }), Is.EqualTo((int)BookingStatus.Refunded));
            Assert.That(verification.QuerySingle<int>(
                "SELECT Status FROM Payments WHERE BookingId = @bookingId",
                new { bookingId }), Is.EqualTo((int)PaymentStatus.Refunded));
            Assert.That(verification.QuerySingle<string>(
                "SELECT Status FROM Tickets WHERE BookingId = @bookingId",
                new { bookingId }), Is.EqualTo("Refunded"));
        });
    }

    [Test]
    public void BookingAdd_RollsBackAllRowsWhenPromotionIsNoLongerAvailable()
    {
        using var database = new TemporaryBookingDatabase();
        var factory = new SqliteConnectionFactory(database.Path);
        var repository = new BookingRepository(factory);

        Assert.Throws<InvalidOperationException>(() => repository.Add(
            NewBooking(),
            [new Ticket
            {
                ShowtimeId = 11,
                SeatId = 21,
                Price = 120000,
                Status = "Active",
                QrCode = "rollback-ticket-qr"
            }],
            [],
            customerId: 7,
            sessionId: "checkout-session",
            promotionId: 41));

        using var connection = factory.CreateConnection();
        Assert.Multiple(() =>
        {
            Assert.That(connection.QuerySingle<int>("SELECT COUNT(*) FROM Bookings"), Is.Zero);
            Assert.That(connection.QuerySingle<int>("SELECT COUNT(*) FROM Payments"), Is.Zero);
            Assert.That(connection.QuerySingle<int>("SELECT COUNT(*) FROM Tickets"), Is.Zero);
            Assert.That(connection.QuerySingle<int>("SELECT IsReleased FROM SeatHolds WHERE SeatId = 21"), Is.Zero);
        });
    }

    [Test]
    public void GetByCustomerId_ReturnsBookingsInReverseChronologicalOrder()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            $"cinema-booking-repository-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        try
        {
            var databasePath = Path.Combine(directory, "test.db");
            var factory = new SqliteConnectionFactory(databasePath);

            using (var connection = factory.CreateConnection())
            {
                connection.Execute(
                    """
                    CREATE TABLE Bookings (
                        Id INTEGER PRIMARY KEY,
                        Code TEXT NOT NULL,
                        CustomerId INTEGER,
                        UserId INTEGER NOT NULL,
                        CreatedAt TEXT NOT NULL,
                        Subtotal NUMERIC NOT NULL,
                        Discount NUMERIC NOT NULL,
                        Total NUMERIC NOT NULL,
                        PaymentMethod INTEGER NOT NULL,
                        Status INTEGER NOT NULL
                    );
                    """);
                connection.Execute(
                    """
                    INSERT INTO Bookings (
                        Id, Code, CustomerId, UserId, CreatedAt,
                        Subtotal, Discount, Total, PaymentMethod, Status)
                    VALUES
                        (1, 'OLD', 7, 1, '2026-01-01 10:00:00', 100, 0, 100, 0, 1),
                        (2, 'NEW', 7, 1, '2026-01-02 10:00:00', 200, 0, 200, 1, 1),
                        (3, 'OTHER', 8, 1, '2026-01-03 10:00:00', 300, 0, 300, 0, 1);
                    """);
            }

            var repository = new BookingRepository(factory);

            var history = repository.GetByCustomerId(7);

            Assert.That(history.Select(booking => booking.Code),
                Is.EqualTo(new[] { "NEW", "OLD" }));
            Assert.That(history.All(booking => booking.CustomerId == 7), Is.True);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(directory, recursive: true);
        }
    }

    private static Booking NewBooking()
    {
        return new Booking
        {
            Code = $"TEST-{Guid.NewGuid():N}",
            CustomerId = 7,
            UserId = 3,
            CreatedAt = DateTime.Now,
            Subtotal = 120000,
            Discount = 0,
            Total = 120000,
            PaymentMethod = PaymentMethod.Cash,
            Status = BookingStatus.Paid
        };
    }

    private sealed class TemporaryBookingDatabase : IDisposable
    {
        private readonly string _directory = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"cinema-booking-workflow-{Guid.NewGuid():N}");

        public string Path { get; }

        public TemporaryBookingDatabase()
        {
            Directory.CreateDirectory(_directory);
            Path = System.IO.Path.Combine(_directory, "workflow.db");
            var factory = new SqliteConnectionFactory(Path);
            using var connection = factory.CreateConnection();
            connection.Execute(
                """
                CREATE TABLE Customers (Id INTEGER PRIMARY KEY);
                CREATE TABLE Rooms (Id INTEGER PRIMARY KEY, IsActive INTEGER NOT NULL);
                CREATE TABLE Showtimes (
                    Id INTEGER PRIMARY KEY,
                    RoomId INTEGER NOT NULL,
                    IsActive INTEGER NOT NULL,
                    StartTime TEXT NOT NULL);
                CREATE TABLE Seats (
                    Id INTEGER PRIMARY KEY,
                    RoomId INTEGER NOT NULL,
                    SeatType INTEGER NOT NULL);
                CREATE TABLE SeatHolds (
                    Id INTEGER PRIMARY KEY,
                    ShowtimeId INTEGER NOT NULL,
                    SeatId INTEGER NOT NULL,
                    CustomerId INTEGER NOT NULL,
                    SessionId TEXT NOT NULL,
                    IsReleased INTEGER NOT NULL,
                    ExpiresAt TEXT NOT NULL);
                CREATE TABLE Bookings (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Code TEXT NOT NULL,
                    CustomerId INTEGER,
                    UserId INTEGER NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    Subtotal NUMERIC NOT NULL,
                    Discount NUMERIC NOT NULL,
                    Total NUMERIC NOT NULL,
                    PaymentMethod INTEGER NOT NULL,
                    Status INTEGER NOT NULL);
                CREATE TABLE Payments (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    BookingId INTEGER NOT NULL,
                    TransactionCode TEXT NOT NULL,
                    PaymentMethod INTEGER NOT NULL,
                    Amount NUMERIC NOT NULL,
                    Status INTEGER NOT NULL,
                    PaidAt TEXT NOT NULL,
                    ProviderReference TEXT NOT NULL,
                    Note TEXT NOT NULL);
                CREATE TABLE Tickets (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    BookingId INTEGER NOT NULL,
                    ShowtimeId INTEGER NOT NULL,
                    SeatId INTEGER NOT NULL,
                    Price NUMERIC NOT NULL,
                    Status TEXT NOT NULL,
                    QrCode TEXT NOT NULL);
                CREATE TABLE BookingProducts (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    BookingId INTEGER NOT NULL,
                    ProductId INTEGER NOT NULL,
                    Qty INTEGER NOT NULL,
                    UnitPrice NUMERIC NOT NULL);
                CREATE TABLE Promotions (
                    Id INTEGER PRIMARY KEY,
                    IsActive INTEGER NOT NULL,
                    UsageLimit INTEGER NOT NULL,
                    UsedCount INTEGER NOT NULL,
                    StartDate TEXT NOT NULL,
                    EndDate TEXT NOT NULL);
                """);
            connection.Execute(
                """
                INSERT INTO Customers(Id) VALUES (7);
                INSERT INTO Rooms(Id, IsActive) VALUES (5, 1);
                INSERT INTO Showtimes(Id, RoomId, IsActive, StartTime)
                    VALUES (11, 5, 1, @startTime);
                INSERT INTO Seats(Id, RoomId, SeatType) VALUES (21, 5, @normalSeat);
                INSERT INTO SeatHolds(
                    Id, ShowtimeId, SeatId, CustomerId, SessionId, IsReleased, ExpiresAt)
                    VALUES (51, 11, 21, 7, 'checkout-session', 0, @expiresAt);
                INSERT INTO Promotions(
                    Id, IsActive, UsageLimit, UsedCount, StartDate, EndDate)
                    VALUES (41, 0, 1, 0, @startDate, @endDate);
                """,
                new
                {
                    startTime = DateTime.Now.AddDays(1).ToString("O"),
                    expiresAt = DateTime.Now.AddMinutes(10).ToString("O"),
                    normalSeat = (int)SeatType.Normal,
                    startDate = DateTime.Now.AddDays(-2).ToString("O"),
                    endDate = DateTime.Now.AddDays(-1).ToString("O")
                });
        }

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(_directory, recursive: true);
        }
    }
}
