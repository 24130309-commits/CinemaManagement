
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
            @"SELECT Id, Username, PasswordHash, FullName, Role, IsActive
              FROM Users
              WHERE Username = @username COLLATE NOCASE",
            new { username });
    }

    public bool IsUsernameExists(string username, int? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(username))
            return false;

        using var connection = ConnectionFactory.CreateConnection();

        return connection.ExecuteScalar<long>(
            @"SELECT COUNT(1)
              FROM Users
              WHERE Username = @username COLLATE NOCASE
                AND (@excludeId IS NULL OR Id <> @excludeId)",
            new { username, excludeId }) > 0;
    }

    public override int Insert(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        using var connection = ConnectionFactory.CreateConnection();

        return connection.ExecuteScalar<int>(
            @"INSERT INTO Users
                (Username, PasswordHash, FullName, Role, IsActive)
              VALUES
                (@Username, @PasswordHash, @FullName, @Role, @IsActive);
              SELECT last_insert_rowid();",
            new
            {
                user.Username,
                user.PasswordHash,
                user.FullName,
                Role = (int)user.Role,
                IsActive = user.IsActive ? 1 : 0
            });
    }

    public override bool Update(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        using var connection = ConnectionFactory.CreateConnection();

        return connection.Execute(
            @"UPDATE Users
              SET Username = @Username,
                  FullName = @FullName,
                  Role = @Role,
                  IsActive = @IsActive
              WHERE Id = @Id",
            new
            {
                user.Id,
                user.Username,
                user.FullName,
                Role = (int)user.Role,
                IsActive = user.IsActive ? 1 : 0
            }) > 0;
    }

    public override bool Delete(int id)
    {
        using var connection = ConnectionFactory.CreateConnection();

        return connection.Execute(
            "DELETE FROM Users WHERE Id = @id",
            new { id }) > 0;
    }

    public bool UpdatePassword(int id, string newHash)
    {
        if (string.IsNullOrWhiteSpace(newHash))
            throw new ArgumentException(
                "Password hash không được để trống.",
                nameof(newHash));

        using var connection = ConnectionFactory.CreateConnection();

        return connection.Execute(
            "UPDATE Users SET PasswordHash = @newHash WHERE Id = @id",
            new { id, newHash }) > 0;
    }

    public bool SetActive(int id, bool isActive)
    {
        using var connection = ConnectionFactory.CreateConnection();

        return connection.Execute(
            "UPDATE Users SET IsActive = @isActive WHERE Id = @id",
            new { id, isActive = isActive ? 1 : 0 }) > 0;
    }
}
