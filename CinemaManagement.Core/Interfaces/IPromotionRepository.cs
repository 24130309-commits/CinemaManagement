using CinemaManagement.Core.Entities;

namespace CinemaManagement.Core.Interfaces
{
    public interface IPromotionRepository : IRepository<Promotion>
    {
        Promotion GetByCode(string code);
        bool IsCodeExists(string code, int? excludeId = null);
    }
}