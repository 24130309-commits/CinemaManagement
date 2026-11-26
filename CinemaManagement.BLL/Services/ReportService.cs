using Dapper;
using CinemaManagement.Core.DTOs;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.BLL.Session;
using CinemaManagement.DAL.Database;

namespace CinemaManagement.BLL.Services;

public sealed class ReportService : IReportService
{
    private readonly IReportRepository _reports;
    private readonly SqliteConnectionFactory _factory;
    private readonly CurrentUserSession _session;

    public ReportService(
        IReportRepository reports,
        SqliteConnectionFactory factory,
        CurrentUserSession session)
    {
        _reports = reports ?? throw new ArgumentNullException(nameof(reports));
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    public IEnumerable<RevenueByMovieDto> GetRevenueByMovie(
        DateTime from,
        DateTime to)
    {
        AuthorizationGuard.EnsureAuthenticated(_session);
        return ValidateRange(from, to, _reports.RevenueByMovie);
    }

    public IEnumerable<OccupancyDto> GetOccupancyReport(
        DateTime from,
        DateTime to)
    {
        AuthorizationGuard.EnsureAuthenticated(_session);
        return ValidateRange(from, to, _reports.OccupancyByShowtime);
    }

    public IEnumerable<RevenueByDayDto> GetRevenueByDay(
        DateTime from,
        DateTime to)
    {
        AuthorizationGuard.EnsureAuthenticated(_session);
        return ValidateRange(from, to, _reports.RevenueByDay);
    }

    public DashboardStatsDto GetDashboardStats()
    {
        AuthorizationGuard.EnsureAuthenticated(_session);
        var today = DateTime.Today;

        using var c = _factory.CreateConnection();

        var tickets = c.ExecuteScalar<int>(
            @"SELECT COUNT(*)
              FROM Tickets t
              JOIN Bookings b ON b.Id = t.BookingId
              WHERE b.Status = 1
                AND t.Status = 'Active'
                AND datetime(b.CreatedAt) >= datetime(@from)
                AND datetime(b.CreatedAt) < datetime(@to)",
            new
            {
                from = today.ToString("O"),
                to = today.AddDays(1).ToString("O")
            });

        var revenue = c.ExecuteScalar<decimal>(
            @"SELECT COALESCE(SUM(Total), 0)
              FROM Bookings
              WHERE Status = 1
                AND datetime(CreatedAt) >= datetime(@from)
                AND datetime(CreatedAt) < datetime(@to)",
            new
            {
                from = today.ToString("O"),
                to = today.AddDays(1).ToString("O")
            });

        var last7 = c.ExecuteScalar<decimal>(
            @"SELECT COALESCE(SUM(Total), 0)
              FROM Bookings
              WHERE Status = 1
                AND datetime(CreatedAt) >= datetime(@from)
                AND datetime(CreatedAt) < datetime(@to)",
            new
            {
                from = today.AddDays(-6).ToString("O"),
                to = today.AddDays(1).ToString("O")
            });

        var last30 = c.ExecuteScalar<decimal>(
            @"SELECT COALESCE(SUM(Total), 0)
              FROM Bookings
              WHERE Status = 1
                AND datetime(CreatedAt) >= datetime(@from)
                AND datetime(CreatedAt) < datetime(@to)",
            new
            {
                from = today.AddDays(-29).ToString("O"),
                to = today.AddDays(1).ToString("O")
            });

        var trend = _reports
            .RevenueByDay(today.AddDays(-6), today)
            .ToList();

        var upcoming = c.Query<UpcomingShowtimeDto>(
            @"SELECT s.Id AS ShowtimeId,
                     m.Title AS MovieTitle,
                     r.Name AS RoomName,
                     s.StartTime,
                     s.EndTime,
                     (SELECT COUNT(*)
                      FROM Seats se
                      WHERE se.RoomId = s.RoomId
                        AND se.SeatType <> 3)
                     -
                     (SELECT COUNT(DISTINCT t.SeatId)
                      FROM Tickets t
                      JOIN Bookings b ON b.Id = t.BookingId
                      WHERE t.ShowtimeId = s.Id
                        AND t.Status = 'Active'
                        AND b.Status = 1) AS SeatsLeft
              FROM Showtimes s
              JOIN Movies m ON m.Id = s.MovieId
              JOIN Rooms r ON r.Id = s.RoomId
              WHERE s.IsActive = 1
                AND datetime(s.StartTime) >= datetime(@now)
              ORDER BY s.StartTime
              LIMIT 8",
            new
            {
                now = DateTime.Now.ToString("O")
            })
            .AsList();

        return new DashboardStatsDto
        {
            TicketsToday = tickets,
            RevenueToday = revenue,
            RevenueLast7Days = last7,
            RevenueLast30Days = last30,
            TopMovies = _reports.TopMovies(5).ToList(),
            RevenueTrend = trend,
            UpcomingShowtimes = upcoming
        };
    }

    private static IEnumerable<T> ValidateRange<T>(
        DateTime from,
        DateTime to,
        Func<DateTime, DateTime, IEnumerable<T>> query)
    {
        if (from.Date > to.Date)
        {
            throw new ArgumentException(
                "Ngày bắt đầu không được sau ngày kết thúc.");
        }

        return query(from, to);
    }
}
