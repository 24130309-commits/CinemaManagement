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
            WHERE IsActive=1
            ORDER BY Category, Name
            """).AsList();
    }

    public IEnumerable<Product> GetByCategory(string category)
    {
        using var connection = ConnectionFactory.CreateConnection();

        return connection.Query<Product>(
            """
            SELECT *
            FROM Products
            WHERE Category=@category
            ORDER BY Name
            """,
            new { category }).AsList();
    }

    public override int Insert(Product product)
    {
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
                product.Name,
                product.Price,
                product.Category,
                IsActive = product.IsActive ? 1 : 0
            });
    }

    public override bool Update(Product product)
    {
        using var connection = ConnectionFactory.CreateConnection();

        return connection.Execute(
            """
            UPDATE Products
            SET Name=@Name,
                Price=@Price,
                Category=@Category,
                IsActive=@IsActive
            WHERE Id=@Id
            """,
            new
            {
                product.Id,
                product.Name,
                product.Price,
                product.Category,
                IsActive = product.IsActive ? 1 : 0
            }) > 0;
    }

    public override bool Delete(int id)
    {
        using var connection = ConnectionFactory.CreateConnection();

        return connection.Execute(
            "DELETE FROM Products WHERE Id=@id",
            new { id }) > 0;
    }
}