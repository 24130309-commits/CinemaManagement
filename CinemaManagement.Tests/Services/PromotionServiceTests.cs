
using CinemaManagement.BLL.Services;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.Tests.Services;

[TestFixture]
public class PromotionServiceTests
{
    [Test]
    public void Validate_ReturnsPromotion_WhenCodeIsValidAndOrderMeetsMinimum()
    {
        var repo = new FakePromotionRepository(ValidPromotion());
        var service = new PromotionService(repo);

        Assert.That(
            service.Validate("SAVE10", 100000).Code,
            Is.EqualTo("SAVE10"));
    }

    [Test]
    public void Validate_RejectsExpiredPromotion()
    {
        var promo = ValidPromotion();
        promo.EndDate = DateTime.Today.AddDays(-1);

        var service = new PromotionService(
            new FakePromotionRepository(promo));

        Assert.Throws<Exception>(
            () => service.Validate("SAVE10", 100000));
    }

    [Test]
    public void Validate_RejectsPromotionWhenUsageLimitReached()
    {
        var promo = ValidPromotion();
        promo.UsageLimit = 5;
        promo.UsedCount = 5;

        var service = new PromotionService(
            new FakePromotionRepository(promo));

        Assert.Throws<Exception>(
            () => service.Validate("SAVE10", 100000));
    }

    [Test]
    public void Validate_RejectsOrderBelowMinimum()
    {
        var promo = ValidPromotion();
        promo.MinOrder = 200000;

        var service = new PromotionService(
            new FakePromotionRepository(promo));

        Assert.Throws<Exception>(
            () => service.Validate("SAVE10", 100000));
    }

    [Test]
    public void ApplyDiscount_CapsDiscountAtOrderTotal()
    {
        var promo = ValidPromotion();
        promo.Type = "Fixed";
        promo.Value = 200000;

        var service = new PromotionService(
            new FakePromotionRepository(promo));

        Assert.That(
            service.ApplyDiscount("SAVE10", 100000),
            Is.EqualTo(100000m));
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

    private sealed class FakePromotionRepository
        : IPromotionRepository
    {
        private readonly List<Promotion> _items;

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
            entity.Id = _items.Count + 1;
            _items.Add(entity);

            return entity.Id;
        }

        public bool Update(Promotion entity)
        {
            return true;
        }

        public bool Delete(int id)
        {
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
