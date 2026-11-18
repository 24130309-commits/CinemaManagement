using CinemaManagement.Core.DTOS;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.BLL.Services;

public sealed class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _repository;

    public CustomerService(ICustomerRepository repository)
    {
        _repository = repository;
    }

    public IEnumerable<Customer> GetAll()
    {
        return _repository.GetAll();
    }

    public IEnumerable<Customer> Search(string keyword)
    {
        return _repository.Search(keyword);
    }

    public Customer? FindByPhone(string phone)
    {
        return string.IsNullOrWhiteSpace(phone)
            ? null
            : _repository.GetByPhone(phone.Trim());
    }

    public Customer Create(Customer customer)
    {
        Validate(customer);

        if (_repository.GetByPhone(customer.Phone) != null)
            throw new BusinessException(
                "Số điện thoại đã được đăng ký.");

        customer.Phone = customer.Phone.Trim();
        customer.Tier = "Standard";
        customer.Points = 0;

        customer.Id = _repository.Insert(customer);

        if (customer.Id <= 0)
            throw new BusinessException("Không thể tạo khách hàng.");

        return customer;
    }

    public void Update(Customer customer)
    {
        Validate(customer);

        if (customer.Id <= 0)
            throw new BusinessException("Khách hàng không hợp lệ.");

        customer.Phone = customer.Phone.Trim();

        var existing = _repository.GetByPhone(customer.Phone);

        if (existing != null && existing.Id != customer.Id)
            throw new BusinessException(
                "Số điện thoại đã được sử dụng.");

        if (!_repository.Update(customer))
            throw new BusinessException(
                "Không thể cập nhật khách hàng.");
    }

    public void AddPointsAndUpgradeTier(
        int customerId,
        int points)
    {
        if (customerId <= 0 || points <= 0)
            throw new BusinessException(
                "Mã khách hàng hoặc điểm tích lũy không hợp lệ.");

        if (!_repository.AddPoints(customerId, points))
            throw new BusinessException(
                "Không tìm thấy khách hàng.");

        var customer = _repository.GetById(customerId)
            ?? throw new BusinessException(
                "Không tìm thấy khách hàng.");

        var tier = customer.Points >= 5000
            ? "Platinum"
            : customer.Points >= 2000
                ? "Gold"
                : customer.Points >= 500
                    ? "Silver"
                    : "Standard";

        _repository.UpdateTier(customerId, tier);
    }

    public IEnumerable<CustomerPurchaseHistoryDto>
        GetPurchaseHistory(int customerId)
    {
        if (customerId <= 0)
            throw new BusinessException(
                "Mã khách hàng không hợp lệ.");

        return _repository.GetPurchaseHistory(customerId);
    }

    private static void Validate(Customer customer)
    {
        if (customer == null ||
            string.IsNullOrWhiteSpace(customer.FullName) ||
            string.IsNullOrWhiteSpace(customer.Phone))
        {
            throw new BusinessException(
                "Họ tên và số điện thoại là bắt buộc.");
        }

        var phone = customer.Phone.Trim();

        if (phone.Length is < 9 or > 11 ||
            !phone.All(char.IsDigit))
        {
            throw new BusinessException(
                "Số điện thoại không hợp lệ.");
        }

        if (!string.IsNullOrWhiteSpace(customer.Email) &&
            !customer.Email.Contains('@'))
        {
            throw new BusinessException("Email không hợp lệ.");
        }
    }
}