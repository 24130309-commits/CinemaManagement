using CinemaManagement.Core.Entities;

namespace CinemaManagement.Core.Interfaces;

public interface IPromotionService
{
    IEnumerable<Promotion> GetAllPromotions();

    Promotion ValidateAndGetPromotion(string code);

    Promotion Validate(string code, decimal orderTotal);

    decimal ApplyDiscount(string code, decimal orderTotal);

    void AddPromotion(Promotion promotion);

    void UpdatePromotion(Promotion promotion);

    void DeletePromotion(int id);
}