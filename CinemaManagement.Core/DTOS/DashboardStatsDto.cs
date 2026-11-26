namespace CinemaManagement.Core.DTOs;

public sealed class DashboardStatsDto
{
    public int TicketsToday { get; init; }
    public decimal RevenueToday { get; init; }
    public decimal RevenueLast7Days { get; init; }
    public decimal RevenueLast30Days { get; init; }
    public IReadOnlyList<RevenueByMovieDto> TopMovies { get; init; } = [];
    public IReadOnlyList<RevenueByDayDto> RevenueTrend { get; init; } = [];
    public IReadOnlyList<UpcomingShowtimeDto> UpcomingShowtimes { get; init; } = [];
}
