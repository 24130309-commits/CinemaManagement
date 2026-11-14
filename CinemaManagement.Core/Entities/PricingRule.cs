namespace CinemaManagement.Core.Entities
{
    public class PricingRule
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string DayType { get; set; } // Weekday, Weekend, Holiday
        public string SeatType { get; set; } // Standard, VIP, Couple
        public decimal ExtraFee { get; set; }
    }
}