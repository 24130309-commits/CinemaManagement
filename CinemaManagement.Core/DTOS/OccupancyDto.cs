using System;

namespace CinemaManagement.Core.DTOs;

public class OccupancyDto
{
   public int ShowtimeIdƠ { get; set; }
   public string MovieTitle { get; set; } = string.Empty;
   public DateTime Date { get; set; }
   public int TotalSeats { get; set; }
   public int SoldSeats { get; set; }
   public decimal TotalPrice { get; set; }
}