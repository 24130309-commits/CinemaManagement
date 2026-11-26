
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.BLL.Session;

namespace CinemaManagement.BLL.Services;

public class PromotionService : IPromotionService
{
    private readonly IPromotionRepository _promotionRepository;
    private readonly CurrentUserSession _session;

    public PromotionService(
        IPromotionRepository promotionRepository,
        CurrentUserSession session)
    {
        _promotionRepository = promotionRepository
            ?? throw new ArgumentNullException(nameof(promotionRepository));
        _session = session
            ?? throw new ArgumentNullException(nameof(session));
    }

    public IEnumerable<Promotion> GetAllPromotions()
    {
        return _promotionRepository.GetAll();
    }

    public Promotion ValidateAndGetPromotion(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new BusinessException(
                "Vui lòng nhập mã khuyến mãi.");
        }

        var promotion = _promotionRepository.GetByCode(
            code.Trim());

        if (promotion is null)
        {
            throw new BusinessException(
                "Mã khuyến mãi không tồn tại.");
        }

        var now = DateTime.Now;

        if (!promotion.IsActive)
        {
            throw new BusinessException(
                "Chương trình khuyến mãi đã bị quản trị viên khóa.");
        }

        if (now < promotion.StartDate)
        {
            throw new BusinessException(
                "Mã khuyến mãi chưa đến thời gian áp dụng.");
        }

        if (now > promotion.EndDate)
        {
            throw new BusinessException(
                "Mã khuyến mãi đã hết hạn.");
        }

        if (promotion.UsageLimit > 0 &&
            promotion.UsedCount >= promotion.UsageLimit)
        {
            throw new BusinessException(
                "Mã khuyến mãi đã hết lượt sử dụng.");
        }

        if (promotion.Value <= 0)
        {
            throw new BusinessException(
                "Giá trị khuyến mãi không hợp lệ.");
        }

        return promotion;
    }

    public Promotion Validate(string code, decimal orderTotal)
    {
        if (orderTotal < 0)
        {
            throw new BusinessException(
                "Giá trị đơn hàng không hợp lệ.");
        }

        var promotion = ValidateAndGetPromotion(code);

        if (orderTotal < promotion.MinOrder)
        {
            throw new BusinessException(
                "Đơn hàng chưa đạt giá trị tối thiểu để sử dụng mã.");
        }

        return promotion;
    }

    public decimal ApplyDiscount(string code, decimal orderTotal)
    {
        var promotion = Validate(code, orderTotal);

        decimal discount;

        if (string.Equals(
            promotion.Type,
            "Percent",
            StringComparison.OrdinalIgnoreCase))
        {
            if (promotion.Value > 100)
            {
                throw new BusinessException(
                    "Phần trăm giảm giá không được vượt quá 100%.");
            }

            discount = orderTotal * promotion.Value / 100m;
        }
        else if (string.Equals(
            promotion.Type,
            "Fixed",
            StringComparison.OrdinalIgnoreCase))
        {
            discount = promotion.Value;
        }
        else
        {
            throw new BusinessException(
                "Loại khuyến mãi không hợp lệ.");
        }

        if (promotion.MaxDiscountAmount > 0)
        {
            discount = Math.Min(
                discount,
                promotion.MaxDiscountAmount);
        }

        discount = decimal.Round(
            discount,
            0,
            MidpointRounding.AwayFromZero);

        return Math.Min(
            orderTotal,
            Math.Max(0m, discount));
    }

    public void AddPromotion(Promotion promotion)
    {
        AuthorizationGuard.EnsureAdministrator(_session, "quản lý khuyến mãi");
        ValidatePromotionData(promotion);

        promotion.Code = NormalizeCode(promotion.Code);

        if (_promotionRepository.IsCodeExists(promotion.Code))
        {
            throw new BusinessException(
                "Mã khuyến mãi này đã tồn tại.");
        }

        // Chương trình mới bắt đầu với 0 lượt sử dụng.
        promotion.UsedCount = 0;

        if (_promotionRepository.Insert(promotion) <= 0)
        {
            throw new BusinessException(
                "Không thể thêm chương trình khuyến mãi.");
        }
    }

    public void UpdatePromotion(Promotion promotion)
    {
        AuthorizationGuard.EnsureAdministrator(_session, "quản lý khuyến mãi");
        ValidatePromotionData(promotion);

        if (promotion.Id <= 0)
        {
            throw new BusinessException(
                "Mã khuyến mãi không hợp lệ.");
        }

        promotion.Code = NormalizeCode(promotion.Code);

        var existing = _promotionRepository.GetById(promotion.Id);

        if (existing is null)
        {
            throw new BusinessException(
                "Không tìm thấy chương trình khuyến mãi.");
        }

        if (_promotionRepository.IsCodeExists(
            promotion.Code,
            promotion.Id))
        {
            throw new BusinessException(
                "Mã khuyến mãi bị trùng với chương trình khác.");
        }

        // Không cho chỉnh sửa làm mất lịch sử lượt sử dụng.
        promotion.UsedCount = existing.UsedCount;

        if (promotion.UsageLimit > 0 &&
            promotion.UsageLimit < promotion.UsedCount)
        {
            throw new BusinessException(
                "Giới hạn lượt sử dụng không được nhỏ hơn số lượt đã dùng.");
        }

        if (!_promotionRepository.Update(promotion))
        {
            throw new BusinessException(
                "Không thể cập nhật chương trình khuyến mãi.");
        }
    }

    public void DeletePromotion(int id)
    {
        AuthorizationGuard.EnsureAdministrator(_session, "quản lý khuyến mãi");
        if (id <= 0)
        {
            throw new BusinessException(
                "Mã khuyến mãi không hợp lệ.");
        }

        var promotion = _promotionRepository.GetById(id);

        if (promotion is null)
        {
            throw new BusinessException(
                "Không tìm thấy chương trình khuyến mãi.");
        }

        if (promotion.UsedCount > 0)
        {
            throw new BusinessException(
                "Không thể xóa chương trình đã phát sinh giao dịch. Hãy khóa chương trình thay vì xóa.");
        }

        if (!_promotionRepository.Delete(id))
        {
            throw new BusinessException(
                "Không thể xóa chương trình khuyến mãi.");
        }
    }

    private static string NormalizeCode(string code)
    {
        return code.Trim().ToUpperInvariant();
    }

    private static void ValidatePromotionData(Promotion promotion)
    {
        if (promotion is null)
        {
            throw new BusinessException(
                "Dữ liệu khuyến mãi không hợp lệ.");
        }

        if (string.IsNullOrWhiteSpace(promotion.Code))
        {
            throw new BusinessException(
                "Mã khuyến mãi không được để trống.");
        }

        if (promotion.Code.Trim().Length > 50)
        {
            throw new BusinessException(
                "Mã khuyến mãi không được vượt quá 50 ký tự.");
        }

        if (string.IsNullOrWhiteSpace(promotion.Type))
        {
            throw new BusinessException(
                "Vui lòng chọn loại khuyến mãi.");
        }

        if (promotion.Value <= 0)
        {
            throw new BusinessException(
                "Giá trị khuyến mãi phải lớn hơn 0.");
        }

        if (string.Equals(
            promotion.Type,
            "Percent",
            StringComparison.OrdinalIgnoreCase))
        {
            if (promotion.Value > 100)
            {
                throw new BusinessException(
                    "Phần trăm giảm giá không được vượt quá 100%.");
            }
        }
        else if (!string.Equals(
            promotion.Type,
            "Fixed",
            StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessException(
                "Loại khuyến mãi không hợp lệ.");
        }

        if (promotion.MaxDiscountAmount < 0 ||
            promotion.MinOrder < 0)
        {
            throw new BusinessException(
                "Giá trị giảm tối đa hoặc đơn hàng tối thiểu không hợp lệ.");
        }

        if (promotion.UsageLimit < 0 ||
            promotion.UsedCount < 0)
        {
            throw new BusinessException(
                "Giới hạn lượt sử dụng không hợp lệ.");
        }

        if (promotion.UsageLimit > 0 &&
            promotion.UsedCount > promotion.UsageLimit)
        {
            throw new BusinessException(
                "Số lượt đã sử dụng vượt quá giới hạn chương trình.");
        }

        if (promotion.EndDate <= promotion.StartDate)
        {
            throw new BusinessException(
                "Thời gian kết thúc phải sau thời gian bắt đầu.");
        }
    }
}
