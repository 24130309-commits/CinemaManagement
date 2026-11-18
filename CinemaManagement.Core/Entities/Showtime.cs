namespace CinemaManagement.Core.Entities;

public class Showtime
{
    public int Id { get; set; }
    public int MovieId { get; set; }
    public int RoomId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public decimal BasePrice { get; set; }
    public bool IsActive { get; set; } = true;

    public string MovieTitle { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
}