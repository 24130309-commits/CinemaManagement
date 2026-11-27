using Dapper;
using CinemaManagement.Core.DTOs;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.DAL.Database;

namespace CinemaManagement.DAL.Repositories;

public sealed class ReportRepository : IReportRepository
{
    private readonly SqliteConnectionFactory _factory;

    public ReportRepository(SqliteConnectionFactory factory)
    {
        _factory = factory
            ?? throw new ArgumentNullException(nameof(factory));
    }

    private static object Range(DateTime from, DateTime to) => new
    {
        // Keep the local offset: booking timestamps are persisted with "O"
        // and SQLite normalizes offset timestamps to UTC in datetime().
        From = from.Date.ToString("O"),
        To = to.Date.AddDays(1).ToString("O")
    };

    public IEnumerable<RevenueByMovieDto> GetRevenueByMovie(
        DateTime fromDate,
        DateTime toDate)
    {
        using var connection = _factory.CreateConnection();

        const string sql = """
            WITH TicketRevenue AS
            (
                SELECT
                    t.ShowtimeId,
                    t.BookingId,
                    COUNT(t.Id) AS TicketCount,
                    SUM(t.Price) AS TicketRevenue
                FROM Tickets t
                INNER JOIN Bookings b ON b.Id = t.BookingId
                WHERE b.Status = 1
                  AND t.Status = 'Active'
                GROUP BY t.ShowtimeId, t.BookingId
            ),
            ProductRevenue AS
            (
                SELECT
                    BookingId,
                    SUM(Qty * UnitPrice) AS ProductRevenue
                FROM BookingProducts
                GROUP BY BookingId
            )
            SELECT
                m.Id AS MovieId,
                m.Title AS MovieTitle,
                COUNT(DISTINCT s.Id) AS TotalShowtimes,
                COALESCE(SUM(tr.TicketCount), 0) AS TotalTicketsSold,
                COALESCE(SUM(tr.TicketCount), 0) AS TicketCount,
                COALESCE(SUM(tr.TicketRevenue), 0) AS TicketRevenue,
                COALESCE(SUM(pr.ProductRevenue), 0) AS ProductRevenue,
                COALESCE(SUM(tr.TicketRevenue), 0)
                    + COALESCE(SUM(pr.ProductRevenue), 0)
                    AS TotalRevenue
            FROM Movies m
            INNER JOIN Showtimes s ON s.MovieId = m.Id
            LEFT JOIN TicketRevenue tr ON tr.ShowtimeId = s.Id
            LEFT JOIN ProductRevenue pr ON pr.BookingId = tr.BookingId
            WHERE datetime(s.StartTime) >= datetime(@From)
              AND datetime(s.StartTime) < datetime(@To)
            GROUP BY m.Id, m.Title
            ORDER BY TotalRevenue DESC;
            """;

        return connection
            .Query<RevenueByMovieDto>(
                sql,
                Range(fromDate, toDate))
            .AsList();
    }

    public IEnumerable<RevenueByMovieDto> RevenueByMovie(
        DateTime from,
        DateTime to)
    {
        return GetRevenueByMovie(from, to);
    }

    public IEnumerable<OccupancyDto> GetOccupancyReport(
        DateTime fromDate,
        DateTime toDate)
    {
        using var connection = _factory.CreateConnection();

        const string sql = """
            SELECT
                s.Id AS ShowtimeId,
                m.Title AS MovieTitle,
                r.Name AS RoomName,
                s.StartTime AS Date,
                s.StartTime AS StartTime,
                (SELECT COUNT(*)
                 FROM Seats se
                 WHERE se.RoomId = s.RoomId
                   AND se.SeatType <> 3) AS TotalSeats,
                (SELECT COUNT(DISTINCT t.SeatId)
                 FROM Tickets t
                 INNER JOIN Bookings b ON b.Id = t.BookingId
                 WHERE t.ShowtimeId = s.Id
                   AND t.Status = 'Active'
                   AND b.Status = 1) AS SoldSeats,
                (SELECT COALESCE(SUM(t.Price), 0)
                 FROM Tickets t
                 INNER JOIN Bookings b ON b.Id = t.BookingId
                 WHERE t.ShowtimeId = s.Id
                   AND t.Status = 'Active'
                   AND b.Status = 1) AS TotalPrice
            FROM Showtimes s
            INNER JOIN Movies m ON m.Id = s.MovieId
            INNER JOIN Rooms r ON r.Id = s.RoomId
            WHERE datetime(s.StartTime) >= datetime(@From)
              AND datetime(s.StartTime) < datetime(@To)
            ORDER BY s.StartTime;
            """;

        return connection
            .Query<OccupancyDto>(
                sql,
                Range(fromDate, toDate))
            .AsList();
    }

    public IEnumerable<OccupancyDto> OccupancyByShowtime(
        DateTime from,
        DateTime to)
    {
        return GetOccupancyReport(from, to);
    }

    public IEnumerable<RevenueByDayDto> RevenueByDay(
        DateTime from,
        DateTime to)
    {
        using var connection = _factory.CreateConnection();

        return connection.Query<RevenueByDayDto>(
            """
            SELECT
                substr(CreatedAt, 1, 10) AS RevenueDate,
                COUNT(*) AS BookingCount,
                COALESCE(SUM(Total), 0) AS Revenue
            FROM Bookings
            WHERE Status = 1
              AND datetime(CreatedAt) >= datetime(@From)
              AND datetime(CreatedAt) < datetime(@To)
            GROUP BY substr(CreatedAt, 1, 10)
            ORDER BY RevenueDate;
            """,
            Range(from, to)).AsList();
    }

    public IEnumerable<RevenueByMovieDto> TopMovies(int n)
    {
        using var connection = _factory.CreateConnection();

        return connection.Query<RevenueByMovieDto>(
            """
            WITH TicketRevenue AS
            (
                SELECT
                    t.ShowtimeId,
                    t.BookingId,
                    COUNT(t.Id) AS TicketCount,
                    SUM(t.Price) AS TicketRevenue
                FROM Tickets t
                INNER JOIN Bookings b ON b.Id = t.BookingId
                WHERE b.Status = 1
                  AND t.Status = 'Active'
                GROUP BY t.ShowtimeId, t.BookingId
            ),
            ProductRevenue AS
            (
                SELECT
                    BookingId,
                    SUM(Qty * UnitPrice) AS ProductRevenue
                FROM BookingProducts
                GROUP BY BookingId
            )
            SELECT
                m.Id AS MovieId,
                m.Title AS MovieTitle,
                COUNT(DISTINCT s.Id) AS TotalShowtimes,
                COALESCE(SUM(tr.TicketCount), 0) AS TotalTicketsSold,
                COALESCE(SUM(tr.TicketCount), 0) AS TicketCount,
                COALESCE(SUM(tr.TicketRevenue), 0) AS TicketRevenue,
                COALESCE(SUM(pr.ProductRevenue), 0) AS ProductRevenue,
                COALESCE(SUM(tr.TicketRevenue), 0)
                    + COALESCE(SUM(pr.ProductRevenue), 0)
                    AS TotalRevenue
            FROM Movies m
            INNER JOIN Showtimes s ON s.MovieId = m.Id
            INNER JOIN TicketRevenue tr ON tr.ShowtimeId = s.Id
            LEFT JOIN ProductRevenue pr ON pr.BookingId = tr.BookingId
            GROUP BY m.Id, m.Title
            ORDER BY TotalRevenue DESC
            LIMIT @Limit;
            """,
            new { Limit = Math.Max(1, n) })
            .AsList();
    }
}
