
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.BLL.Services;

public class PricingService : IPricingService
{
    private readonly IPricingRuleRepository _pricingRuleRepository;
    private readonly IPromotionService _promotionService;
    private readonly IRoomRepository? _roomRepository;

    public PricingService(
        IPricingRuleRepository pricingRuleRepository,
        IPromotionService promotionService)
        : this(pricingRuleRepository, promotionService, null)
    {
    }

    public PricingService(
        IPricingRuleRepository pricingRuleRepository,
        IPromotionService promotionService,
        IRoomRepository? roomRepository)
    {
        _pricingRuleRepository = pricingRuleRepository
            ?? throw new ArgumentNullException(nameof(pricingRuleRepository));

        _promotionService = promotionService
            ?? throw new ArgumentNullException(nameof(promotionService));

        _roomRepository = roomRepository;
    }

    public IEnumerable<PricingRule> GetAllRules()
    {
        return _pricingRuleRepository.GetAll();
    }

    public void AddRule(PricingRule rule)
    {
        ValidateRule(rule);

        if (_pricingRuleRepository.Insert(rule) <= 0)
        {
            throw new BusinessException(
                "Không thể thêm quy tắc giá.");
        }
    }

    public void UpdateRule(PricingRule rule)
    {
        ValidateRule(rule);

        if (rule.Id <= 0)
        {
            throw new BusinessException(
                "Mã quy tắc giá không hợp lệ.");
        }

        if (!_pricingRuleRepository.Update(rule))
        {
            throw new BusinessException(
                "Không thể cập nhật quy tắc giá.");
        }
    }

    public void DeleteRule(int id)
    {
        if (id <= 0)
        {
            throw new BusinessException(
                "Mã quy tắc giá không hợp lệ.");
        }

        if (!_pricingRuleRepository.Delete(id))
        {
            throw new BusinessException(
                "Không thể xóa quy tắc giá.");
        }
    }

    public decimal CalculateTicketPrice(Showtime showtime, Seat seat)
    {
        if (showtime is null || seat is null)
        {
            throw new BusinessException(
                "Thông tin suất chiếu hoặc ghế không hợp lệ.");
        }

        if (showtime.BasePrice < 0)
        {
            throw new BusinessException(
                "Giá vé cơ bản không hợp lệ.");
        }

        if (showtime.RoomId != seat.RoomId)
        {
            throw new BusinessException(
                "Ghế không thuộc phòng của suất chiếu.");
        }

        if (_roomRepository is null)
        {
            throw new BusinessException(
                "Chưa cấu hình repository phòng chiếu.");
        }

        var room = _roomRepository.GetById(showtime.RoomId);

        if (room is null || !room.IsActive)
        {
            throw new BusinessException(
                "Phòng chiếu không tồn tại hoặc đã ngừng hoạt động.");
        }

        var dayType = GetDayType(showtime.StartTime);

        var matchingRules = _pricingRuleRepository
            .GetBySeatAndScreenType(
                seat.SeatType,
                room.ScreenType,
                dayType)
            .Where(rule => TimeMatches(
                rule,
                showtime.StartTime.TimeOfDay))
            .ToList();

        var multiplier = matchingRules.Count == 0
            ? 1m
            : matchingRules.Max(rule => rule.Multiplier);

        var extraFee = matchingRules.Sum(rule => rule.ExtraFee);

        var totalPrice = showtime.BasePrice * multiplier + extraFee;

        return decimal.Round(
            Math.Max(0, totalPrice),
            0,
            MidpointRounding.AwayFromZero);
    }

    public decimal CalculateFinalTicketPrice(
        decimal basePrice,
        DateTime showTime,
        string seatType,
        string? promoCode = null)
    {
        if (basePrice < 0)
        {
            throw new BusinessException(
                "Giá vé cơ bản không hợp lệ.");
        }

        if (string.IsNullOrWhiteSpace(seatType))
        {
            throw new BusinessException(
                "Loại ghế không hợp lệ.");
        }

        var dayType = GetDayType(showTime);

        var matchingRules = _pricingRuleRepository
            .GetByDayAndSeatType(dayType, seatType)
            .Where(rule => TimeMatches(rule, showTime.TimeOfDay))
            .ToList();

        var multiplier = matchingRules.Count == 0
            ? 1m
            : matchingRules.Max(rule => rule.Multiplier);

        var extraFee = matchingRules.Sum(rule => rule.ExtraFee);

        var totalPrice = basePrice * multiplier + extraFee;

        if (!string.IsNullOrWhiteSpace(promoCode))
        {
            var promotion =
                _promotionService.ValidateAndGetPromotion(promoCode);

            if (promotion.MinOrder > totalPrice)
            {
                throw new BusinessException(
                    "Đơn hàng chưa đạt giá trị tối thiểu để sử dụng mã.");
            }

            var discount = string.Equals(
                promotion.Type,
                "Percent",
                StringComparison.OrdinalIgnoreCase)
                ? totalPrice * promotion.Value / 100m
                : promotion.Value;

            if (promotion.MaxDiscountAmount > 0)
            {
                discount = Math.Min(
                    discount,
                    promotion.MaxDiscountAmount);
            }

            totalPrice -= Math.Min(totalPrice, discount);
        }

        return decimal.Round(
            Math.Max(0, totalPrice),
            0,
            MidpointRounding.AwayFromZero);
    }

    private static string GetDayType(DateTime date)
    {
        return date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday
            ? "Weekend"
            : "Weekday";
    }

    private static bool TimeMatches(
        PricingRule rule,
        TimeSpan currentTime)
    {
        if (!TimeSpan.TryParse(rule.TimeFrom, out var from))
        {
            return true;
        }

        if (!TimeSpan.TryParse(rule.TimeTo, out var to))
        {
            return true;
        }

        if (from <= to)
        {
            return currentTime >= from && currentTime <= to;
        }

        return currentTime >= from || currentTime <= to;
    }

    private static void ValidateRule(PricingRule rule)
    {
        if (rule is null)
        {
            throw new BusinessException(
                "Quy tắc giá không hợp lệ.");
        }

        if (string.IsNullOrWhiteSpace(rule.Name))
        {
            throw new BusinessException(
                "Tên quy tắc giá không được để trống.");
        }

        if (rule.Multiplier <= 0)
        {
            throw new BusinessException(
                "Hệ số giá phải lớn hơn 0.");
        }

        if (rule.ExtraFee < 0)
        {
            throw new BusinessException(
                "Phụ thu không được âm.");
        }

        if (string.IsNullOrWhiteSpace(rule.DayType))
        {
            throw new BusinessException(
                "Vui lòng chọn loại ngày.");
        }

        if (string.IsNullOrWhiteSpace(rule.TimeFrom) ||
            string.IsNullOrWhiteSpace(rule.TimeTo))
        {
            throw new BusinessException(
                "Khung giờ áp dụng không hợp lệ.");
        }

        if (!TimeSpan.TryParse(rule.TimeFrom, out _) ||
            !TimeSpan.TryParse(rule.TimeTo, out _))
        {
            throw new BusinessException(
                "Định dạng khung giờ không hợp lệ.");
        }
    }
}
