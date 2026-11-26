namespace CinemaManagement.Core.DTOs;

public sealed class UpcomingShowtimeDto
{
    public int ShowtimeId { get; set; }
    public string MovieTitle { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public int SeatsLeft { get; set; }
}
