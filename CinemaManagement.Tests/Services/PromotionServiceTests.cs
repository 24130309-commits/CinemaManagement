
using CinemaManagement.BLL.Services;
using CinemaManagement.BLL.Session;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.Tests.Services;

[TestFixture]
public class PromotionServiceTests
{
    [Test]
    public void Validate_ReturnsPromotion_WhenCodeIsValidAndOrderMeetsMinimum()
    {
        var repo = new FakePromotionRepository(ValidPromotion());
        var service = CreateService(repo);

        Assert.That(
            service.Validate("SAVE10", 100000).Code,
            Is.EqualTo("SAVE10"));
    }

    [Test]
    public void Validate_RejectsExpiredPromotion()
    {
        var promo = ValidPromotion();
        promo.EndDate = DateTime.Today.AddDays(-1);

        var service = CreateService(new FakePromotionRepository(promo));

        Assert.Throws<BusinessException>(
            () => service.Validate("SAVE10", 100000));
    }

    [Test]
    public void Validate_RejectsPromotionWhenUsageLimitReached()
    {
        var promo = ValidPromotion();
        promo.UsageLimit = 5;
        promo.UsedCount = 5;

        var service = CreateService(new FakePromotionRepository(promo));

        Assert.Throws<BusinessException>(
            () => service.Validate("SAVE10", 100000));
    }

    [Test]
    public void Validate_RejectsOrderBelowMinimum()
    {
        var promo = ValidPromotion();
        promo.MinOrder = 200000;

        var service = CreateService(new FakePromotionRepository(promo));

        Assert.Throws<BusinessException>(
            () => service.Validate("SAVE10", 100000));
    }

    [Test]
    public void ApplyDiscount_CapsDiscountAtOrderTotal()
    {
        var promo = ValidPromotion();
        promo.Type = "Fixed";
        promo.Value = 200000;

        var service = CreateService(new FakePromotionRepository(promo));

        Assert.That(
            service.ApplyDiscount("SAVE10", 100000),
            Is.EqualTo(100000m));
    }

    [Test]
    public void ApplyDiscount_RoundsToWholeVndAmountDisplayedAtCheckout()
    {
        var promo = ValidPromotion();
        promo.Value = 10;
        var service = CreateService(new FakePromotionRepository(promo));

        Assert.That(service.ApplyDiscount("SAVE10", 99999m), Is.EqualTo(10000m));
    }

    [Test]
    public void AddPromotion_RejectsStaffBeforeRepositoryWrite()
    {
        var repository = new FakePromotionRepository(ValidPromotion());
        var session = new CurrentUserSession();
        session.Set(new User { Id = 3, Username = "staff", Role = UserRole.Staff });
        var service = new PromotionService(repository, session);

        Assert.Throws<BusinessException>(() => service.AddPromotion(ValidPromotion()));
        Assert.That(repository.WriteCount, Is.Zero);
    }

    [Test]
    public void AddPromotion_RejectsZeroLengthDateRangeBeforeRepositoryWrite()
    {
        var repository = new FakePromotionRepository(ValidPromotion());
        var promotion = ValidPromotion();
        promotion.StartDate = DateTime.Today;
        promotion.EndDate = DateTime.Today;
        var service = CreateService(repository);

        Assert.Throws<BusinessException>(() => service.AddPromotion(promotion));
        Assert.That(repository.WriteCount, Is.Zero);
    }

    private static Promotion ValidPromotion()
    {
        return new Promotion
        {
            Id = 1,
            Code = "SAVE10",
            Type = "Percent",
            Value = 10,
            StartDate = DateTime.Today.AddDays(-1),
            EndDate = DateTime.Today.AddDays(5),
            MinOrder = 0,
            UsageLimit = 0,
            UsedCount = 0
        };
    }

    private static PromotionService CreateService(
        IPromotionRepository repository)
    {
        var session = new CurrentUserSession();
        session.Set(new User { Id = 1, Username = "admin", Role = UserRole.Admin });
        return new PromotionService(repository, session);
    }

    private sealed class FakePromotionRepository
        : IPromotionRepository
    {
        private readonly List<Promotion> _items;
        public int WriteCount { get; private set; }

        public FakePromotionRepository(Promotion item)
        {
            _items = new List<Promotion> { item };
        }

        public Promotion? GetById(int id)
        {
            return _items.FirstOrDefault(x => x.Id == id);
        }

        public IEnumerable<Promotion> GetAll()
        {
            return _items;
        }

        public int Insert(Promotion entity)
        {
            WriteCount++;
            entity.Id = _items.Count + 1;
            _items.Add(entity);

            return entity.Id;
        }

        public bool Update(Promotion entity)
        {
            WriteCount++;
            return true;
        }

        public bool Delete(int id)
        {
            WriteCount++;
            return _items.RemoveAll(x => x.Id == id) > 0;
        }

        public Promotion? GetByCode(string code)
        {
            return _items.FirstOrDefault(x =>
                x.Code.Equals(
                    code,
                    StringComparison.OrdinalIgnoreCase));
        }

        public bool IsCodeExists(
            string code,
            int? excludeId = null)
        {
            return _items.Any(x =>
                x.Code.Equals(
                    code,
                    StringComparison.OrdinalIgnoreCase)
                && x.Id != excludeId);
        }

        public bool IncreaseUsedCount(int id)
        {
            var promotion = GetById(id);

            if (promotion == null)
                return false;

            if (promotion.UsageLimit > 0 &&
                promotion.UsedCount >= promotion.UsageLimit)
            {
                return false;
            }

            promotion.UsedCount++;

            return true;
        }
    }
}
