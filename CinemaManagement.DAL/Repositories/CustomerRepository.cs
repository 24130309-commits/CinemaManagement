using Dapper;
using CinemaManagement.Core.DTOS;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.DAL.Database;

namespace CinemaManagement.DAL.Repositories;

public sealed class CustomerRepository
    : BaseRepository<Customer>, ICustomerRepository
{
    public CustomerRepository(SqliteConnectionFactory factory)
        : base(factory, "Customers")
    {
    }

    public Customer? GetByPhone(string phone)
    {
        using var connection = ConnectionFactory.CreateConnection();

        return connection.QuerySingleOrDefault<Customer>(
            "SELECT * FROM Customers WHERE Phone=@phone",
            new { phone = phone.Trim() });
    }

    public IEnumerable<Customer> Search(string keyword)
    {
        var term = keyword?.Trim() ?? string.Empty;

        using var connection = ConnectionFactory.CreateConnection();

        return connection.Query<Customer>(
            """
            SELECT *
            FROM Customers
            WHERE @term=''
               OR FullName LIKE @pattern
               OR Phone LIKE @pattern
            ORDER BY FullName
            """,
            new
            {
                term,
                pattern = $"%{term}%"
            }).AsList();
    }

    public override int Insert(Customer customer)
    {
        using var connection = ConnectionFactory.CreateConnection();

        return connection.ExecuteScalar<int>(
            """
            INSERT INTO Customers(
                FullName,
                Phone,
                Email,
                Points,
                Tier
            )
            VALUES(
                @FullName,
                @Phone,
                @Email,
                @Points,
                @Tier
            );

            SELECT last_insert_rowid();
            """,
            customer);
    }

    public override bool Update(Customer customer)
    {
        using var connection = ConnectionFactory.CreateConnection();

        return connection.Execute(
            """
            UPDATE Customers
            SET FullName=@FullName,
                Phone=@Phone,
                Email=@Email,
                Tier=@Tier
            WHERE Id=@Id
            """,
            customer) > 0;
    }

    public override bool Delete(int id)
    {
        using var connection = ConnectionFactory.CreateConnection();

        return connection.Execute(
            "DELETE FROM Customers WHERE Id=@id",
            new { id }) > 0;
    }

    public bool AddPoints(int customerId, int points)
    {
        using var connection = ConnectionFactory.CreateConnection();

        return connection.Execute(
            """
            UPDATE Customers
            SET Points=Points+@points
            WHERE Id=@customerId
            """,
            new
            {
                customerId,
                points
            }) > 0;
    }

    public bool UpdateTier(int customerId, string tier)
    {
        using var connection = ConnectionFactory.CreateConnection();

        return connection.Execute(
            """
            UPDATE Customers
            SET Tier=@tier
            WHERE Id=@customerId
            """,
            new
            {
                customerId,
                tier
            }) > 0;
    }

    public IEnumerable<CustomerPurchaseHistoryDto>
        GetPurchaseHistory(int customerId)
    {
        using var connection = ConnectionFactory.CreateConnection();

        return connection.Query<CustomerPurchaseHistoryDto>(
            """
            SELECT
                b.Id AS BookingId,
                b.Code AS BookingCode,
                b.CreatedAt,
                b.Total,
                CAST(b.Status AS TEXT) AS Status,
                COUNT(t.Id) AS TicketCount
            FROM Bookings b
            LEFT JOIN Tickets t ON t.BookingId=b.Id
            WHERE b.CustomerId=@customerId
            GROUP BY
                b.Id,
                b.Code,
                b.CreatedAt,
                b.Total,
                b.Status
            ORDER BY b.CreatedAt DESC
            """,
            new { customerId }).AsList();
    }
}