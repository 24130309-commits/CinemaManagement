
using System.Net.Mail;
using CinemaManagement.Core.DTOS;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.BLL.Session;

namespace CinemaManagement.BLL.Services;

public sealed class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _repository;
    private readonly CurrentUserSession _session;

    public CustomerService(
        ICustomerRepository repository,
        CurrentUserSession session)
    {
        _repository = repository
            ?? throw new ArgumentNullException(nameof(repository));
        _session = session
            ?? throw new ArgumentNullException(nameof(session));
    }

    public IEnumerable<Customer> GetAll()
    {
        AuthorizationGuard.EnsureAuthenticated(_session);
        return _repository.GetAll();
    }

    public IEnumerable<Customer> Search(string keyword)
    {
        AuthorizationGuard.EnsureAuthenticated(_session);
        return _repository.Search(keyword?.Trim() ?? string.Empty);
    }

    public Customer? FindByPhone(string phone)
    {
        AuthorizationGuard.EnsureAuthenticated(_session);
        if (string.IsNullOrWhiteSpace(phone))
            return null;

        return _repository.GetByPhone(phone.Trim());
    }

    public Customer Create(Customer customer)
    {
        AuthorizationGuard.EnsureAuthenticated(_session);
        Validate(customer);

        customer.FullName = customer.FullName.Trim();
        customer.Phone = customer.Phone.Trim();
        customer.Email = customer.Email.Trim();

        if (_repository.GetByPhone(customer.Phone) is not null)
        {
            throw new BusinessException(
                "Số điện thoại đã được đăng ký.");
        }

        customer.Points = 0;
        customer.Tier = "Standard";

        var id = _repository.Insert(customer);

        if (id <= 0)
            throw new BusinessException("Không thể tạo khách hàng.");

        customer.Id = id;
        return customer;
    }

    public void Update(Customer customer)
    {
        AuthorizationGuard.EnsureAdministrator(_session, "quản lý khách hàng");
        Validate(customer);

        if (customer.Id <= 0)
            throw new BusinessException("Mã khách hàng không hợp lệ.");

        customer.FullName = customer.FullName.Trim();
        customer.Phone = customer.Phone.Trim();
        customer.Email = customer.Email.Trim();

        var existing = _repository.GetByPhone(customer.Phone);

        if (existing is not null && existing.Id != customer.Id)
        {
            throw new BusinessException(
                "Số điện thoại đã được sử dụng.");
        }

        var current = _repository.GetById(customer.Id)
            ?? throw new BusinessException("Không tìm thấy khách hàng.");

        // Điểm và hạng chỉ được thay đổi qua nghiệp vụ tích lũy.
        customer.Points = current.Points;
        customer.Tier = current.Tier;

        if (!_repository.Update(customer))
        {
            throw new BusinessException(
                "Không thể cập nhật khách hàng.");
        }
    }

    public void AddPointsAndUpgradeTier(
        int customerId,
        int points)
    {
        AuthorizationGuard.EnsureAdministrator(_session, "điều chỉnh điểm khách hàng");
        if (customerId <= 0 || points <= 0)
        {
            throw new BusinessException(
                "Mã khách hàng hoặc điểm tích lũy không hợp lệ.");
        }

        if (_repository.GetById(customerId) is null)
        {
            throw new BusinessException(
                "Không tìm thấy khách hàng.");
        }

        if (!_repository.AddPointsAndUpdateTier(customerId, points))
        {
            throw new BusinessException(
                "Không thể cập nhật điểm tích lũy và hạng thành viên.");
        }
    }

    public IEnumerable<CustomerPurchaseHistoryDto>
        GetPurchaseHistory(int customerId)
    {
        AuthorizationGuard.EnsureAuthenticated(_session);
        if (customerId <= 0)
            throw new BusinessException("Mã khách hàng không hợp lệ.");

        if (_repository.GetById(customerId) is null)
            throw new BusinessException("Không tìm thấy khách hàng.");

        return _repository.GetPurchaseHistory(customerId);
    }

    private static void Validate(Customer customer)
    {
        if (customer is null)
        {
            throw new BusinessException(
                "Thông tin khách hàng không hợp lệ.");
        }

        if (string.IsNullOrWhiteSpace(customer.FullName) ||
            customer.FullName.Trim().Length < 2 ||
            customer.FullName.Trim().Length > 100)
        {
            throw new BusinessException(
                "Họ tên phải có từ 2 đến 100 ký tự.");
        }

        if (string.IsNullOrWhiteSpace(customer.Phone))
        {
            throw new BusinessException(
                "Số điện thoại không được để trống.");
        }

        var phone = customer.Phone.Trim();

        if (phone.Length is < 9 or > 11 ||
            !phone.All(char.IsDigit))
        {
            throw new BusinessException(
                "Số điện thoại phải gồm 9 đến 11 chữ số.");
        }

        if (!string.IsNullOrWhiteSpace(customer.Email))
        {
            try
            {
                var address = new MailAddress(customer.Email.Trim());

                if (!string.Equals(
                    address.Address,
                    customer.Email.Trim(),
                    StringComparison.OrdinalIgnoreCase))
                {
                    throw new BusinessException(
                        "Địa chỉ email không hợp lệ.");
                }
            }
            catch (FormatException)
            {
                throw new BusinessException(
                    "Địa chỉ email không hợp lệ.");
            }
        }
    }
}
