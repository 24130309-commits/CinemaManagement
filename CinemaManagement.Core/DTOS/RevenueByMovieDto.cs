namespace CinemaManagement.Core.DTOs;

public class RevenueByMovieDto
{
    public int MovieId { get; set; }
    public string MovieTitle { get; set; } = string.Empty;

    public int TotalShowtimes { get; set; }
    public int TotalTicketsSold { get; set; }

    public int TicketCount { get; set; }

    public decimal TicketRevenue { get; set; }
    public decimal ProductRevenue { get; set; }
    public decimal TotalRevenue { get; set; }
}