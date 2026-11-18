namespace CinemaManagement.Core.Entities;

public class Ticket
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public int ShowtimeId { get; set; }
    public int SeatId { get; set; }
    public decimal Price { get; set; }
    public string Status { get; set; } = "Active";
    public string QrCode { get; set; } = string.Empty;

    // Tương thích cách viết trong code cũ.
    public int ShowTimeId
    {
        get => ShowtimeId;
        set => ShowtimeId = value;
    }
}