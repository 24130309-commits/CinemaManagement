using CinemaManagement.Core.Enums;

namespace CinemaManagement.Core.DTOs;

public class SeatDisplayDto
{
    public int Id { get; set; }

    public string RowLabel { get; set; } = string.Empty;

    public int ColNumber { get; set; }

    public SeatType SeatType { get; set; }

    public SeatStatus Status { get; set; }

    public decimal Price { get; set; }

    public string RowName
    {
        get => RowLabel;
        set => RowLabel = value;
    }

    public int SeatNumber
    {
        get => ColNumber;
        set => ColNumber = value;
    }
}