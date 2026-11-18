using CinemaManagement.Core.Entities;

namespace CinemaManagement.Core.Interfaces;

public interface IProductService
{
    IEnumerable<Product> GetAll();

    IEnumerable<Product> GetActive();

    Product? GetById(int id);

    void Create(Product product);

    void Update(Product product);

    void Delete(int id);
}