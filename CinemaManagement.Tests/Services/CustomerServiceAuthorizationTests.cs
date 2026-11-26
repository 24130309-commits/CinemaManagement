using CinemaManagement.BLL.Services;
using CinemaManagement.BLL.Session;
using CinemaManagement.Core.DTOS;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.Tests.Services;

[TestFixture]
public sealed class CustomerServiceAuthorizationTests
{
    [Test]
    public void AddPointsAndUpgradeTier_RejectsStaffBeforeRepositoryAccess()
    {
        var session = new CurrentUserSession();
        session.Set(new User { Id = 4, Username = "cashier", Role = UserRole.Staff });
        var repository = new RecordingCustomerRepository();
        var service = new CustomerService(repository, session);

        Assert.Throws<BusinessException>(() => service.AddPointsAndUpgradeTier(7, 100));
        Assert.That(repository.Calls, Is.Zero);
    }

    [Test]
    public void AddPointsAndUpgradeTier_RejectsAnonymousCallerBeforeRepositoryAccess()
    {
        var repository = new RecordingCustomerRepository();
        var service = new CustomerService(repository, new CurrentUserSession());

        Assert.Throws<BusinessException>(() => service.AddPointsAndUpgradeTier(7, 100));
        Assert.That(repository.Calls, Is.Zero);
    }

    private sealed class RecordingCustomerRepository : ICustomerRepository
    {
        public int Calls { get; private set; }

        public Customer? GetById(int id) { Calls++; return new Customer { Id = id }; }
        public IEnumerable<Customer> GetAll() { Calls++; return []; }
        public int Insert(Customer entity) { Calls++; return 1; }
        public bool Update(Customer entity) { Calls++; return true; }
        public bool Delete(int id) { Calls++; return true; }
        public Customer? GetByPhone(string phone) { Calls++; return null; }
        public IEnumerable<Customer> Search(string keyword) { Calls++; return []; }
        public bool AddPoints(int customerId, int points) { Calls++; return true; }
        public bool UpdateTier(int customerId, string tier) { Calls++; return true; }
        public bool AddPointsAndUpdateTier(int customerId, int points) { Calls++; return true; }
        public IEnumerable<CustomerPurchaseHistoryDto> GetPurchaseHistory(int customerId) { Calls++; return []; }
    }
}
