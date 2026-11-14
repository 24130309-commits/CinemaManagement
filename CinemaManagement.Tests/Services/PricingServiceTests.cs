using Xunit;
using System;
using System.Collections.Generic;
using CinemaManagement.BLL.Services;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.Tests.Services
{
    public class PricingServiceTests
    {
        [Fact]
        public void CalculateFinalTicketPrice_AppliesSeatExtraFeeAndPromoDiscount()
        {
            var ruleRepo = new StubPricingRuleRepository();
            var promoRepo = new StubPromotionRepository();
            var promoService = new PromotionService(promoRepo);
            var pricingService = new PricingService(ruleRepo, promoService);

            var saturday = new DateTime(2026, 11, 14, 20, 0, 0);
            decimal price = pricingService.CalculateFinalTicketPrice(80000m, saturday, "VIP", "VIP10");

            Assert.Equals(90000m, price);
        }

        private class StubPricingRuleRepository : IPricingRuleRepository
        {
            private readonly List<PricingRule> _rules = new List<PricingRule>
            {
                new PricingRule { DayType = "Weekend", SeatType = "VIP", ExtraFee = 20000m }
            };

            public IEnumerable<PricingRule> GetAll() => _rules;
            public PricingRule GetById(int id) => null;
            public void Add(PricingRule entity) { }
            public void Update(PricingRule entity) { }
            public void Delete(int id) { }
            public IEnumerable<PricingRule> GetByDayAndSeatType(string dayType, string seatType) => _rules;
            public PricingRule GetRule(string dayType, string seatType) => _rules[0];
            public IEnumerable<PricingRule> GetByDayType(string dayType) => _rules;
        }

        private class StubPromotionRepository : IPromotionRepository
        {
            public Promotion GetByCode(string code) => new Promotion
            {
                Code = "VIP10",
                DiscountPercent = 10,
                MaxDiscountAmount = 50000m,
                StartDate = DateTime.Now.AddDays(-1),
                EndDate = DateTime.Now.AddDays(5),
                IsActive = true
            };
            public bool IsCodeExists(string code, int? excludeId = null) => false;
            public IEnumerable<Promotion> GetAll() => new List<Promotion>();
            public Promotion GetById(int id) => null;
            public void Add(Promotion entity) { }
            public void Update(Promotion entity) { }
            public void Delete(int id) { }
        }
    }
}