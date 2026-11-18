using CinemaManagement.Core.Entities;

namespace CinemaManagement.Core.Interfaces;

public interface IProductRepository : IRepository<Product>
{
    IEnumerable<Product> GetActive();

    IEnumerable<Product> GetByCategory(string category);
}