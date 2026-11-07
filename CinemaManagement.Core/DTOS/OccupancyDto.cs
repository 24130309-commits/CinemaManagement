using System;

namespace CinemaManagement.Core.DTOs;

public class OccupancyDto
{
    public int ShowtimeId { get; set; }
    public string MovieTitle { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public int TotalSeats { get; set; }
    public int SoldSeats { get; set; }
    public double OccupancyRate { get; set; }
}