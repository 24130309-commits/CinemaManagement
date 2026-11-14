namespace CinemaManagement.Core.Entities;

public class SeatHold
{
    public int Id { get; set; }
    public int SeatId { get; set; }
    public int CustomerId { get; set; }
    public int ShowtimeId { get; set; }
    public DateTime HoldTime { get; set; }
    public DateTime ExpiryTime { get; set; }
    public bool IsReleased { get; set; }
    
    
    
    
}