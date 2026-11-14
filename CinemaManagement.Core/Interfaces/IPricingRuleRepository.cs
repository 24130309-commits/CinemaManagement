using System.Collections.Generic;
using CinemaManagement.Core.Entities;

namespace CinemaManagement.Core.Interfaces
{
    public interface IPricingRuleRepository : IRepository<PricingRule>
    {
        IEnumerable<PricingRule> GetByDayAndSeatType(string dayType, string seatType);
    }
}