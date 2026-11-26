
using CinemaManagement.BLL.Session;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.BLL.Services;

public class SeatHoldService : ISeatHoldService
{
    private readonly ISeatHoldRepository _seatHoldRepo;
    private readonly ITicketRepository _ticketRepo;
    private readonly IShowtimeRepository _showtimeRepo;
    private readonly ISeatRepository _seatRepo;
    private readonly CurrentUserSession _session;
    private readonly ISettingService? _settings;

    private int HoldMinutes =>
        _settings?.GetHoldMinutes()
        ?? CinemaManagement.Core.Constants.AppConstants.DefaultHoldMinutes;

    public SeatHoldService(
        ISeatHoldRepository seatHoldRepo,
        ITicketRepository ticketRepo,
        CurrentUserSession session,
        ISettingService? settings = null,
        IShowtimeRepository? showtimeRepo = null,
        ISeatRepository? seatRepo = null)
    {
        _seatHoldRepo = seatHoldRepo
            ?? throw new ArgumentNullException(nameof(seatHoldRepo));

        _ticketRepo = ticketRepo
            ?? throw new ArgumentNullException(nameof(ticketRepo));

        _session = session
            ?? throw new ArgumentNullException(nameof(session));

        _settings = settings;

        _showtimeRepo = showtimeRepo
            ?? throw new ArgumentNullException(nameof(showtimeRepo));

        _seatRepo = seatRepo
            ?? throw new ArgumentNullException(nameof(seatRepo));
    }

    public bool IsSeatAvailable(int showtimeId, int seatId)
    {
        AuthorizationGuard.EnsureAuthenticated(_session);
        ValidateShowtimeAndSeat(showtimeId, seatId);

        _seatHoldRepo.ReleaseExpiredHolds();

        if (_ticketRepo.IsSeatBooked(showtimeId, seatId))
            return false;

        var seat = _seatRepo
            .GetByRoom(_showtimeRepo.GetById(showtimeId)!.RoomId)
            .First(x => x.Id == seatId);

        if (seat.SeatType == SeatType.Aisle)
            return false;

        return !_seatHoldRepo
            .GetActiveHoldsByShowtime(showtimeId)
            .Any(hold =>
                hold.SeatId == seatId &&
                !hold.IsReleased &&
                hold.ExpiresAt > DateTime.Now);
    }

    public SeatHold HoldSeat(
        int showtimeId,
        int seatId,
        int customerId)
    {
        if (!_session.IsAuthenticated || _session.User is null)
        {
            throw new BusinessException(
                "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.");
        }

        if (showtimeId <= 0 || seatId <= 0 || customerId <= 0)
        {
            throw new BusinessException(
                "Thông tin giữ chỗ không hợp lệ.");
        }

        if (HoldMinutes <= 0)
        {
            throw new BusinessException(
                "Thời gian giữ ghế chưa được cấu hình hợp lệ.");
        }

        var showtime = ValidateShowtimeAndSeat(
            showtimeId,
            seatId);

        // Không cho người dùng giữ ghế của khách hàng khác.

        var seat = _seatRepo
            .GetByRoom(showtime.RoomId)
            .FirstOrDefault(x => x.Id == seatId);

        if (seat is null)
        {
            throw new BusinessException(
                "Ghế không thuộc phòng của suất chiếu.");
        }

        if (seat.SeatType == SeatType.Aisle)
        {
            throw new BusinessException(
                "Vị trí lối đi không thể đặt vé.");
        }

        if (_ticketRepo.IsSeatBooked(showtimeId, seatId))
        {
            throw new BusinessException(
                "Ghế này đã được đặt mua.");
        }

        var now = DateTime.Now;

        var newHold = new SeatHold
        {
            ShowtimeId = showtimeId,
            SeatId = seatId,
            CustomerId = customerId,
            SessionId = _session.SessionId,
            HoldTime = now,
            ExpiresAt = now.AddMinutes(HoldMinutes),
            IsReleased = false
        };

        if (!_seatHoldRepo.TryAcquireOrExtend(newHold))
        {
            throw new BusinessException(
                "Ghế này đang được một giao dịch khác giữ. Vui lòng chọn ghế khác.");
        }

        return newHold;
    }

    public void ReleaseHold(int holdId)
    {
        if (holdId <= 0)
            return;

        if (!_session.IsAuthenticated)
        {
            throw new BusinessException(
                "Phiên đăng nhập đã hết hạn.");
        }

        _seatHoldRepo.ReleaseIfOwned(
            holdId,
            _session.SessionId);
    }

    public void ReleaseAllCustomerHolds(
        int showtimeId,
        int customerId)
    {
        if (showtimeId <= 0 || customerId <= 0)
            return;

        if (!_session.IsAuthenticated)
        {
            throw new BusinessException(
                "Phiên đăng nhập đã hết hạn.");
        }

        if (_session.User is null)
        {
            throw new BusinessException(
                "Phiên đăng nhập đã hết hạn.");
        }

        var holds = _seatHoldRepo
            .GetActiveHoldsByShowtime(showtimeId)
            .Where(hold =>
                hold.CustomerId == customerId &&
                hold.SessionId == _session.SessionId &&
                !hold.IsReleased &&
                hold.ExpiresAt > DateTime.Now)
            .ToList();

        foreach (var hold in holds)
        {
            _seatHoldRepo.ReleaseIfOwned(
                hold.Id,
                _session.SessionId);
        }
    }

    private Showtime ValidateShowtimeAndSeat(
        int showtimeId,
        int seatId)
    {
        if (showtimeId <= 0 || seatId <= 0)
        {
            throw new BusinessException(
                "Thông tin suất chiếu hoặc ghế không hợp lệ.");
        }

        var showtime = _showtimeRepo.GetById(showtimeId);

        if (showtime is null)
        {
            throw new BusinessException(
                "Không tìm thấy suất chiếu.");
        }

        if (!showtime.IsActive)
        {
            throw new BusinessException(
                "Suất chiếu đã ngừng hoạt động.");
        }

        if (showtime.StartTime <= DateTime.Now)
        {
            throw new BusinessException(
                "Suất chiếu đã bắt đầu hoặc không còn nhận đặt vé.");
        }

        var seatExists = _seatRepo
            .GetByRoom(showtime.RoomId)
            .Any(seat => seat.Id == seatId);

        if (!seatExists)
        {
            throw new BusinessException(
                "Ghế không thuộc phòng chiếu của suất chiếu này.");
        }

        return showtime;
    }
}
