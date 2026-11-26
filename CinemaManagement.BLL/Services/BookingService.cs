
using CinemaManagement.BLL.Session;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.BLL.Services;

public sealed class BookingService : IBookingService
{
    private readonly IBookingRepository _bookings;
    private readonly ITicketRepository _tickets;
    private readonly ISeatHoldRepository _holds;
    private readonly ISeatMapService _seatMap;
    private readonly IShowtimeRepository _showtimes;
    private readonly CurrentUserSession _session;
    private readonly IProductService _productsService;
    private readonly ICustomerService _customers;
    private readonly IPromotionService _promotions;

    public BookingService(
        IBookingRepository bookings,
        ITicketRepository tickets,
        ISeatHoldRepository holds,
        ISeatMapService seatMap,
        CurrentUserSession session,
        IProductService productsService,
        ICustomerService customers,
        IPromotionService promotions,
        IShowtimeRepository showtimes)
    {
        _bookings = bookings ?? throw new ArgumentNullException(nameof(bookings));
        _tickets = tickets ?? throw new ArgumentNullException(nameof(tickets));
        _holds = holds ?? throw new ArgumentNullException(nameof(holds));
        _seatMap = seatMap ?? throw new ArgumentNullException(nameof(seatMap));
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _productsService = productsService ?? throw new ArgumentNullException(nameof(productsService));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _promotions = promotions ?? throw new ArgumentNullException(nameof(promotions));
        _showtimes = showtimes ?? throw new ArgumentNullException(nameof(showtimes));
    }

    public int CreateBooking(
        int customerId,
        int showtimeId,
        List<int> seatIds,
        List<BookingProduct> products,
        PaymentMethod paymentMethod,
        string? promotionCode = null)
    {
        if (!_session.IsAuthenticated || _session.User is null)
        {
            throw new BusinessException(
                "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.");
        }

        var now = DateTime.Now;

        if (showtimeId <= 0)
            throw new BusinessException("Suất chiếu không hợp lệ.");

        var showtime = _showtimes.GetById(showtimeId)
            ?? throw new BusinessException("Không tìm thấy suất chiếu.");

        if (!showtime.IsActive)
        {
            throw new BusinessException(
                "Suất chiếu đã ngừng hoạt động.");
        }

        if (showtime.StartTime <= now)
        {
            throw new BusinessException(
                "Suất chiếu đã bắt đầu hoặc không còn nhận đặt vé.");
        }

        if (customerId <= 0 ||
            !_customers.GetAll().Any(x => x.Id == customerId))
        {
            throw new BusinessException(
                "Khách hàng không hợp lệ.");
        }


        if (!Enum.IsDefined(typeof(PaymentMethod), paymentMethod))
        {
            throw new BusinessException(
                "Phương thức thanh toán không hợp lệ.");
        }

        if (seatIds is null || seatIds.Count == 0)
        {
            throw new BusinessException(
                "Vui lòng chọn ít nhất một ghế.");
        }


        // Ghế trùng trong dữ liệu đầu vào phải được báo lỗi.
        if (seatIds.Any(id => id <= 0) ||
            seatIds.Distinct().Count() != seatIds.Count)
        {
            throw new BusinessException(
                "Danh sách ghế không hợp lệ hoặc có ghế bị chọn trùng.");
        }

        var selectedSeats = seatIds.ToList();

        _holds.ReleaseExpiredHolds();

        var seatMap = _seatMap
            .GetSeatMapByShowtime(showtimeId)
            .ToDictionary(x => x.Id);

        var activeHolds = _holds
            .GetActiveHoldsByShowtime(showtimeId)
            .Where(x =>
                !x.IsReleased &&
                x.ExpiresAt > now)
            .ToList();

        var tickets = new List<Ticket>();

        foreach (var seatId in selectedSeats)
        {
            if (!seatMap.TryGetValue(seatId, out var seat))
            {
                throw new BusinessException(
                    $"Ghế #{seatId} không thuộc phòng của suất chiếu.");
            }

            if (seat.SeatType == SeatType.Aisle ||
                seat.Status == SeatStatus.Disabled)
            {
                throw new BusinessException(
                    $"Vị trí {seat.RowName}{seat.SeatNumber} không được phép đặt vé.");
            }

            if (seat.Status == SeatStatus.Booked ||
                _tickets.IsSeatBooked(showtimeId, seatId))
            {
                throw new BusinessException(
                    $"Ghế {seat.RowName}{seat.SeatNumber} đã được bán.");
            }

            var ownedHold = activeHolds.FirstOrDefault(x =>
                x.SeatId == seatId &&
                x.CustomerId == customerId &&
                x.SessionId == _session.SessionId);

            if (ownedHold is null)
            {
                throw new BusinessException(
                    $"Ghế {seat.RowName}{seat.SeatNumber} không còn được giữ bởi giao dịch này.");
            }

            if (seat.Status == SeatStatus.Holding &&
                !activeHolds.Any(x =>
                    x.SeatId == seatId &&
                    x.CustomerId == customerId &&
                    x.SessionId == _session.SessionId))
            {
                throw new BusinessException(
                    $"Ghế {seat.RowName}{seat.SeatNumber} đang được người khác giữ.");
            }

            if (seat.Price < 0)
            {
                throw new BusinessException(
                    $"Giá vé ghế {seat.RowName}{seat.SeatNumber} không hợp lệ.");
            }

            tickets.Add(new Ticket
            {
                ShowtimeId = showtimeId,
                SeatId = seatId,
                Price = seat.Price,
                Status = "Active",
                QrCode = Guid.NewGuid().ToString("N")
            });
        }

        var validatedProducts = ValidateProducts(products);

        decimal ticketTotal;
        decimal productTotal;
        decimal subtotal;

        try
        {
            ticketTotal = tickets.Sum(x => x.Price);

            productTotal = validatedProducts.Sum(
                x => checked(x.Price * x.Quantity));

            subtotal = checked(ticketTotal + productTotal);
        }
        catch (OverflowException)
        {
            throw new BusinessException(
                "Tổng giá trị đơn hàng vượt quá giới hạn.");
        }

        if (subtotal < 0)
        {
            throw new BusinessException(
                "Tổng giá trị đơn hàng không hợp lệ.");
        }

        decimal discount = 0m;
        int? promotionId = null;

        if (!string.IsNullOrWhiteSpace(promotionCode))
        {
            var promotion = _promotions.Validate(
                promotionCode.Trim(),
                subtotal);

            if (promotion is null)
            {
                throw new BusinessException(
                    "Mã khuyến mãi không hợp lệ.");
            }

            discount = _promotions.ApplyDiscount(
                promotion.Code,
                subtotal);

            if (discount < 0 || discount > subtotal)
            {
                throw new BusinessException(
                    "Giá trị giảm giá không hợp lệ.");
            }

            promotionId = promotion.Id;
        }

        var total = subtotal - discount;

        var booking = new Booking
        {
            Code = GenerateBookingCode(),
            CustomerId = customerId,
            UserId = _session.User.Id,
            CreatedAt = now,
            Subtotal = subtotal,
            Discount = discount,
            Total = total,
            Status = BookingStatus.Paid,
            PaymentMethod = paymentMethod
        };

        return _bookings.Add(
            booking,
            tickets,
            validatedProducts,
            customerId,
            _session.SessionId,
            promotionId);
    }

    private List<BookingProduct> ValidateProducts(
        List<BookingProduct>? products)
    {
        var result = new List<BookingProduct>();

        if (products is null || products.Count == 0)
            return result;

        foreach (var group in products.GroupBy(x => x.ProductId))
        {
            if (group.Key <= 0 ||
                group.Any(x => x.Quantity <= 0))
            {
                throw new BusinessException(
                    "Số lượng hoặc mã sản phẩm không hợp lệ.");
            }

            int quantity;

            try
            {
                quantity = group.Aggregate(
                    0,
                    (total, item) => checked(total + item.Quantity));
            }
            catch (OverflowException)
            {
                throw new BusinessException(
                    "Số lượng sản phẩm vượt quá giới hạn.");
            }

            var product = _productsService.GetById(group.Key);

            if (product is null || !product.IsActive)
            {
                throw new BusinessException(
                    $"Sản phẩm #{group.Key} không còn kinh doanh.");
            }

            if (product.Price < 0)
            {
                throw new BusinessException(
                    $"Giá sản phẩm {product.Name} không hợp lệ.");
            }

            result.Add(new BookingProduct
            {
                ProductId = product.Id,
                Quantity = quantity,
                Price = product.Price
            });
        }

        return result;
    }

    private static string GenerateBookingCode()
    {
        return $"CM{DateTime.Now:yyMMddHHmmss}" +
               Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
    }

    public List<Booking> GetBookingHistory(int customerId)
    {
        AuthorizationGuard.EnsureAuthenticated(_session);
        if (customerId <= 0)
        {
            throw new BusinessException(
                "Mã khách hàng không hợp lệ.");
        }

        return _bookings.GetByCustomerId(customerId);
    }

    public Booking? GetBookingDetails(int bookingId)
    {
        AuthorizationGuard.EnsureAuthenticated(_session);
        if (bookingId <= 0)
        {
            throw new BusinessException(
                "Mã hóa đơn không hợp lệ.");
        }

        return _bookings.GetById(bookingId);
    }
}
