namespace CinemaManagement.Core.DTOs;

public class OccupancyDto
{
    public int ShowtimeId { get; set; }
    public string MovieTitle { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;

    public DateTime Date { get; set; }
    public DateTime StartTime { get; set; }

    public decimal TotalPrice { get; set; }

    public int SoldSeats { get; set; }
    public int TotalSeats { get; set; }

    public double OccupancyRate =>
        TotalSeats > 0
            ? Math.Round((double)SoldSeats / TotalSeats * 100, 2)
            : 0;
}