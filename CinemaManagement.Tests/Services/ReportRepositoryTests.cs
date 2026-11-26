using Dapper;
using Microsoft.Data.Sqlite;
using CinemaManagement.DAL.Database;
using CinemaManagement.DAL.Repositories;
using CinemaManagement.BLL.Services;
using CinemaManagement.BLL.Session;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;
using CinemaManagement.Core.DTOs;

namespace CinemaManagement.Tests.Services;

[TestFixture]
public sealed class ReportRepositoryTests
{
    private SqliteConnection _anchor = null!;
    private ReportRepository _repository = null!;
    private SqliteConnectionFactory _factory = null!;
    private DateTime _showtime;

    [SetUp]
    public void SetUp()
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = $"report-tests-{Guid.NewGuid():N}",
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared
        }.ToString();

        _anchor = new SqliteConnection(connectionString);
        _anchor.Open();
        _anchor.Execute(
            """
            CREATE TABLE Movies (Id INTEGER PRIMARY KEY, Title TEXT NOT NULL);
            CREATE TABLE Rooms (Id INTEGER PRIMARY KEY, Name TEXT NOT NULL);
            CREATE TABLE Seats (
                Id INTEGER PRIMARY KEY, RoomId INTEGER NOT NULL, SeatType INTEGER NOT NULL);
            CREATE TABLE Showtimes (
                Id INTEGER PRIMARY KEY, MovieId INTEGER NOT NULL, RoomId INTEGER NOT NULL,
                StartTime TEXT NOT NULL, EndTime TEXT NOT NULL, IsActive INTEGER NOT NULL);
            CREATE TABLE Bookings (
                Id INTEGER PRIMARY KEY, Status INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL, Total NUMERIC NOT NULL);
            CREATE TABLE Tickets (
                Id INTEGER PRIMARY KEY, BookingId INTEGER NOT NULL, ShowtimeId INTEGER NOT NULL,
                SeatId INTEGER NOT NULL, Price NUMERIC NOT NULL, Status TEXT NOT NULL);
            CREATE TABLE BookingProducts (
                Id INTEGER PRIMARY KEY, BookingId INTEGER NOT NULL,
                Qty INTEGER NOT NULL, UnitPrice NUMERIC NOT NULL);
            INSERT INTO Movies VALUES (1, 'Test Movie');
            INSERT INTO Rooms VALUES (1, 'Room 1');
            INSERT INTO Seats VALUES (1, 1, 0), (2, 1, 1), (3, 1, 3);
            """);

        _showtime = DateTime.Now.AddHours(1);
        _anchor.Execute(
            "INSERT INTO Showtimes VALUES (1, 1, 1, @StartTime, @EndTime, 1);",
            new
            {
                StartTime = _showtime.ToString("O"),
                EndTime = _showtime.AddHours(2).ToString("O")
            });
        _anchor.Execute(
            """
            INSERT INTO Bookings VALUES (1, 1, @CreatedAt, 120);
            INSERT INTO Tickets VALUES
                (1, 1, 1, 1, 70, 'Active'),
                (2, 1, 1, 2, 50, 'Active');
            INSERT INTO BookingProducts VALUES (1, 1, 2, 15);
            """,
            new { CreatedAt = DateTime.Now.ToString("O") });

        _factory = SqliteConnectionFactory.FromConnectionString(connectionString);
        _repository = new ReportRepository(_factory);
    }

    [TearDown]
    public void TearDown() => _anchor.Dispose();

    [Test]
    public void GetOccupancyReport_ExcludesAislePositionsFromCapacity()
    {
        var report = _repository.GetOccupancyReport(_showtime, _showtime).Single();

        Assert.That(report.TotalSeats, Is.EqualTo(2));
        Assert.That(report.SoldSeats, Is.EqualTo(2));
        Assert.That(report.OccupancyRate, Is.EqualTo(100));
        Assert.That(report.RoomName, Is.EqualTo("Room 1"));
    }

    [Test]
    public void GetRevenueByMovie_AddsBookingProductsOncePerBooking()
    {
        var report = _repository.GetRevenueByMovie(_showtime, _showtime).Single();

        Assert.That(report.TicketRevenue, Is.EqualTo(120));
        Assert.That(report.ProductRevenue, Is.EqualTo(30));
        Assert.That(report.TotalRevenue, Is.EqualTo(150));
    }

    [Test]
    public void GetDashboardStats_ReturnsAccessibleTypedData()
    {
        var session = new CurrentUserSession();
        session.Set(new User
        {
            Id = 1,
            Username = "admin",
            Role = UserRole.Admin
        });
        var service = new ReportService(_repository, _factory, session);

        DashboardStatsDto stats = service.GetDashboardStats();

        Assert.That(stats.TicketsToday, Is.EqualTo(2));
        Assert.That(stats.RevenueToday, Is.EqualTo(120));
        Assert.That(stats.TopMovies.Single().MovieTitle, Is.EqualTo("Test Movie"));
        Assert.That(stats.RevenueTrend.Single().Revenue, Is.EqualTo(120));
        Assert.That(stats.UpcomingShowtimes.Single().ShowtimeId, Is.EqualTo(1));
    }

    [Test]
    public void RevenueByDay_MapsRowsToTypedDtos()
    {
        var row = _repository.RevenueByDay(DateTime.Today, DateTime.Today).Single();

        Assert.That(row.RevenueDate, Is.EqualTo(DateTime.Today.ToString("yyyy-MM-dd")));
        Assert.That(row.BookingCount, Is.EqualTo(1));
        Assert.That(row.Revenue, Is.EqualTo(120));
    }
}
