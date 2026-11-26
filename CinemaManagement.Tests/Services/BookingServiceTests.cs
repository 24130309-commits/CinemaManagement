using CinemaManagement.BLL.Services;
using CinemaManagement.BLL.Session;
using CinemaManagement.Core.DTOs;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.Tests.Services;

[TestFixture]
public sealed class BookingServiceTests
{
    [Test]
    public void CreateBooking_CalculatesTotalsAndPassesHeldSeatToRepository()
    {
        var session = new CurrentUserSession();
        session.Set(new User { Id = 3, Username = "staff", Role = UserRole.Staff });
        var bookings = new FakeBookingRepository();
        var service = CreateService(session, bookings);

        var bookingId = service.CreateBooking(
            customerId: 7,
            showtimeId: 11,
            seatIds: [21],
            products: [new BookingProduct { ProductId = 31, Quantity = 2 }],
            paymentMethod: PaymentMethod.EWallet);

        Assert.Multiple(() =>
        {
            Assert.That(bookingId, Is.EqualTo(901));
            Assert.That(bookings.AddCount, Is.EqualTo(1));
            Assert.That(bookings.Booking?.Status, Is.EqualTo(BookingStatus.Paid));
            Assert.That(bookings.Booking?.Subtotal, Is.EqualTo(150000));
            Assert.That(bookings.Booking?.Discount, Is.Zero);
            Assert.That(bookings.Booking?.Total, Is.EqualTo(150000));
            Assert.That(bookings.Booking?.PaymentMethod, Is.EqualTo(PaymentMethod.EWallet));
            Assert.That(bookings.Tickets.Single().SeatId, Is.EqualTo(21));
            Assert.That(bookings.Products.Single().ProductId, Is.EqualTo(31));
            Assert.That(bookings.Products.Single().Price, Is.EqualTo(25000));
            Assert.That(bookings.Products.Single().Quantity, Is.EqualTo(2));
            Assert.That(bookings.SessionId, Is.EqualTo(session.SessionId));
        });
    }

    [Test]
    public void CreateBooking_RejectsAnonymousCallerBeforeRepositoryWrite()
    {
        var bookings = new FakeBookingRepository();
        var service = CreateService(new CurrentUserSession(), bookings);

        Assert.Throws<BusinessException>(() => service.CreateBooking(
            customerId: 7,
            showtimeId: 11,
            seatIds: [21],
            products: [],
            paymentMethod: PaymentMethod.Cash));

        Assert.That(bookings.AddCount, Is.Zero);
    }

    private static BookingService CreateService(
        CurrentUserSession session,
        FakeBookingRepository bookings)
    {
        var showtime = new Showtime
        {
            Id = 11,
            RoomId = 5,
            StartTime = DateTime.Now.AddDays(1),
            EndTime = DateTime.Now.AddDays(1).AddHours(2),
            BasePrice = 80000,
            IsActive = true
        };

        return new BookingService(
            bookings,
            new FakeTicketRepository(),
            new FakeSeatHoldRepository(session.SessionId),
            new FakeSeatMapService(),
            session,
            new FakeProductService(),
            new FakeCustomerService(),
            new FakePromotionService(),
            new FakeShowtimeRepository(showtime));
    }

    private sealed class FakeBookingRepository : IBookingRepository
    {
        public int AddCount { get; private set; }
        public Booking? Booking { get; private set; }
        public List<Ticket> Tickets { get; private set; } = [];
        public List<BookingProduct> Products { get; private set; } = [];
        public string SessionId { get; private set; } = string.Empty;

        public int Add(Booking booking, List<Ticket> tickets, List<BookingProduct> products,
            int customerId, string sessionId, int? promotionId = null)
        {
            AddCount++;
            Booking = booking;
            Tickets = tickets;
            Products = products;
            SessionId = sessionId;
            return 901;
        }

        public List<Booking> GetAll() => [];
        public List<Booking> GetByCustomerId(int customerId) => [];
        public Booking? GetById(int id) => null;
        public List<BookingProduct> GetProductsByBookingId(int bookingId) => [];
    }

    private sealed class FakeTicketRepository : ITicketRepository
    {
        public List<Ticket> GetByBookingId(int bookingId) => [];
        public List<Ticket> GetByShowtimeId(int showtimeId) => [];
        public bool IsSeatBooked(int showtimeId, int seatId) => false;
    }

    private sealed class FakeSeatHoldRepository(string sessionId) : ISeatHoldRepository
    {
        public List<SeatHold> GetActiveHoldsByShowtime(int showtimeId) =>
        [
            new SeatHold
            {
                ShowtimeId = showtimeId,
                SeatId = 21,
                CustomerId = 7,
                SessionId = sessionId,
                ExpiresAt = DateTime.Now.AddMinutes(5)
            }
        ];

        public SeatHold? GetById(int id) => null;
        public void Add(SeatHold seatHold) { }
        public void Update(SeatHold seatHold) { }
        public bool TryAcquireOrExtend(SeatHold seatHold) => true;
        public bool ReleaseIfOwned(int holdId, string ownerSessionId) => true;
        public void ReleaseExpiredHolds() { }
    }

    private sealed class FakeSeatMapService : ISeatMapService
    {
        public List<SeatDisplayDto> GetSeatMapByShowtime(int showtimeId) =>
        [
            new SeatDisplayDto
            {
                Id = 21,
                RowLabel = "A",
                ColNumber = 3,
                SeatType = SeatType.VIP,
                Status = SeatStatus.Available,
                Price = 100000
            }
        ];
    }

    private sealed class FakeShowtimeRepository(Showtime showtime) : IShowtimeRepository
    {
        public Showtime? GetById(int id) => id == showtime.Id ? showtime : null;
        public IEnumerable<Showtime> GetAll() => [showtime];
        public int Insert(Showtime entity) => throw new NotSupportedException();
        public bool Update(Showtime entity) => throw new NotSupportedException();
        public bool Delete(int id) => throw new NotSupportedException();
        public IEnumerable<Showtime> GetByMovieId(int movieId) => [];
        public IEnumerable<Showtime> GetByDate(DateTime date) => [];
        public bool HasConflict(int roomId, DateTime startTime, DateTime endTime, int? excludeShowtimeId = null) => false;
        public bool HasTickets(int showtimeId) => false;
    }

    private sealed class FakeProductService : IProductService
    {
        public Product? GetById(int id) => id == 31
            ? new Product { Id = 31, Name = "Combo", Price = 25000, IsActive = true }
            : null;

        public IEnumerable<Product> GetAll() => [];
        public IEnumerable<Product> GetActive() => [];
        public void Create(Product product) => throw new NotSupportedException();
        public void Update(Product product) => throw new NotSupportedException();
        public void Delete(int id) => throw new NotSupportedException();
    }

    private sealed class FakeCustomerService : ICustomerService
    {
        public IEnumerable<Customer> GetAll() => [new Customer { Id = 7 }];
        public IEnumerable<Customer> Search(string keyword) => GetAll();
        public Customer? FindByPhone(string phone) => null;
        public Customer Create(Customer customer) => throw new NotSupportedException();
        public void Update(Customer customer) => throw new NotSupportedException();
        public void AddPointsAndUpgradeTier(int customerId, int points) => throw new NotSupportedException();
        public IEnumerable<CinemaManagement.Core.DTOS.CustomerPurchaseHistoryDto> GetPurchaseHistory(int customerId) => [];
    }

    private sealed class FakePromotionService : IPromotionService
    {
        public IEnumerable<Promotion> GetAllPromotions() => [];
        public Promotion ValidateAndGetPromotion(string code) => throw new NotSupportedException();
        public Promotion Validate(string code, decimal orderTotal) => throw new NotSupportedException();
        public decimal ApplyDiscount(string code, decimal orderTotal) => throw new NotSupportedException();
        public void AddPromotion(Promotion promotion) => throw new NotSupportedException();
        public void UpdatePromotion(Promotion promotion) => throw new NotSupportedException();
        public void DeletePromotion(int id) => throw new NotSupportedException();
    }
}
