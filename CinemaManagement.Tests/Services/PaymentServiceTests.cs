using CinemaManagement.BLL.Services;
using CinemaManagement.BLL.Session;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.DAL.Database;
using CinemaManagement.DAL.Repositories;
using Dapper;
using Microsoft.Data.Sqlite;

namespace CinemaManagement.Tests.Services;

[TestFixture]
public sealed class PaymentServiceTests
{
    [Test]
    public void RefundBooking_RejectsStaffBeforeRepositoryWrite()
    {
        var repository = new FakePaymentRepository();
        var service = new PaymentService(repository, Session(UserRole.Staff));

        Assert.Throws<BusinessException>(() => service.RefundBooking(7, "duplicate"));
        Assert.That(repository.RefundCount, Is.Zero);
    }

    [Test]
    public void RefundBooking_RecordsAuditEntryAfterSuccessfulRefund()
    {
        var repository = new FakePaymentRepository { RefundResult = true };
        var audit = new FakeAuditService();
        var service = new PaymentService(repository, Session(UserRole.Admin), audit);

        Assert.That(service.RefundBooking(7, "  duplicate charge  "), Is.True);

        Assert.That(repository.RefundCount, Is.EqualTo(1));
        Assert.That(repository.LastNote, Is.EqualTo("duplicate charge"));
        Assert.That(audit.Action, Is.EqualTo("Refund booking"));
        Assert.That(audit.EntityName, Is.EqualTo("Bookings"));
        Assert.That(audit.EntityId, Is.EqualTo(7));
        Assert.That(audit.Detail, Is.EqualTo("duplicate charge"));
    }

    [Test]
    public void RepositoryRefund_RejectsPaymentTotalThatDiffersFromBooking()
    {
        using var database = new TemporaryPaymentDatabase();
        database.Seed(paymentAmount: 99m);
        var repository = new PaymentRepository(new SqliteConnectionFactory(database.Path));

        Assert.That(repository.RefundBooking(7, "test"), Is.False);

        using var connection = new SqliteConnectionFactory(database.Path).CreateConnection();
        Assert.Multiple(() =>
        {
            Assert.That(connection.QuerySingle<int>("SELECT Status FROM Bookings WHERE Id = 7"), Is.EqualTo((int)BookingStatus.Paid));
            Assert.That(connection.QuerySingle<int>("SELECT Status FROM Payments WHERE BookingId = 7"), Is.EqualTo((int)PaymentStatus.Success));
        });
    }

    [Test]
    public void RepositoryRefund_UpdatesPaymentBookingAndTicketsTogether()
    {
        using var database = new TemporaryPaymentDatabase();
        database.Seed(paymentAmount: 100m);
        var repository = new PaymentRepository(new SqliteConnectionFactory(database.Path));

        Assert.That(repository.RefundBooking(7, "test"), Is.True);

        using var connection = new SqliteConnectionFactory(database.Path).CreateConnection();
        Assert.Multiple(() =>
        {
            Assert.That(connection.QuerySingle<int>("SELECT Status FROM Bookings WHERE Id = 7"), Is.EqualTo((int)BookingStatus.Refunded));
            Assert.That(connection.QuerySingle<int>("SELECT Status FROM Payments WHERE BookingId = 7"), Is.EqualTo((int)PaymentStatus.Refunded));
            Assert.That(connection.QuerySingle<string>("SELECT Status FROM Tickets WHERE BookingId = 7"), Is.EqualTo("Refunded"));
        });
    }

    private static CurrentUserSession Session(UserRole role)
    {
        var session = new CurrentUserSession();
        session.Set(new User { Id = 9, Username = "operator", Role = role });
        return session;
    }

    private sealed class FakePaymentRepository : IPaymentRepository
    {
        public bool RefundResult { get; init; }
        public int RefundCount { get; private set; }
        public string LastNote { get; private set; } = string.Empty;
        public List<Payment> GetByBookingId(int bookingId) => [];
        public Payment? GetByTransactionCode(string transactionCode) => null;
        public bool RefundBooking(int bookingId, string note)
        {
            RefundCount++;
            LastNote = note;
            return RefundResult;
        }
    }

    private sealed class FakeAuditService : IAuditService
    {
        public string Action { get; private set; } = string.Empty;
        public string EntityName { get; private set; } = string.Empty;
        public int? EntityId { get; private set; }
        public string Detail { get; private set; } = string.Empty;
        public void Log(string action, string entityName, int? entityId, string details)
        {
            Action = action;
            EntityName = entityName;
            EntityId = entityId;
            Detail = details;
        }
        public IEnumerable<AuditLog> Search(int userId, DateTime from, DateTime to) => [];
        public IEnumerable<AuditLog> GetRecentLogs(int limit = 100) => [];
    }

    private sealed class TemporaryPaymentDatabase : IDisposable
    {
        private readonly string _directory = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), $"cinema-payment-tests-{Guid.NewGuid():N}");

        public string Path { get; }

        public TemporaryPaymentDatabase()
        {
            Directory.CreateDirectory(_directory);
            Path = System.IO.Path.Combine(_directory, "payments.db");
            using var connection = new SqliteConnectionFactory(Path).CreateConnection();
            connection.Execute("CREATE TABLE Bookings (Id INTEGER PRIMARY KEY, Status INTEGER NOT NULL, Total NUMERIC NOT NULL);");
            connection.Execute("CREATE TABLE Payments (Id INTEGER PRIMARY KEY, BookingId INTEGER NOT NULL, TransactionCode TEXT NOT NULL, PaymentMethod INTEGER NOT NULL, Amount NUMERIC NOT NULL, Status INTEGER NOT NULL, PaidAt TEXT NOT NULL, ProviderReference TEXT NOT NULL, Note TEXT NOT NULL);");
            connection.Execute("CREATE TABLE Tickets (Id INTEGER PRIMARY KEY, BookingId INTEGER NOT NULL, Status TEXT NOT NULL);");
        }

        public void Seed(decimal paymentAmount)
        {
            using var connection = new SqliteConnectionFactory(Path).CreateConnection();
            connection.Execute("INSERT INTO Bookings(Id, Status, Total) VALUES (7, @Status, 100);", new { Status = (int)BookingStatus.Paid });
            connection.Execute("INSERT INTO Payments(Id, BookingId, TransactionCode, PaymentMethod, Amount, Status, PaidAt, ProviderReference, Note) VALUES (1, 7, 'PAY7', 0, @Amount, @Status, '2026-01-01', '', '');", new { Amount = paymentAmount, Status = (int)PaymentStatus.Success });
            connection.Execute("INSERT INTO Tickets(Id, BookingId, Status) VALUES (1, 7, 'Active');");
        }

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, recursive: true);
        }
    }
}
