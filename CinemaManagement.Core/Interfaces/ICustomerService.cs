using CinemaManagement.Core.Entities;
using CinemaManagement.Core.DTOS;

namespace CinemaManagement.Core.Interfaces;

public interface ICustomerService
{
    IEnumerable<Customer> GetAll();

    IEnumerable<Customer> Search(string keyword);

    Customer? FindByPhone(string phone);

    Customer Create(Customer customer);

    void Update(Customer customer);

    void AddPointsAndUpgradeTier(int customerId, int points);

    IEnumerable<CustomerPurchaseHistoryDto>
        GetPurchaseHistory(int customerId);
}