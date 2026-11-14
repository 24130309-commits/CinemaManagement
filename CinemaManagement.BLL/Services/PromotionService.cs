using System;
using System.Collections.Generic;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.BLL.Services
{
    public class PromotionService : IPromotionService
    {
        private readonly IPromotionRepository _promotionRepository;

        public PromotionService(IPromotionRepository promotionRepository)
        {
            _promotionRepository = promotionRepository;
        }

        public IEnumerable<Promotion> GetAllPromotions() => _promotionRepository.GetAll();

        public Promotion ValidateAndGetPromotion(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new BusinessException("Vui lòng nhập mã khuyến mãi.");

            var promo = _promotionRepository.GetByCode(code.Trim().ToUpper());
            if (promo == null || !promo.IsActive)
                throw new BusinessException("Mã khuyến mãi không tồn tại hoặc đã bị khóa.");

            var now = DateTime.Now;
            if (now < promo.StartDate || now > promo.EndDate)
                throw new BusinessException("Mã khuyến mãi chưa đến hạn hoặc đã hết hạn sử dụng.");

            return promo;
        }

        public void AddPromotion(Promotion promotion)
        {
            ValidatePromotionData(promotion);
            promotion.Code = promotion.Code.Trim().ToUpper();

            if (_promotionRepository.IsCodeExists(promotion.Code))
                throw new BusinessException("Mã khuyến mãi này đã tồn tại.");

            _promotionRepository.Add(promotion);
        }

        public void UpdatePromotion(Promotion promotion)
        {
            ValidatePromotionData(promotion);
            promotion.Code = promotion.Code.Trim().ToUpper();

            if (_promotionRepository.IsCodeExists(promotion.Code, promotion.Id))
                throw new BusinessException("Mã khuyến mãi bị trùng với chương trình khác.");

            _promotionRepository.Update(promotion);
        }

        public void DeletePromotion(int id) => _promotionRepository.Delete(id);

        private void ValidatePromotionData(Promotion promo)
        {
            if (promo == null)
                throw new BusinessException("Dữ liệu khuyến mãi không hợp lệ.");
            if (string.IsNullOrWhiteSpace(promo.Code))
                throw new BusinessException("Mã code khuyến mãi không được để trống.");
            if (promo.DiscountPercent <= 0 || promo.DiscountPercent > 100)
                throw new BusinessException("Phần trăm giảm giá phải từ 1% đến 100%.");
            if (promo.EndDate <= promo.StartDate)
                throw new BusinessException("Ngày kết thúc phải lớn hơn ngày bắt đầu.");
        }
    }
}