using CinemaManagement.Core.Entities;

namespace CinemaManagement.Core.Interfaces;

public interface IPricingService
{
    IEnumerable<PricingRule> GetAllRules();

    void AddRule(PricingRule rule);

    void UpdateRule(PricingRule rule);

    void DeleteRule(int id);

    decimal CalculateTicketPrice(Showtime showtime, Seat seat);

    decimal CalculateFinalTicketPrice(
        decimal basePrice,
        DateTime showTime,
        string seatType,
        string? promoCode = null);
}