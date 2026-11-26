namespace CinemaManagement.Core.DTOs;

public sealed class RevenueByDayDto
{
    public string RevenueDate { get; set; } = string.Empty;
    public int BookingCount { get; set; }
    public decimal Revenue { get; set; }
}
