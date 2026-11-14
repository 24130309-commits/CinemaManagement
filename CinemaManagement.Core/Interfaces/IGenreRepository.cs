using CinemaManagement.Core.Entities;

namespace CinemaManagement.Core.Interfaces
{
    public interface IGenreRepository : IRepository<Genre>
    {
        bool IsNameExists(string name, int? excludeId = null);
    }
}