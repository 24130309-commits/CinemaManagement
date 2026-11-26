using CinemaManagement.Core.Entities;

namespace CinemaManagement.Core.Interfaces;

public interface IProductRepository : IRepository<Product>
{
    IEnumerable<Product> GetActive();

    IEnumerable<Product> GetByCategory(string category);

    // Kiểm tra sản phẩm đã từng xuất hiện trong hóa đơn hay chưa.
    bool HasBookings(int productId);
}