using CinemaManagement.Core.Enums;

namespace CinemaManagement.Core.Entities;

public class PricingRule
{
    public int Id { get; set; }
    public SeatType SeatType { get; set; }
    public ScreenType ScreenType { get; set; }
    public string DayType { get; set; } = "All";
    public string TimeFrom { get; set; } = "00:00";
    public string TimeTo { get; set; } = "23:59";
    public decimal Multiplier { get; set; } = 1m;
}