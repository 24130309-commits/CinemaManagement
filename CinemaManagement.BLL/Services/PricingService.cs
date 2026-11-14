using System;
using System.Collections.Generic;
using System.Linq;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.BLL.Services
{
    public class PricingService : IPricingService
    {
        private readonly IPricingRuleRepository _pricingRuleRepository;
        private readonly IPromotionService _promotionService;

        public PricingService(IPricingRuleRepository pricingRuleRepository, IPromotionService promotionService)
        {
            _pricingRuleRepository = pricingRuleRepository;
            _promotionService = promotionService;
        }

        public IEnumerable<PricingRule> GetAllRules() => _pricingRuleRepository.GetAll();

        public void AddRule(PricingRule rule)
        {
            ValidateRule(rule);
            _pricingRuleRepository.Add(rule);
        }

        public void UpdateRule(PricingRule rule)
        {
            ValidateRule(rule);
            _pricingRuleRepository.Update(rule);
        }

        public void DeleteRule(int id) => _pricingRuleRepository.Delete(id);

        public decimal CalculateFinalTicketPrice(decimal basePrice, DateTime showTime, string seatType, string promoCode = null)
        {
            decimal totalPrice = basePrice;

            // 1. Xác định loại ngày (Cuối tuần: Thứ 7, Chủ Nhật)
            bool isWeekend = showTime.DayOfWeek == DayOfWeek.Saturday || showTime.DayOfWeek == DayOfWeek.Sunday;
            string dayType = isWeekend ? "Weekend" : "Weekday";

            // 2. Cộng phụ thu theo quy tắc giá (Ngày + Loại ghế)
            var rules = _pricingRuleRepository.GetByDayAndSeatType(dayType, seatType);
            if (rules != null && rules.Any())
            {
                totalPrice += rules.Sum(r => r.ExtraFee);
            }

            // 3. Áp dụng mã khuyến mãi (nếu có nhập)
            if (!string.IsNullOrWhiteSpace(promoCode))
            {
                var promo = _promotionService.ValidateAndGetPromotion(promoCode);
                decimal discountAmount = totalPrice * (promo.DiscountPercent / 100m);

                if (promo.MaxDiscountAmount > 0 && discountAmount > promo.MaxDiscountAmount)
                {
                    discountAmount = promo.MaxDiscountAmount;
                }

                totalPrice -= discountAmount;
            }

            return totalPrice < 0 ? 0 : totalPrice;
        }

        private void ValidateRule(PricingRule rule)
        {
            if (rule == null)
                throw new BusinessException("Quy tắc giá không hợp lệ.");
            if (string.IsNullOrWhiteSpace(rule.Name))
                throw new BusinessException("Tên quy tắc giá không được để trống.");
            if (rule.ExtraFee < 0)
                throw new BusinessException("Phụ thu không được âm.");
        }
    }
}