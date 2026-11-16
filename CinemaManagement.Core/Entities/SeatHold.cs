namespace CinemaManagement.Core.Entities;

public class SeatHold
{
    public int Id { get; set; }
    public int ShowtimeId { get; set; }
    public int SeatId { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }

    // Các thuộc tính tương thích với code cũ.
    public DateTime ExpiryTime
    {
        get => ExpiresAt;
        set => ExpiresAt = value;
    }

    public DateTime HoldTime { get; set; }
    public int CustomerId { get; set; }
    public bool IsReleased { get; set; }
}