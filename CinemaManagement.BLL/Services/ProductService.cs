
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.BLL.Session;

namespace CinemaManagement.BLL.Services;

public sealed class ProductService : IProductService
{
    private readonly IProductRepository _repository;
    private readonly CurrentUserSession _session;

    public ProductService(
        IProductRepository repository,
        CurrentUserSession session)
    {
        _repository = repository
            ?? throw new ArgumentNullException(nameof(repository));
        _session = session
            ?? throw new ArgumentNullException(nameof(session));
    }

    public IEnumerable<Product> GetAll()
    {
        return _repository.GetAll();
    }

    public IEnumerable<Product> GetActive()
    {
        return _repository.GetActive();
    }

    public Product? GetById(int id)
    {
        if (id <= 0)
            return null;

        return _repository.GetById(id);
    }

    public void Create(Product product)
    {
        AuthorizationGuard.EnsureAdministrator(_session, "quản lý sản phẩm");
        Validate(product);

        product.Name = product.Name.Trim();
        product.Category = product.Category.Trim();

        if (IsNameExists(product.Name))
        {
            throw new BusinessException(
                "Tên sản phẩm đã tồn tại.");
        }

        var id = _repository.Insert(product);

        if (id <= 0)
            throw new BusinessException("Không thể thêm sản phẩm.");

        product.Id = id;
    }

    public void Update(Product product)
    {
        AuthorizationGuard.EnsureAdministrator(_session, "quản lý sản phẩm");
        Validate(product);

        if (product.Id <= 0)
            throw new BusinessException("Mã sản phẩm không hợp lệ.");

        product.Name = product.Name.Trim();
        product.Category = product.Category.Trim();

        _ = _repository.GetById(product.Id)
            ?? throw new BusinessException("Không tìm thấy sản phẩm.");

        if (!_repository.Update(product))
        {
            throw new BusinessException(
                "Không thể cập nhật sản phẩm.");
        }
    }

    public void Delete(int id)
    {
        AuthorizationGuard.EnsureAdministrator(_session, "quản lý sản phẩm");
        if (id <= 0)
            throw new BusinessException("Mã sản phẩm không hợp lệ.");

        if (_repository.GetById(id) is null)
            throw new BusinessException("Không tìm thấy sản phẩm.");

        if (_repository.HasBookings(id))
        {
            throw new BusinessException(
                "Sản phẩm đã xuất hiện trong hóa đơn nên không thể xóa. " +
                "Hãy chuyển sản phẩm sang trạng thái ngừng kinh doanh.");
        }

        if (!_repository.Delete(id))
        {
            throw new BusinessException(
                "Không thể xóa sản phẩm.");
        }
    }

    private bool IsNameExists(string name, int? excludeId = null)
    {
        return _repository.GetAll().Any(product =>
            product.Id != excludeId &&
            string.Equals(
                product.Name.Trim(),
                name,
                StringComparison.OrdinalIgnoreCase));
    }

    private static void Validate(Product product)
    {
        if (product is null)
            throw new BusinessException("Thông tin sản phẩm không hợp lệ.");

        if (string.IsNullOrWhiteSpace(product.Name) ||
            product.Name.Trim().Length > 100)
        {
            throw new BusinessException(
                "Tên sản phẩm không được để trống và không vượt quá 100 ký tự.");
        }

        if (string.IsNullOrWhiteSpace(product.Category) ||
            product.Category.Trim().Length > 50)
        {
            throw new BusinessException(
                "Danh mục sản phẩm không hợp lệ.");
        }

        if (product.Price < 0)
        {
            throw new BusinessException(
                "Giá sản phẩm không được âm.");
        }
    }
}
