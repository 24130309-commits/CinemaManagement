using CinemaManagement.Core.Enums;

namespace CinemaManagement.Core.Entities;

public class Seat
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public string RowLabel { get; set; } = string.Empty;
    public int ColNumber { get; set; }
    public SeatType SeatType { get; set; } = SeatType.Normal;

    public string SeatCode => $"{RowLabel}{ColNumber}";
}