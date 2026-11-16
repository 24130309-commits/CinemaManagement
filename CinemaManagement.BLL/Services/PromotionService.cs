
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
            _promotionRepository = promotionRepository
                ?? throw new ArgumentNullException(nameof(promotionRepository));
        }

        public IEnumerable<Promotion> GetAllPromotions()
        {
            return _promotionRepository.GetAll();
        }

        public Promotion ValidateAndGetPromotion(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new BusinessException("Vui lòng nhập mã khuyến mãi.");

            var promotion =
                _promotionRepository.GetByCode(code.Trim().ToUpperInvariant());

            if (promotion == null)
                throw new BusinessException(
                    "Mã khuyến mãi không tồn tại.");

            var now = DateTime.Now;

            if (now < promotion.StartDate || now > promotion.EndDate)
                throw new BusinessException(
                    "Mã khuyến mãi chưa đến hạn hoặc đã hết hạn.");

            if (promotion.UsageLimit > 0 &&
                promotion.UsedCount >= promotion.UsageLimit)
                throw new BusinessException(
                    "Mã khuyến mãi đã hết lượt sử dụng.");

            if (promotion.Value <= 0)
                throw new BusinessException(
                    "Giá trị khuyến mãi không hợp lệ.");

            return promotion;
        }

        public void AddPromotion(Promotion promotion)
        {
            ValidatePromotionData(promotion);

            promotion.Code = promotion.Code.Trim().ToUpperInvariant();

            if (_promotionRepository.IsCodeExists(promotion.Code))
                throw new BusinessException(
                    "Mã khuyến mãi này đã tồn tại.");

            if (_promotionRepository.Insert(promotion) <= 0)
                throw new BusinessException(
                    "Không thể thêm chương trình khuyến mãi.");
        }

        public void UpdatePromotion(Promotion promotion)
        {
            ValidatePromotionData(promotion);

            if (promotion.Id <= 0)
                throw new BusinessException(
                    "Mã khuyến mãi không hợp lệ.");

            promotion.Code = promotion.Code.Trim().ToUpperInvariant();

            if (_promotionRepository.IsCodeExists(
                promotion.Code,
                promotion.Id))
                throw new BusinessException(
                    "Mã khuyến mãi bị trùng với chương trình khác.");

            if (!_promotionRepository.Update(promotion))
                throw new BusinessException(
                    "Không thể cập nhật chương trình khuyến mãi.");
        }

        public void DeletePromotion(int id)
        {
            if (id <= 0)
                throw new BusinessException(
                    "Mã khuyến mãi không hợp lệ.");

            if (!_promotionRepository.Delete(id))
                throw new BusinessException(
                    "Không thể xóa chương trình khuyến mãi.");
        }

        private static void ValidatePromotionData(Promotion promotion)
        {
            if (promotion == null)
                throw new BusinessException(
                    "Dữ liệu khuyến mãi không hợp lệ.");

            if (string.IsNullOrWhiteSpace(promotion.Code))
                throw new BusinessException(
                    "Mã khuyến mãi không được để trống.");

            if (string.IsNullOrWhiteSpace(promotion.Type))
                throw new BusinessException(
                    "Vui lòng chọn loại khuyến mãi.");

            if (promotion.Value <= 0)
                throw new BusinessException(
                    "Giá trị khuyến mãi phải lớn hơn 0.");

            if (string.Equals(
                promotion.Type,
                "Percent",
                StringComparison.OrdinalIgnoreCase) &&
                promotion.Value > 100)
                throw new BusinessException(
                    "Phần trăm giảm giá không được vượt quá 100%.");

            if (promotion.MinOrder < 0)
                throw new BusinessException(
                    "Giá trị đơn hàng tối thiểu không hợp lệ.");

            if (promotion.UsageLimit < 0)
                throw new BusinessException(
                    "Giới hạn lượt sử dụng không hợp lệ.");

            if (promotion.EndDate <= promotion.StartDate)
                throw new BusinessException(
                    "Ngày kết thúc phải sau ngày bắt đầu.");
        }
    }
}
