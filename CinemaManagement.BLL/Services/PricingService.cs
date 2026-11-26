
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.BLL.Session;

namespace CinemaManagement.BLL.Services;

public class PricingService : IPricingService
{
    private readonly IPricingRuleRepository _pricingRuleRepository;
    private readonly IPromotionService _promotionService;
    private readonly IRoomRepository? _roomRepository;
    private readonly CurrentUserSession _session;

    public PricingService(
        IPricingRuleRepository pricingRuleRepository,
        IPromotionService promotionService,
        IRoomRepository? roomRepository,
        CurrentUserSession session)
    {
        _pricingRuleRepository = pricingRuleRepository
            ?? throw new ArgumentNullException(nameof(pricingRuleRepository));

        _promotionService = promotionService
            ?? throw new ArgumentNullException(nameof(promotionService));

        _roomRepository = roomRepository;
        _session = session
            ?? throw new ArgumentNullException(nameof(session));
    }

    public IEnumerable<PricingRule> GetAllRules()
    {
        return _pricingRuleRepository.GetAll();
    }

    public void AddRule(PricingRule rule)
    {
        AuthorizationGuard.EnsureAdministrator(_session, "quản lý quy tắc giá");
        ValidateRule(rule);

        if (_pricingRuleRepository.Insert(rule) <= 0)
        {
            throw new BusinessException(
                "Không thể thêm quy tắc giá.");
        }
    }

    public void UpdateRule(PricingRule rule)
    {
        AuthorizationGuard.EnsureAdministrator(_session, "quản lý quy tắc giá");
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
        AuthorizationGuard.EnsureAdministrator(_session, "quản lý quy tắc giá");
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

    public decimal CalculateTicketPrice(
        Showtime showtime,
        Seat seat)
    {
        if (showtime is null || seat is null)
        {
            throw new BusinessException(
                "Thông tin suất chiếu hoặc ghế không hợp lệ.");
        }

        if (showtime.Id <= 0 ||
            showtime.RoomId <= 0 ||
            showtime.BasePrice < 0)
        {
            throw new BusinessException(
                "Thông tin suất chiếu hoặc giá vé không hợp lệ.");
        }

        if (seat.Id <= 0 ||
            seat.RoomId != showtime.RoomId)
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

        var rules = _pricingRuleRepository.GetBySeatAndScreenType(
            seat.SeatType,
            room.ScreenType,
            dayType);

        var selectedRule = ResolveApplicableRule(
            rules,
            dayType,
            showtime.StartTime.TimeOfDay);

        var multiplier = selectedRule?.Multiplier ?? 1m;
        var extraFee = selectedRule?.ExtraFee ?? 0m;

        var totalPrice =
            showtime.BasePrice * multiplier + extraFee;

        return RoundPrice(totalPrice);
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

        if (!Enum.TryParse<SeatType>(
            seatType.Trim(),
            true,
            out var parsedSeatType))
        {
            throw new BusinessException(
                "Loại ghế không tồn tại.");
        }

        var dayType = GetDayType(showTime);

        var rules = _pricingRuleRepository.GetByDayAndSeatType(
            dayType,
            parsedSeatType.ToString());

        var selectedRule = ResolveApplicableRule(
            rules,
            dayType,
            showTime.TimeOfDay);

        var multiplier = selectedRule?.Multiplier ?? 1m;
        var extraFee = selectedRule?.ExtraFee ?? 0m;

        var totalPrice =
            basePrice * multiplier + extraFee;

        if (!string.IsNullOrWhiteSpace(promoCode))
        {
            var discount = _promotionService.ApplyDiscount(
                promoCode.Trim(),
                totalPrice);

            totalPrice -= discount;
        }

        return RoundPrice(totalPrice);
    }

    private static PricingRule? ResolveApplicableRule(
        IEnumerable<PricingRule> rules,
        string dayType,
        TimeSpan currentTime)
    {
        var matchedRules = rules
            .Where(rule => TimeMatches(rule, currentTime))
            .ToList();

        if (matchedRules.Count == 0)
            return null;

        // Quy tắc theo ngày cụ thể được ưu tiên hơn All.
        var daySpecificRules = matchedRules
            .Where(rule => string.Equals(
                rule.DayType,
                dayType,
                StringComparison.OrdinalIgnoreCase))
            .ToList();

        var candidates = daySpecificRules.Count > 0
            ? daySpecificRules
            : matchedRules
                .Where(rule => string.Equals(
                    rule.DayType,
                    "All",
                    StringComparison.OrdinalIgnoreCase))
                .ToList();

        return candidates
            .OrderBy(rule => GetTimeWindowDuration(rule))
            .ThenByDescending(rule => rule.Multiplier)
            .ThenByDescending(rule => rule.ExtraFee)
            .ThenBy(rule => rule.Id)
            .FirstOrDefault();
    }

    private static bool TimeMatches(
        PricingRule rule,
        TimeSpan currentTime)
    {
        if (!TimeSpan.TryParse(rule.TimeFrom, out var from) ||
            !TimeSpan.TryParse(rule.TimeTo, out var to))
        {
            return false;
        }

        if (from <= to)
        {
            return currentTime >= from && currentTime <= to;
        }

        // Hỗ trợ khung giờ qua nửa đêm.
        return currentTime >= from || currentTime <= to;
    }

    private static TimeSpan GetTimeWindowDuration(
        PricingRule rule)
    {
        if (!TimeSpan.TryParse(rule.TimeFrom, out var from) ||
            !TimeSpan.TryParse(rule.TimeTo, out var to))
        {
            return TimeSpan.MaxValue;
        }

        if (to >= from)
            return to - from;

        return TimeSpan.FromDays(1) - from + to;
    }

    private static string GetDayType(DateTime date)
    {
        return date.DayOfWeek is
            DayOfWeek.Saturday or DayOfWeek.Sunday
            ? "Weekend"
            : "Weekday";
    }

    private static decimal RoundPrice(decimal price)
    {
        return decimal.Round(
            Math.Max(0m, price),
            0,
            MidpointRounding.AwayFromZero);
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

        if (!Enum.IsDefined(typeof(SeatType), rule.SeatType) ||
            !Enum.IsDefined(typeof(ScreenType), rule.ScreenType))
        {
            throw new BusinessException(
                "Loại ghế hoặc loại phòng không hợp lệ.");
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

        if (!new[] { "All", "Weekday", "Weekend" }
            .Contains(rule.DayType.Trim(),
                StringComparer.OrdinalIgnoreCase))
        {
            throw new BusinessException(
                "Loại ngày không hợp lệ.");
        }

        if (!TimeSpan.TryParse(rule.TimeFrom, out var from) ||
            !TimeSpan.TryParse(rule.TimeTo, out var to) ||
            from < TimeSpan.Zero ||
            to < TimeSpan.Zero ||
            from >= TimeSpan.FromDays(1) ||
            to >= TimeSpan.FromDays(1))
        {
            throw new BusinessException(
                "Khung giờ áp dụng không hợp lệ.");
        }
    }
}
