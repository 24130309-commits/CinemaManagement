namespace CinemaManagement.Core.DTOS;

public class RevenueByMovieDto
{
    public int MovieId { get; set; }
    public string MovieTitle { get; set; } = string.Empty;
    public int TicketCount { get; set; }
    public decimal TicketRevenue { get; set; }
    public decimal ProductRevenue { get; set; }
    public decimal TotalRevenue { get; set; }
}