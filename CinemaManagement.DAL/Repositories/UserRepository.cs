
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

        return connection.ExecuteScalar<long>(
            """
            SELECT COUNT(1)
            FROM Users
            WHERE Username = @Username COLLATE NOCASE
              AND (@ExcludeId IS NULL OR Id <> @ExcludeId);
            """,
            new
            {
                Username = username.Trim(),
                ExcludeId = excludeId
            }) > 0;
    }

    public override int Insert(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

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
                user.Username,
                user.PasswordHash,
                user.FullName,
                user.Email,
                Role = (int)user.Role,
                IsActive = user.IsActive ? 1 : 0,
                CreatedAt = user.CreatedAt.ToString("O")
            });
    }

    public override bool Update(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

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
                user.Username,
                user.FullName,
                user.Email,
                Role = (int)user.Role,
                IsActive = user.IsActive ? 1 : 0
            }) > 0;
    }

    public override bool Delete(int id)
    {
        using var connection = ConnectionFactory.CreateConnection();

        return connection.Execute(
            """
            DELETE FROM Users
            WHERE Id = @Id;
            """,
            new { Id = id }) > 0;
    }

    public bool UpdatePassword(int id, string newHash)
    {
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
}
