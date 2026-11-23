using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;

namespace CinemaManagement.Core.Interfaces;

public interface IPricingRuleRepository : IRepository<PricingRule>
{
    IEnumerable<PricingRule> GetByDayAndSeatType(
        string dayType,
        string seatType);

    IEnumerable<PricingRule> GetBySeatAndScreenType(
        SeatType seatType,
        ScreenType screenType,
        string dayType);
}