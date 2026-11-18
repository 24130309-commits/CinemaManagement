using CinemaManagement.Core.Enums;

namespace CinemaManagement.Core.DTOS;

public class ShowtimeDisplayDto
{
    public int ShowtimeId { get; set; }
    public string MovieTitle { get; set; } = string.Empty;
    public string PosterPath { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public ScreenType ScreenType { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public decimal BasePrice { get; set; }
    public int SeatsLeft { get; set; }
}