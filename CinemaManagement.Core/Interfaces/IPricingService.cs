using System;
using System.Collections.Generic;
using CinemaManagement.Core.Entities;

namespace CinemaManagement.Core.Interfaces
{
    public interface IPricingService
    {
        IEnumerable<PricingRule> GetAllRules();
        void AddRule(PricingRule rule);
        void UpdateRule(PricingRule rule);
        void DeleteRule(int id);
        decimal CalculateFinalTicketPrice(decimal basePrice, DateTime showTime, string seatType, string promoCode = null);
    }
}