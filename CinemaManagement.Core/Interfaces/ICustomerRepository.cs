using CinemaManagement.Core.Entities;
using CinemaManagement.Core.DTOS;

namespace CinemaManagement.Core.Interfaces;

public interface ICustomerRepository : IRepository<Customer>
{
    Customer? GetByPhone(string phone);

    IEnumerable<Customer> Search(string keyword);

    bool AddPoints(int customerId, int points);

    bool UpdateTier(int customerId, string tier);

    // Cộng điểm và cập nhật hạng trong cùng một thao tác.
    bool AddPointsAndUpdateTier(int customerId, int points);

    IEnumerable<CustomerPurchaseHistoryDto>
        GetPurchaseHistory(int customerId);
}