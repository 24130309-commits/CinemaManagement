
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
        if (string.IsNullOrWhiteSpace(phone))
            return null;

        using var connection = ConnectionFactory.CreateConnection();

        return connection.QuerySingleOrDefault<Customer>(
            """
            SELECT *
            FROM Customers
            WHERE Phone = @Phone;
            """,
            new { Phone = phone.Trim() });
    }

    public IEnumerable<Customer> Search(string keyword)
    {
        var term = keyword?.Trim() ?? string.Empty;

        using var connection = ConnectionFactory.CreateConnection();

        return connection.Query<Customer>(
            """
            SELECT *
            FROM Customers
            WHERE @Term = ''
               OR FullName LIKE @Pattern
               OR Phone LIKE @Pattern
               OR Email LIKE @Pattern
            ORDER BY FullName COLLATE NOCASE;
            """,
            new
            {
                Term = term,
                Pattern = $"%{term}%"
            }).AsList();
    }

    public override int Insert(Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);
        ValidateCustomer(customer);

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
            new
            {
                FullName = customer.FullName.Trim(),
                Phone = customer.Phone.Trim(),
                Email = customer.Email?.Trim() ?? string.Empty,
                customer.Points,
                Tier = string.IsNullOrWhiteSpace(customer.Tier)
                    ? "Standard"
                    : customer.Tier.Trim()
            });
    }

    public override bool Update(Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);

        if (customer.Id <= 0)
            return false;

        ValidateCustomer(customer);

        using var connection = ConnectionFactory.CreateConnection();

        return connection.Execute(
            """
            UPDATE Customers
            SET FullName = @FullName,
                Phone = @Phone,
                Email = @Email,
                Tier = @Tier
            WHERE Id = @Id;
            """,
            new
            {
                customer.Id,
                FullName = customer.FullName.Trim(),
                Phone = customer.Phone.Trim(),
                Email = customer.Email?.Trim() ?? string.Empty,
                Tier = string.IsNullOrWhiteSpace(customer.Tier)
                    ? "Standard"
                    : customer.Tier.Trim()
            }) > 0;
    }

    public override bool Delete(int id)
    {
        if (id <= 0)
            return false;

        using var connection = ConnectionFactory.CreateConnection();

        var hasBookings = connection.ExecuteScalar<bool>(
            """
            SELECT EXISTS(
                SELECT 1
                FROM Bookings
                WHERE CustomerId = @Id
            );
            """,
            new { Id = id });

        if (hasBookings)
            return false;

        return connection.Execute(
            """
            DELETE FROM Customers
            WHERE Id = @Id;
            """,
            new { Id = id }) > 0;
    }

    public bool AddPoints(int customerId, int points)
    {
        if (customerId <= 0 || points <= 0)
            return false;

        using var connection = ConnectionFactory.CreateConnection();

        return connection.Execute(
            """
            UPDATE Customers
            SET Points = Points + @Points
            WHERE Id = @CustomerId;
            """,
            new
            {
                CustomerId = customerId,
                Points = points
            }) > 0;
    }

    public bool UpdateTier(int customerId, string tier)
    {
        if (customerId <= 0 || string.IsNullOrWhiteSpace(tier))
            return false;

        using var connection = ConnectionFactory.CreateConnection();

        return connection.Execute(
            """
            UPDATE Customers
            SET Tier = @Tier
            WHERE Id = @CustomerId;
            """,
            new
            {
                CustomerId = customerId,
                Tier = tier.Trim()
            }) > 0;
    }

    public bool AddPointsAndUpdateTier(
        int customerId,
        int points)
    {
        if (customerId <= 0 || points <= 0)
            return false;

        using var connection = ConnectionFactory.CreateConnection();

        return connection.Execute(
            """
            UPDATE Customers
            SET
                Points = Points + @Points,
                Tier = CASE
                    WHEN Points + @Points >= 5000 THEN 'Platinum'
                    WHEN Points + @Points >= 2000 THEN 'Gold'
                    WHEN Points + @Points >= 500 THEN 'Silver'
                    ELSE 'Standard'
                END
            WHERE Id = @CustomerId;
            """,
            new
            {
                CustomerId = customerId,
                Points = points
            }) > 0;
    }

    public IEnumerable<CustomerPurchaseHistoryDto>
        GetPurchaseHistory(int customerId)
    {
        if (customerId <= 0)
            return [];

        using var connection = ConnectionFactory.CreateConnection();

        return connection.Query<CustomerPurchaseHistoryDto>(
            """
            SELECT
                b.Id AS BookingId,
                b.Code AS BookingCode,
                b.CreatedAt,
                b.Total,
                CASE b.Status
                    WHEN 0 THEN 'Chờ xử lý'
                    WHEN 1 THEN 'Đã thanh toán'
                    WHEN 2 THEN 'Đã hủy'
                    WHEN 3 THEN 'Đã hoàn tiền'
                    ELSE 'Không xác định'
                END AS Status,
                COUNT(t.Id) AS TicketCount
            FROM Bookings b
            LEFT JOIN Tickets t ON t.BookingId = b.Id
            WHERE b.CustomerId = @CustomerId
            GROUP BY
                b.Id,
                b.Code,
                b.CreatedAt,
                b.Total,
                b.Status
            ORDER BY b.CreatedAt DESC;
            """,
            new { CustomerId = customerId }).AsList();
    }

    private static void ValidateCustomer(Customer customer)
    {
        if (string.IsNullOrWhiteSpace(customer.FullName))
            throw new ArgumentException("Tên khách hàng không được để trống.");

        if (string.IsNullOrWhiteSpace(customer.Phone))
            throw new ArgumentException("Số điện thoại không được để trống.");

        if (customer.Points < 0)
            throw new ArgumentOutOfRangeException(
                nameof(customer.Points),
                "Điểm thành viên không được âm.");
    }
}
