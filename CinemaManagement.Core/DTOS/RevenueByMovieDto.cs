namespace CinemaManagement.Core.DTOs;

public class RevenueByMovieDto
{
    public int MovieId { get; set; }
    public string MovieTitle { get; set; } = string.Empty;

    // Thuộc tính B
    public int TotalShowtimes { get; set; }
    public int TotalTicketsSold { get; set; }

    // Thuộc tính A
    public int TicketCount { get; set; }
    public decimal TicketRevenue { get; set; }
    public decimal ProductRevenue { get; set; }

    // Có trong cả A và B
    public decimal TotalRevenue { get; set; }
}