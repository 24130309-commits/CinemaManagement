
using System.Globalization;
using Dapper;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.DAL.Database;

namespace CinemaManagement.DAL.Repositories;

public sealed class BookingRepository : IBookingRepository
{
    private readonly SqliteConnectionFactory _factory;

    private const string SelectSql = """
        SELECT
            Id,
            Code,
            CustomerId,
            UserId,
            CreatedAt,
            Subtotal,
            Discount,
            Total,
            PaymentMethod,
            Status
        FROM Bookings
        """;

    public BookingRepository(SqliteConnectionFactory factory)
    {
        _factory = factory
            ?? throw new ArgumentNullException(nameof(factory));
    }

    public List<Booking> GetAll()
    {
        using var connection = _factory.CreateConnection();

        return connection.Query<Booking>(
            SelectSql + " ORDER BY CreatedAt DESC;").AsList();
    }

    public List<Booking> GetByCustomerId(int customerId)
    {
        if (customerId <= 0)
            return new List<Booking>();

        using var connection = _factory.CreateConnection();

        return connection.Query<Booking>(
            SelectSql + " WHERE CustomerId = @CustomerId ORDER BY CreatedAt DESC;",
            new { CustomerId = customerId }).AsList();
    }

    public Booking? GetById(int id)
    {
        if (id <= 0)
            return null;

        using var connection = _factory.CreateConnection();

        return connection.QuerySingleOrDefault<Booking>(
            SelectSql + " WHERE Id = @Id;",
            new { Id = id });
    }

    public List<BookingProduct> GetProductsByBookingId(int bookingId)
    {
        if (bookingId <= 0)
            return new List<BookingProduct>();

        using var connection = _factory.CreateConnection();

        return connection.Query<BookingProduct>(
            """
            SELECT Id, BookingId, ProductId, Qty, UnitPrice
            FROM BookingProducts
            WHERE BookingId = @BookingId
            ORDER BY Id;
            """,
            new { BookingId = bookingId }).AsList();
    }

    public int Add(
        Booking booking,
        List<Ticket> tickets,
        List<BookingProduct> products,
        int customerId,
        string sessionId,
        int? promotionId = null)
    {
        ArgumentNullException.ThrowIfNull(booking);
        ArgumentNullException.ThrowIfNull(tickets);
        ArgumentNullException.ThrowIfNull(products);

        if (tickets.Count == 0)
            throw new ArgumentException(
                "Hóa đơn phải có ít nhất một vé.",
                nameof(tickets));

        if (customerId <= 0)
            throw new ArgumentException("Khách hàng không hợp lệ.");

        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Phiên giao dịch không hợp lệ.");

        if (promotionId.HasValue && promotionId.Value <= 0)
            throw new ArgumentException("Mã khuyến mãi không hợp lệ.");

        ValidateBookingAmounts(booking);
        ValidateTickets(tickets);
        ValidateProducts(products);

        var seatIds = tickets
            .Select(ticket => ticket.SeatId)
            .ToList();

        var showtimeId = tickets[0].ShowtimeId;

        if (tickets.Any(ticket => ticket.ShowtimeId != showtimeId))
            throw new ArgumentException(
                "Các vé trong một hóa đơn phải thuộc cùng một suất chiếu.");

        using var connection = _factory.CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            var now = DateTime.Now.ToString("O");

            var customerExists = connection.ExecuteScalar<bool>(
                """
                SELECT EXISTS(
                    SELECT 1
                    FROM Customers
                    WHERE Id = @CustomerId
                );
                """,
                new { CustomerId = customerId },
                transaction);

            if (!customerExists)
                throw new InvalidOperationException("Khách hàng không tồn tại.");

            var showtime = connection.QuerySingleOrDefault<ShowtimeValidationRow>(
                """
                SELECT s.Id, s.RoomId, s.IsActive, s.StartTime
                FROM Showtimes s
                INNER JOIN Rooms r ON r.Id = s.RoomId
                WHERE s.Id = @ShowtimeId
                  AND r.IsActive = 1;
                """,
                new { ShowtimeId = showtimeId },
                transaction);

            if (showtime == null || !showtime.IsActive)
                throw new InvalidOperationException(
                    "Suất chiếu không tồn tại hoặc đã ngừng hoạt động.");

            var startTime = DateTime.Parse(
                showtime.StartTime,
                CultureInfo.InvariantCulture);

            if (startTime <= DateTime.Now)
                throw new InvalidOperationException(
                    "Suất chiếu đã bắt đầu, không thể bán vé.");

            var validSeatCount = connection.ExecuteScalar<int>(
                """
                SELECT COUNT(*)
                FROM Seats
                WHERE Id IN @SeatIds
                  AND RoomId = @RoomId
                  AND SeatType <> @AisleType;
                """,
                new
                {
                    SeatIds = seatIds,
                    RoomId = showtime.RoomId,
                    AisleType = (int)SeatType.Aisle
                },
                transaction);

            if (validSeatCount != seatIds.Count)
                throw new InvalidOperationException(
                    "Có ghế không thuộc phòng chiếu hoặc là vị trí lối đi.");

            var soldCount = connection.ExecuteScalar<int>(
                """
                SELECT COUNT(*)
                FROM Tickets
                WHERE ShowtimeId = @ShowtimeId
                  AND SeatId IN @SeatIds
                  AND Status = 'Active';
                """,
                new
                {
                    ShowtimeId = showtimeId,
                    SeatIds = seatIds
                },
                transaction);

            if (soldCount > 0)
                throw new InvalidOperationException(
                    "Một hoặc nhiều ghế đã được bán.");

            var ownedHoldCount = connection.ExecuteScalar<int>(
                """
                SELECT COUNT(DISTINCT SeatId)
                FROM SeatHolds
                WHERE ShowtimeId = @ShowtimeId
                  AND SeatId IN @SeatIds
                  AND CustomerId = @CustomerId
                  AND SessionId = @SessionId
                  AND IsReleased = 0
                  AND julianday(ExpiresAt) > julianday(@Now);
                """,
                new
                {
                    ShowtimeId = showtimeId,
                    SeatIds = seatIds,
                    CustomerId = customerId,
                    SessionId = sessionId,
                    Now = now
                },
                transaction);

            if (ownedHoldCount != seatIds.Count)
                throw new InvalidOperationException(
                    "Một hoặc nhiều ghế đã hết thời gian giữ chỗ hoặc không còn thuộc giao dịch này.");

            const string insertBooking = """
                INSERT INTO Bookings
                    (Code, CustomerId, UserId, CreatedAt, Subtotal,
                     Discount, Total, PaymentMethod, Status)
                VALUES
                    (@Code, @CustomerId, @UserId, @CreatedAt, @Subtotal,
                     @Discount, @Total, @PaymentMethod, @Status);

                SELECT last_insert_rowid();
                """;

            var bookingId = connection.ExecuteScalar<int>(
                insertBooking,
                new
                {
                    booking.Code,
                    CustomerId = customerId,
                    booking.UserId,
                    CreatedAt = booking.CreatedAt.ToString("O"),
                    booking.Subtotal,
                    booking.Discount,
                    booking.Total,
                    PaymentMethod = (int)booking.PaymentMethod,
                    Status = (int)booking.Status
                },
                transaction);

            var payment = new Payment
            {
                BookingId = bookingId,
                TransactionCode =
                    $"PAY{DateTime.Now:yyMMddHHmmss}" +
                    Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
                PaymentMethod = booking.PaymentMethod,
                Amount = booking.Total,
                Status = PaymentStatus.Success,
                PaidAt = DateTime.Now,
                ProviderReference = string.Empty,
                Note = "Thanh toán mô phỏng thành công"
            };

            connection.Execute(
                """
                INSERT INTO Payments
                    (BookingId, TransactionCode, PaymentMethod,
                     Amount, Status, PaidAt, ProviderReference, Note)
                VALUES
                    (@BookingId, @TransactionCode, @PaymentMethod,
                     @Amount, @Status, @PaidAt, @ProviderReference, @Note);
                """,
                new
                {
                    payment.BookingId,
                    payment.TransactionCode,
                    PaymentMethod = (int)payment.PaymentMethod,
                    payment.Amount,
                    Status = (int)payment.Status,
                    PaidAt = payment.PaidAt.ToString("O"),
                    payment.ProviderReference,
                    payment.Note
                },
                transaction);

            foreach (var ticket in tickets)
            {
                connection.Execute(
                    """
                    INSERT INTO Tickets
                        (BookingId, ShowtimeId, SeatId, Price, Status, QrCode)
                    VALUES
                        (@BookingId, @ShowtimeId, @SeatId, @Price, @Status, @QrCode);
                    """,
                    new
                    {
                        BookingId = bookingId,
                        ticket.ShowtimeId,
                        ticket.SeatId,
                        ticket.Price,
                        ticket.Status,
                        ticket.QrCode
                    },
                    transaction);
            }

            foreach (var product in products)
            {
                connection.Execute(
                    """
                    INSERT INTO BookingProducts
                        (BookingId, ProductId, Qty, UnitPrice)
                    VALUES
                        (@BookingId, @ProductId, @Qty, @UnitPrice);
                    """,
                    new
                    {
                        BookingId = bookingId,
                        product.ProductId,
                        Qty = product.Quantity,
                        UnitPrice = product.Price
                    },
                    transaction);
            }

            if (promotionId.HasValue)
            {
                var updated = connection.Execute(
                    """
                    UPDATE Promotions
                    SET UsedCount = UsedCount + 1
                    WHERE Id = @Id
                      AND IsActive = 1
                      AND (UsageLimit = 0 OR UsedCount < UsageLimit)
                      AND julianday(StartDate) <= julianday(@Now)
                      AND julianday(EndDate) >= julianday(@Now);
                    """,
                    new
                    {
                        Id = promotionId.Value,
                        Now = now
                    },
                    transaction);

                if (updated != 1)
                    throw new InvalidOperationException(
                        "Mã khuyến mãi vừa hết lượt sử dụng hoặc đã hết hạn.");
            }

            var releasedHolds = connection.Execute(
                """
                UPDATE SeatHolds
                SET IsReleased = 1
                WHERE ShowtimeId = @ShowtimeId
                  AND SeatId IN @SeatIds
                  AND CustomerId = @CustomerId
                  AND SessionId = @SessionId
                  AND IsReleased = 0
                  AND julianday(ExpiresAt) > julianday(@Now);
                """,
                new
                {
                    ShowtimeId = showtimeId,
                    SeatIds = seatIds,
                    CustomerId = customerId,
                    SessionId = sessionId,
                    Now = now
                },
                transaction);

            if (releasedHolds < seatIds.Count)
                throw new InvalidOperationException(
                    "Không thể hoàn tất giải phóng ghế giữ chỗ.");

            transaction.Commit();

            booking.Id = bookingId;

            return bookingId;
        }
        catch
        {
            try
            {
                transaction.Rollback();
            }
            catch
            {
            }

            throw;
        }
    }

    private static void ValidateBookingAmounts(Booking booking)
    {
        if (booking.Subtotal < 0 ||
            booking.Discount < 0 ||
            booking.Discount > booking.Subtotal ||
            booking.Total < 0 ||
            booking.Total != booking.Subtotal - booking.Discount)
        {
            throw new ArgumentException(
                "Thông tin tiền của hóa đơn không hợp lệ.");
        }
    }

    private static void ValidateTickets(List<Ticket> tickets)
    {
        var seatIds = tickets.Select(ticket => ticket.SeatId).ToList();

        if (tickets.Any(ticket =>
                ticket.SeatId <= 0 ||
                ticket.ShowtimeId <= 0 ||
                ticket.Price < 0))
        {
            throw new ArgumentException(
                "Danh sách vé chứa dữ liệu không hợp lệ.",
                nameof(tickets));
        }

        if (seatIds.Distinct().Count() != seatIds.Count)
            throw new ArgumentException(
                "Danh sách vé có ghế bị trùng.",
                nameof(tickets));

        var showtimeId = tickets[0].ShowtimeId;

        if (tickets.Any(ticket => ticket.ShowtimeId != showtimeId))
            throw new ArgumentException(
                "Các vé phải thuộc cùng một suất chiếu.",
                nameof(tickets));
    }

    private static void ValidateProducts(List<BookingProduct> products)
    {
        if (products.Any(product =>
                product.ProductId <= 0 ||
                product.Quantity <= 0 ||
                product.Price < 0))
        {
            throw new ArgumentException(
                "Danh sách sản phẩm không hợp lệ.",
                nameof(products));
        }
    }

    private sealed class ShowtimeValidationRow
    {
        public int Id { get; set; }
        public int RoomId { get; set; }
        public bool IsActive { get; set; }
        public string StartTime { get; set; } = string.Empty;
    }
}
