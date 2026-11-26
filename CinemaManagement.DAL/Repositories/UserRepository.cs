
using Dapper;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.DAL.Database;

namespace CinemaManagement.DAL.Repositories;

public sealed class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(SqliteConnectionFactory factory)
        : base(factory, "Users")
    {
    }

    public User? GetByUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return null;

        using var connection = ConnectionFactory.CreateConnection();

        return connection.QuerySingleOrDefault<User>(
            """
            SELECT *
            FROM Users
            WHERE Username = @Username COLLATE NOCASE;
            """,
            new { Username = username.Trim() });
    }

    public bool IsUsernameExists(
        string username,
        int? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(username))
            return false;

        using var connection = ConnectionFactory.CreateConnection();

        return connection.ExecuteScalar<bool>(
            """
            SELECT EXISTS
            (
                SELECT 1
                FROM Users
                WHERE Username = @Username COLLATE NOCASE
                  AND (@ExcludeId IS NULL OR Id <> @ExcludeId)
            );
            """,
            new
            {
                Username = username.Trim(),
                ExcludeId = excludeId
            });
    }

    public override int Insert(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        ValidateUser(user);

        if (IsUsernameExists(user.Username))
            throw new InvalidOperationException("Tên đăng nhập đã tồn tại.");

        using var connection = ConnectionFactory.CreateConnection();

        return connection.ExecuteScalar<int>(
            """
            INSERT INTO Users
            (
                Username,
                PasswordHash,
                FullName,
                Email,
                Role,
                IsActive,
                CreatedAt
            )
            VALUES
            (
                @Username,
                @PasswordHash,
                @FullName,
                @Email,
                @Role,
                @IsActive,
                @CreatedAt
            );

            SELECT last_insert_rowid();
            """,
            new
            {
                Username = user.Username.Trim(),
                user.PasswordHash,
                FullName = user.FullName.Trim(),
                Email = user.Email?.Trim() ?? string.Empty,
                Role = (int)user.Role,
                IsActive = user.IsActive ? 1 : 0,
                CreatedAt = user.CreatedAt.ToString("O")
            });
    }

    public override bool Update(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (user.Id <= 0)
            return false;

        ValidateUser(user);

        if (IsUsernameExists(user.Username, user.Id))
            throw new InvalidOperationException("Tên đăng nhập đã tồn tại.");

        using var connection = ConnectionFactory.CreateConnection();

        return connection.Execute(
            """
            UPDATE Users
            SET Username = @Username,
                FullName = @FullName,
                Email = @Email,
                Role = @Role,
                IsActive = @IsActive
            WHERE Id = @Id;
            """,
            new
            {
                user.Id,
                Username = user.Username.Trim(),
                FullName = user.FullName.Trim(),
                Email = user.Email?.Trim() ?? string.Empty,
                Role = (int)user.Role,
                IsActive = user.IsActive ? 1 : 0
            }) > 0;
    }

    public override bool Delete(int id)
    {
        if (id <= 0)
            return false;

        using var connection = ConnectionFactory.CreateConnection();

        var hasAuditLogs = connection.ExecuteScalar<bool>(
            """
            SELECT EXISTS
            (
                SELECT 1
                FROM AuditLogs
                WHERE UserId = @Id
            );
            """,
            new { Id = id });

        var hasBookings = connection.ExecuteScalar<bool>(
            """
            SELECT EXISTS
            (
                SELECT 1
                FROM Bookings
                WHERE UserId = @Id
            );
            """,
            new { Id = id });

        if (hasAuditLogs || hasBookings)
            return false;

        return connection.Execute(
            """
            DELETE FROM Users
            WHERE Id = @Id;
            """,
            new { Id = id }) > 0;
    }

    public bool UpdatePassword(int id, string newHash)
    {
        if (id <= 0)
            return false;

        if (string.IsNullOrWhiteSpace(newHash))
        {
            throw new ArgumentException(
                "Password hash không được để trống.",
                nameof(newHash));
        }

        using var connection = ConnectionFactory.CreateConnection();

        return connection.Execute(
            """
            UPDATE Users
            SET PasswordHash = @NewHash
            WHERE Id = @Id;
            """,
            new
            {
                Id = id,
                NewHash = newHash
            }) > 0;
    }

    public bool SetActive(int id, bool isActive)
    {
        if (id <= 0)
            return false;

        using var connection = ConnectionFactory.CreateConnection();

        return connection.Execute(
            """
            UPDATE Users
            SET IsActive = @IsActive
            WHERE Id = @Id;
            """,
            new
            {
                Id = id,
                IsActive = isActive ? 1 : 0
            }) > 0;
    }

    private static void ValidateUser(User user)
    {
        if (string.IsNullOrWhiteSpace(user.Username))
            throw new ArgumentException("Tên đăng nhập không được để trống.");

        if (string.IsNullOrWhiteSpace(user.PasswordHash))
            throw new ArgumentException("Mật khẩu đã mã hóa không được để trống.");

        if (string.IsNullOrWhiteSpace(user.FullName))
            throw new ArgumentException("Họ tên nhân viên không được để trống.");

        if (!Enum.IsDefined(user.Role))
            throw new ArgumentException("Vai trò tài khoản không hợp lệ.");
    }
}
