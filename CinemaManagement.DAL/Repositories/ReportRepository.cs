
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
            from = from.Date.ToString("O"),
            to = to.Date.AddDays(1).ToString("O")
        };
    }

    public IEnumerable<RevenueByMovieDto> RevenueByMovie(
        DateTime from,
        DateTime to)
    {
        using var c = _factory.CreateConnection();

        const string sql = @"
            SELECT MovieId,
                   MovieTitle,
                   SUM(TicketCount) AS TicketCount,
                   SUM(TicketRevenue) AS TicketRevenue,
                   SUM(ProductRevenue) AS ProductRevenue,
                   SUM(TicketRevenue) + SUM(ProductRevenue) AS TotalRevenue
            FROM
            (
                SELECT m.Id AS MovieId,
                       m.Title AS MovieTitle,
                       b.Id AS BookingId,
                       COUNT(t.Id) AS TicketCount,
                       COALESCE(SUM(t.Price), 0) AS TicketRevenue,
                       COALESCE(p.ProductRevenue, 0) AS ProductRevenue
                FROM Movies m
                JOIN Showtimes s ON s.MovieId = m.Id
                JOIN Tickets t ON t.ShowtimeId = s.Id
                JOIN Bookings b
                    ON b.Id = t.BookingId
                    AND b.Status = 1
                LEFT JOIN
                (
                    SELECT bp.BookingId,
                           SUM(bp.Qty * bp.UnitPrice) AS ProductRevenue
                    FROM BookingProducts bp
                    GROUP BY bp.BookingId
                ) p ON p.BookingId = b.Id
                WHERE t.Status = 'Active'
                  AND datetime(b.CreatedAt) >= datetime(@from)
                  AND datetime(b.CreatedAt) < datetime(@to)
                GROUP BY m.Id, m.Title, b.Id, p.ProductRevenue
            ) RevenueByBooking
            GROUP BY MovieId, MovieTitle
            ORDER BY TotalRevenue DESC";

        return c.Query<RevenueByMovieDto>(
            sql,
            Range(from, to)).AsList();
    }

    public IEnumerable<OccupancyDto> OccupancyByShowtime(
        DateTime from,
        DateTime to)
    {
        using var c = _factory.CreateConnection();

        const string sql = @"
            SELECT
                s.Id AS ShowtimeId,
                m.Title AS MovieTitle,
                s.StartTime AS [Date],

                (
                    SELECT COUNT(*)
                    FROM Seats se
                    WHERE se.RoomId = s.RoomId
                ) AS TotalSeats,

                (
                    SELECT COUNT(DISTINCT t.SeatId)
                    FROM Tickets t
                    JOIN Bookings b ON b.Id = t.BookingId
                    WHERE t.ShowtimeId = s.Id
                      AND t.Status = 'Active'
                      AND b.Status = 1
                ) AS SoldSeats,

                (
                    SELECT COALESCE(SUM(t.Price), 0)
                    FROM Tickets t
                    JOIN Bookings b ON b.Id = t.BookingId
                    WHERE t.ShowtimeId = s.Id
                      AND t.Status = 'Active'
                      AND b.Status = 1
                ) AS TotalPrice

            FROM Showtimes s
            JOIN Movies m ON m.Id = s.MovieId

            WHERE datetime(s.StartTime) >= datetime(@from)
              AND datetime(s.StartTime) < datetime(@to)

            ORDER BY s.StartTime";

        return c.Query<OccupancyDto>(
            sql,
            Range(from, to)).AsList();
    }

    public IEnumerable<dynamic> RevenueByDay(
        DateTime from,
        DateTime to)
    {
        using var c = _factory.CreateConnection();

        const string sql = @"
            SELECT
                substr(CreatedAt, 1, 10) AS RevenueDate,
                COUNT(*) AS BookingCount,
                COALESCE(SUM(Total), 0) AS Revenue
            FROM Bookings
            WHERE Status = 1
              AND datetime(CreatedAt) >= datetime(@from)
              AND datetime(CreatedAt) < datetime(@to)
            GROUP BY substr(CreatedAt, 1, 10)
            ORDER BY RevenueDate";

        return c.Query(
            sql,
            Range(from, to)).AsList();
    }

    public IEnumerable<RevenueByMovieDto> TopMovies(int n)
    {
        using var c = _factory.CreateConnection();

        const string sql = @"
            SELECT
                m.Id AS MovieId,
                m.Title AS MovieTitle,
                COUNT(t.Id) AS TicketCount,
                COALESCE(SUM(t.Price), 0) AS TicketRevenue,
                0 AS ProductRevenue,
                COALESCE(SUM(t.Price), 0) AS TotalRevenue
            FROM Movies m
            JOIN Showtimes s ON s.MovieId = m.Id
            JOIN Tickets t ON t.ShowtimeId = s.Id
            JOIN Bookings b
                ON b.Id = t.BookingId
                AND b.Status = 1
            WHERE t.Status = 'Active'
            GROUP BY m.Id, m.Title
            ORDER BY TicketCount DESC
            LIMIT @n";

        return c.Query<RevenueByMovieDto>(
            sql,
            new { n = Math.Max(1, n) }).AsList();
    }
}
