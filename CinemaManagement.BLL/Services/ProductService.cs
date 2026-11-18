using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.BLL.Services;

public sealed class ProductService : IProductService
{
    private readonly IProductRepository _repository;

    public ProductService(IProductRepository repository)
    {
        _repository = repository;
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
        return _repository.GetById(id);
    }

    public void Create(Product product)
    {
        Validate(product);

        product.Id = _repository.Insert(product);

        if (product.Id <= 0)
            throw new BusinessException(
                "Không thể thêm sản phẩm.");
    }

    public void Update(Product product)
    {
        Validate(product);

        if (product.Id <= 0 || !_repository.Update(product))
            throw new BusinessException(
                "Không thể cập nhật sản phẩm.");
    }

    public void Delete(int id)
    {
        if (id <= 0 || !_repository.Delete(id))
            throw new BusinessException(
                "Không thể xóa sản phẩm.");
    }

    private static void Validate(Product product)
    {
        if (product == null ||
            string.IsNullOrWhiteSpace(product.Name) ||
            string.IsNullOrWhiteSpace(product.Category) ||
            product.Price < 0)
        {
            throw new BusinessException(
                "Tên, danh mục và giá sản phẩm không hợp lệ.");
        }
    }
}