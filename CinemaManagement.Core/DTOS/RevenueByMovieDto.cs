// CinemaManagement.Core/DTOs/RevenueByMovieDto.cs
namespace CinemaManagement.Core.DTOs
{
    public class RevenueByMovieDto
    {
        public int MovieId { get; set; }
        public string MovieTitle { get; set; }
        public int TotalShowtimes { get; set; }
        public int TotalTicketsSold { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
