
using CinemaManagement.Core.DTOs;
using CinemaManagement.Core.Enums;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.BLL.Session;

namespace CinemaManagement.BLL.Services;

public sealed class SeatMapService : ISeatMapService
{
    private readonly IShowtimeRepository _showtimes;
    private readonly ITicketRepository _tickets;
    private readonly ISeatHoldRepository _holds;
    private readonly ISeatRepository _seats;
    private readonly IRoomRepository _rooms;
    private readonly IPricingService _pricing;
    private readonly CurrentUserSession _session;

    public SeatMapService(
        IShowtimeRepository showtimes,
        ITicketRepository tickets,
        ISeatHoldRepository holds,
        ISeatRepository seats,
        IPricingService pricing,
        IRoomRepository rooms,
        CurrentUserSession session)
    {
        _showtimes = showtimes ?? throw new ArgumentNullException(nameof(showtimes));
        _tickets = tickets ?? throw new ArgumentNullException(nameof(tickets));
        _holds = holds ?? throw new ArgumentNullException(nameof(holds));
        _seats = seats ?? throw new ArgumentNullException(nameof(seats));
        _pricing = pricing ?? throw new ArgumentNullException(nameof(pricing));
        _rooms = rooms ?? throw new ArgumentNullException(nameof(rooms));
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    public List<SeatDisplayDto> GetSeatMapByShowtime(int showtimeId)
    {
        AuthorizationGuard.EnsureAuthenticated(_session);
        if (showtimeId <= 0)
            throw new BusinessException("Mã suất chiếu không hợp lệ.");

        var showtime = _showtimes.GetById(showtimeId)
            ?? throw new BusinessException("Không tìm thấy suất chiếu.");

        if (!showtime.IsActive || showtime.StartTime <= DateTime.Now)
        {
            throw new BusinessException(
                "Suất chiếu đã ngừng hoạt động hoặc không còn nhận đặt vé.");
        }

        var room = _rooms.GetById(showtime.RoomId);

        if (room is null || !room.IsActive)
        {
            throw new BusinessException(
                "Phòng chiếu đã ngừng hoạt động.");
        }

        _holds.ReleaseExpiredHolds();

        var soldSeats = _tickets.GetByShowTimeId(showtimeId)
            .Select(ticket => ticket.SeatId)
            .ToHashSet();

        var heldSeats = _holds.GetActiveHoldsByShowtime(showtimeId)
            .Where(hold =>
                !hold.IsReleased &&
                hold.ExpiresAt > DateTime.Now)
            .Select(hold => hold.SeatId)
            .ToHashSet();

        return _seats.GetByRoom(showtime.RoomId)
            .OrderBy(seat => seat.RowLabel)
            .ThenBy(seat => seat.ColNumber)
            .Select(seat =>
            {
                var isAisle = seat.SeatType == SeatType.Aisle;

                SeatStatus status;

                if (isAisle)
                    status = SeatStatus.Disabled;
                else if (soldSeats.Contains(seat.Id))
                    status = SeatStatus.Booked;
                else if (heldSeats.Contains(seat.Id))
                    status = SeatStatus.Holding;
                else
                    status = SeatStatus.Available;

                return new SeatDisplayDto
                {
                    Id = seat.Id,
                    RowLabel = seat.RowLabel,
                    ColNumber = seat.ColNumber,
                    SeatType = seat.SeatType,
                    Status = status,

                    // Lối đi không phải ghế bán vé.
                    Price = isAisle
                        ? 0m
                        : _pricing.CalculateTicketPrice(showtime, seat)
                };
            })
            .ToList();
    }
}
