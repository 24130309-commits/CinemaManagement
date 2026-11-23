
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
        _factory = factory;
    }

    private static object Range(DateTime from, DateTime to)
    {
        return new
        {
            From = from.Date.ToString("yyyy-MM-dd HH:mm:ss"),
            To = to.Date.AddDays(1).ToString("yyyy-MM-dd HH:mm:ss")
        };
    }

    public IEnumerable<RevenueByMovieDto> GetRevenueByMovie(
        DateTime fromDate,
        DateTime toDate)
    {
        using var connection = _factory.CreateConnection();

        const string sql = """
            SELECT
                m.Id AS MovieId,
                m.Title AS MovieTitle,
                COUNT(DISTINCT s.Id) AS TotalShowtimes,
                COUNT(t.Id) AS TotalTicketsSold,
                COUNT(t.Id) AS TicketCount,
                COALESCE(SUM(t.Price), 0) AS TicketRevenue,
                0 AS ProductRevenue,
                COALESCE(SUM(t.Price), 0) AS TotalRevenue
            FROM Movies m
            JOIN Showtimes s ON s.MovieId = m.Id
            LEFT JOIN Tickets t ON t.ShowtimeId = s.Id
            LEFT JOIN Bookings b
                ON b.Id = t.BookingId
                AND b.Status = 1
            WHERE datetime(s.StartTime) >= datetime(@From)
              AND datetime(s.StartTime) < datetime(@To)
              AND (t.Id IS NULL OR b.Id IS NOT NULL)
            GROUP BY m.Id, m.Title
            ORDER BY TotalRevenue DESC;
            """;

        return connection.Query<RevenueByMovieDto>(
            sql,
            Range(fromDate, toDate)).AsList();
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
                s.StartTime AS Date,
                s.StartTime AS StartTime,

                (
                    SELECT COUNT(*)
                    FROM Seats se
                    WHERE se.RoomId = s.RoomId
                ) AS TotalSeats,

                (
                    SELECT COUNT(DISTINCT t.SeatId)
                    FROM Tickets t
                    INNER JOIN Bookings b ON b.Id = t.BookingId
                    WHERE t.ShowtimeId = s.Id
                      AND b.Status = 1
                ) AS SoldSeats,

                (
                    SELECT COALESCE(SUM(t.Price), 0)
                    FROM Tickets t
                    INNER JOIN Bookings b ON b.Id = t.BookingId
                    WHERE t.ShowtimeId = s.Id
                      AND b.Status = 1
                ) AS TotalPrice

            FROM Showtimes s
            INNER JOIN Movies m ON m.Id = s.MovieId
            WHERE datetime(s.StartTime) >= datetime(@From)
              AND datetime(s.StartTime) < datetime(@To)
            ORDER BY s.StartTime;
            """;

        return connection.Query<OccupancyDto>(
            sql,
            Range(fromDate, toDate)).AsList();
    }

    public IEnumerable<OccupancyDto> OccupancyByShowtime(
        DateTime from,
        DateTime to)
    {
        return GetOccupancyReport(from, to);
    }

    public IEnumerable<dynamic> RevenueByDay(
        DateTime from,
        DateTime to)
    {
        using var connection = _factory.CreateConnection();

        const string sql = """
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
            """;

        return connection.Query(sql, Range(from, to)).AsList();
    }

    public IEnumerable<RevenueByMovieDto> TopMovies(int n)
    {
        using var connection = _factory.CreateConnection();

        const string sql = """
            SELECT
                m.Id AS MovieId,
                m.Title AS MovieTitle,
                COUNT(DISTINCT s.Id) AS TotalShowtimes,
                COUNT(t.Id) AS TotalTicketsSold,
                COUNT(t.Id) AS TicketCount,
                COALESCE(SUM(t.Price), 0) AS TicketRevenue,
                0 AS ProductRevenue,
                COALESCE(SUM(t.Price), 0) AS TotalRevenue
            FROM Movies m
            INNER JOIN Showtimes s ON s.MovieId = m.Id
            INNER JOIN Tickets t ON t.ShowtimeId = s.Id
            INNER JOIN Bookings b
                ON b.Id = t.BookingId
                AND b.Status = 1
            GROUP BY m.Id, m.Title
            ORDER BY TicketCount DESC
            LIMIT @Limit;
            """;

        return connection.Query<RevenueByMovieDto>(
            sql,
            new { Limit = Math.Max(1, n) }).AsList();
    }
}
