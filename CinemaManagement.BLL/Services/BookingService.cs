using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.BLL.Services;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepo;
    private readonly ITicketRepository _ticketRepo;
    private readonly ISeatHoldRepository _seatHoldRepo;
    private readonly ISeatMapService _seatMapService;

    public BookingService(
        IBookingRepository bookingRepo,
        ITicketRepository ticketRepo,
        ISeatHoldRepository seatHoldRepo,
        ISeatMapService seatMapService)
    {
        _bookingRepo = bookingRepo;
        _ticketRepo = ticketRepo;
        _seatHoldRepo = seatHoldRepo;
        _seatMapService = seatMapService;
    }

    public int CreateBooking(
        int customerId,
        int showtimeId,
        List<int> seatIds,
        List<BookingProduct> products,
        PaymentMethod paymentMethod)
    {
        if (seatIds == null || seatIds.Count == 0)
        {
            throw new BusinessException(
                "Vui lòng chọn ít nhất một ghế trước khi thanh toán.");
        }

        _seatHoldRepo.ReleaseExpiredHolds();

        var seatMap = _seatMapService
            .GetSeatMapByShowtime(showtimeId)
            .ToDictionary(s => s.Id);

        var activeHolds = _seatHoldRepo
            .GetActiveHoldsByShowTime(showtimeId)
            .Where(h => !h.IsReleased && h.ExpiryTime > DateTime.Now)
            .ToList();

        var tickets = new List<Ticket>();
        decimal ticketTotal = 0m;

        foreach (int seatId in seatIds.Distinct())
        {
            if (!seatMap.TryGetValue(seatId, out var seatDto))
            {
                throw new BusinessException(
                    $"Ghế mã #{seatId} không thuộc phòng chiếu của suất này.");
            }

            if (_ticketRepo.IsSeatBooked(showtimeId, seatId))
            {
                throw new BusinessException(
                    $"Ghế {seatDto.RowName}{seatDto.SeatNumber} đã có người đặt.");
            }

            var holdByOther = activeHolds.FirstOrDefault(
                h => h.SeatId == seatId && h.CustomerId != customerId);

            if (holdByOther != null)
            {
                throw new BusinessException(
                    $"Ghế {seatDto.RowName}{seatDto.SeatNumber} đang được khách khác giữ.");
            }

            tickets.Add(new Ticket
            {
                ShowTimeId = showtimeId,
                SeatId = seatId,
                Price = seatDto.Price
            });

            ticketTotal += seatDto.Price;
        }

        products ??= new List<BookingProduct>();

        decimal productTotal = products.Sum(
            p => p.Price * p.Quantity);

        var booking = new Booking
        {
            CustomerId = customerId,
            BookingTime = DateTime.Now,
            TotalAmount = ticketTotal + productTotal,
            Status = BookingStatus.Completed,
            PaymentMethod = paymentMethod
        };

        int bookingId = _bookingRepo.Add(
            booking,
            tickets,
            products);

        foreach (var hold in activeHolds.Where(
                     h => h.CustomerId == customerId))
        {
            hold.IsReleased = true;
            _seatHoldRepo.Update(hold);
        }

        return bookingId;
    }

    public List<Booking> GetBookingHistory(int customerId)
    {
        return _bookingRepo.GetByCustomerId(customerId);
    }

    public Booking? GetBookingDetails(int bookingId)
    {
        return _bookingRepo.GetById(bookingId);
    }
}