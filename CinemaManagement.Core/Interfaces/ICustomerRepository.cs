using CinemaManagement.Core.Entities;
using CinemaManagement.Core.DTOS;

namespace CinemaManagement.Core.Interfaces;

public interface ICustomerRepository : IRepository<Customer>
{
    Customer? GetByPhone(string phone);

    IEnumerable<Customer> Search(string keyword);

    bool AddPoints(int customerId, int points);

    bool UpdateTier(int customerId, string tier);

    IEnumerable<CustomerPurchaseHistoryDto>
        GetPurchaseHistory(int customerId);
}