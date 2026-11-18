namespace CinemaManagement.Core.DTOS;

public class PriceBreakdownDto
{
    public decimal BasePrice { get; set; }
    public decimal SeatMultiplier { get; set; } = 1m;
    public decimal ScreenMultiplier { get; set; } = 1m;
    public decimal DayMultiplier { get; set; } = 1m;
    public decimal TimeMultiplier { get; set; } = 1m;
    public decimal FinalPrice { get; set; }

    public decimal TotalMultiplier =>
        SeatMultiplier *
        ScreenMultiplier *
        DayMultiplier *
        TimeMultiplier;
}