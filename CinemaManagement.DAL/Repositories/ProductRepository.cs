
using Dapper;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.DAL.Database;

namespace CinemaManagement.DAL.Repositories;

public sealed class ProductRepository
    : BaseRepository<Product>, IProductRepository
{
    public ProductRepository(SqliteConnectionFactory factory)
        : base(factory, "Products")
    {
    }

    public IEnumerable<Product> GetActive()
    {
        using var connection = ConnectionFactory.CreateConnection();

        return connection.Query<Product>(
            """
            SELECT *
            FROM Products
            WHERE IsActive = 1
            ORDER BY Category, Name;
            """).AsList();
    }

    public IEnumerable<Product> GetByCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return [];

        using var connection = ConnectionFactory.CreateConnection();

        return connection.Query<Product>(
            """
            SELECT *
            FROM Products
            WHERE Category = @Category
            ORDER BY Name;
            """,
            new { Category = category.Trim() }).AsList();
    }

    public bool HasBookings(int productId)
    {
        if (productId <= 0)
            return false;

        using var connection = ConnectionFactory.CreateConnection();

        return connection.ExecuteScalar<bool>(
            """
            SELECT EXISTS
            (
                SELECT 1
                FROM BookingProducts
                WHERE ProductId = @ProductId
            );
            """,
            new { ProductId = productId });
    }

    public override int Insert(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);
        ValidateProduct(product);

        using var connection = ConnectionFactory.CreateConnection();

        return connection.ExecuteScalar<int>(
            """
            INSERT INTO Products(
                Name,
                Price,
                Category,
                IsActive
            )
            VALUES(
                @Name,
                @Price,
                @Category,
                @IsActive
            );

            SELECT last_insert_rowid();
            """,
            new
            {
                Name = product.Name.Trim(),
                product.Price,
                Category = product.Category.Trim(),
                IsActive = product.IsActive ? 1 : 0
            });
    }

    public override bool Update(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (product.Id <= 0)
            return false;

        ValidateProduct(product);

        using var connection = ConnectionFactory.CreateConnection();

        return connection.Execute(
            """
            UPDATE Products
            SET Name = @Name,
                Price = @Price,
                Category = @Category,
                IsActive = @IsActive
            WHERE Id = @Id;
            """,
            new
            {
                product.Id,
                Name = product.Name.Trim(),
                product.Price,
                Category = product.Category.Trim(),
                IsActive = product.IsActive ? 1 : 0
            }) > 0;
    }

    public override bool Delete(int id)
    {
        if (id <= 0)
            return false;

        using var connection = ConnectionFactory.CreateConnection();

        var hasBookings = connection.ExecuteScalar<bool>(
            """
            SELECT EXISTS
            (
                SELECT 1
                FROM BookingProducts
                WHERE ProductId = @Id
            );
            """,
            new { Id = id });

        if (hasBookings)
            return false;

        return connection.Execute(
            """
            DELETE FROM Products
            WHERE Id = @Id;
            """,
            new { Id = id }) > 0;
    }

    private static void ValidateProduct(Product product)
    {
        if (string.IsNullOrWhiteSpace(product.Name))
            throw new ArgumentException("Tên sản phẩm không được để trống.");

        if (string.IsNullOrWhiteSpace(product.Category))
            throw new ArgumentException("Danh mục sản phẩm không được để trống.");

        if (product.Price < 0)
            throw new ArgumentOutOfRangeException(
                nameof(product.Price),
                "Giá sản phẩm không được âm.");
    }
}
