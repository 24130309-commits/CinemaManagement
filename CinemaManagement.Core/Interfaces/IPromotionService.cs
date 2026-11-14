using System.Collections.Generic;
using CinemaManagement.Core.Entities;

namespace CinemaManagement.Core.Interfaces
{
    public interface IPromotionService
    {
        IEnumerable<Promotion> GetAllPromotions();
        Promotion ValidateAndGetPromotion(string code);
        void AddPromotion(Promotion promotion);
        void UpdatePromotion(Promotion promotion);
        void DeletePromotion(int id);
    }
}