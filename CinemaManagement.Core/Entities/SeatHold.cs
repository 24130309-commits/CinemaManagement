namespace CinemaManagement.Core.Entities;

public class SeatHold
{
    public int Id { get; set; }

    public int ShowtimeId { get; set; }

    public int SeatId { get; set; }

    public int CustomerId { get; set; }

    public string SessionId { get; set; } = string.Empty;

    public DateTime HoldTime { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime ExpiryTime
    {
        get => ExpiresAt;
        set => ExpiresAt = value;
    }

    public bool IsReleased { get; set; }
}