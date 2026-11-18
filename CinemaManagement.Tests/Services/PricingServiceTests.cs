using CinemaManagement.BLL.Services;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.Tests.Services;

[TestFixture]
public class PricingServiceTests
{
    [Test]
    public void CalculateTicketPrice_AppliesMatchingSeatScreenDayAndTimeRule()
    {
        var rules = new FakePricingRuleRepository();

        rules.Items.Add(new PricingRule
        {
            Id = 1,
            SeatType = SeatType.VIP,
            ScreenType = ScreenType.IMAX,
            DayType = "Weekday",
            TimeFrom = "18:00",
            TimeTo = "23:00",
            Multiplier = 1.5m
        });

        var room = new FakeRoomRepository();

        var service = new PricingService(
            rules,
            new FakePromotionService(),
            room);

        var start = NextWeekdayAt(19, 0);

        var showtime = new Showtime
        {
            Id = 1,
            RoomId = 1,
            StartTime = start,
            BasePrice = 100000
        };

        var seat = new Seat
        {
            Id = 1,
            RoomId = 1,
            SeatType = SeatType.VIP
        };

        Assert.That(
            service.CalculateTicketPrice(showtime, seat),
            Is.EqualTo(150000m));
    }

    [Test]
    public void CalculateFinalTicketPrice_AppliesPercentVoucher()
    {
        var promotion = new Promotion
        {
            Id = 2,
            Code = "SALE10",
            Type = "Percent",
            Value = 10,
            MinOrder = 0,
            StartDate = DateTime.Today.AddDays(-1),
            EndDate = DateTime.Today.AddDays(1)
        };

        var service = new PricingService(
            new FakePricingRuleRepository(),
            new FakePromotionService(promotion));

        var result = service.CalculateFinalTicketPrice(
            100000,
            DateTime.Today.AddHours(19),
            "Normal",
            "SALE10");

        Assert.That(result, Is.EqualTo(90000m));
    }

    [Test]
    public void CalculateFinalTicketPrice_UsesBasePriceWhenNoRuleMatches()
    {
        var service = new PricingService(
            new FakePricingRuleRepository(),
            new FakePromotionService());

        Assert.That(
            service.CalculateFinalTicketPrice(
                80000,
                DateTime.Today.AddHours(10),
                "Normal"),
            Is.EqualTo(80000m));
    }

    private static DateTime NextWeekdayAt(
        int hour,
        int minute)
    {
        var date = DateTime.Today.AddDays(1);

        while (date.DayOfWeek is
               DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            date = date.AddDays(1);
        }

        return date.AddHours(hour).AddMinutes(minute);
    }

    private sealed class FakePricingRuleRepository
        : IPricingRuleRepository
    {
        public List<PricingRule> Items { get; } = new();

        public PricingRule? GetById(int id)
        {
            return Items.FirstOrDefault(x => x.Id == id);
        }

        public IEnumerable<PricingRule> GetAll()
        {
            return Items;
        }

        public int Insert(PricingRule entity)
        {
            entity.Id = Items.Count + 1;
            Items.Add(entity);

            return entity.Id;
        }

        public bool Update(PricingRule entity)
        {
            return true;
        }

        public bool Delete(int id)
        {
            return Items.RemoveAll(x => x.Id == id) > 0;
        }

        public IEnumerable<PricingRule> GetByDayAndSeatType(
            string dayType,
            string seatType)
        {
            return Items.Where(x =>
                x.DayType == dayType &&
                x.SeatType.ToString() == seatType);
        }

        public IEnumerable<PricingRule> GetBySeatAndScreenType(
            SeatType seatType,
            ScreenType screenType,
            string dayType)
        {
            return Items.Where(x =>
                x.SeatType == seatType &&
                x.ScreenType == screenType &&
                (x.DayType == dayType || x.DayType == "All"));
        }
    }

    private sealed class FakePromotionService
        : IPromotionService
    {
        private readonly Promotion? _promotion;

        public FakePromotionService(
            Promotion? promotion = null)
        {
            _promotion = promotion;
        }

        public IEnumerable<Promotion> GetAllPromotions()
        {
            return _promotion == null
                ? Array.Empty<Promotion>()
                : new[] { _promotion };
        }

        public Promotion ValidateAndGetPromotion(string code)
        {
            return _promotion
                ?? throw new InvalidOperationException(
                    "No promotion");
        }

        public Promotion Validate(
            string code,
            decimal orderTotal)
        {
            return _promotion
                ?? throw new InvalidOperationException(
                    "No promotion");
        }

        public decimal ApplyDiscount(
            string code,
            decimal orderTotal)
        {
            return _promotion == null
                ? 0
                : Math.Min(
                    orderTotal,
                    orderTotal * _promotion.Value / 100m);
        }

        public void AddPromotion(Promotion promotion)
        {
        }

        public void UpdatePromotion(Promotion promotion)
        {
        }

        public void DeletePromotion(int id)
        {
        }
    }

    private sealed class FakeRoomRepository
        : IRoomRepository
    {
        public Room? GetById(int id)
        {
            return new Room
            {
                Id = 1,
                ScreenType = ScreenType.IMAX,
                IsActive = true
            };
        }

        public IEnumerable<Room> GetAll()
        {
            return Array.Empty<Room>();
        }

        public int Insert(Room entity)
        {
            return 1;
        }

        public bool Update(Room entity)
        {
            return true;
        }

        public bool Delete(int id)
        {
            return true;
        }

        public bool IsRoomNameExists(
            string name,
            int? excludeId = null)
        {
            return false;
        }

        public IEnumerable<Room> GetActive()
        {
            return GetAll();
        }
    }
}