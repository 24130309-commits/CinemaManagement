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
        using var connection = ConnectionFactory.CreateConnection();

        return connection.QuerySingleOrDefault<User>(
            @"SELECT *
              FROM Users
              WHERE Username = @username COLLATE NOCASE",
            new { username });
    }

    public override int Insert(User user)
    {
        using var connection = ConnectionFactory.CreateConnection();

        return connection.ExecuteScalar<int>(
            @"INSERT INTO Users
              (Username, PasswordHash, FullName, Role, IsActive)
              VALUES
              (@Username, @PasswordHash, @FullName, @Role, @IsActive);

              SELECT last_insert_rowid();",
            user);
    }

    public override bool Update(User user)
    {
        using var connection = ConnectionFactory.CreateConnection();

        return connection.Execute(
            @"UPDATE Users
              SET Username = @Username,
                  FullName = @FullName,
                  Role = @Role,
                  IsActive = @IsActive
              WHERE Id = @Id",
            user) > 0;
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
        using var connection = ConnectionFactory.CreateConnection();

        return connection.Execute(
            @"UPDATE Users
              SET PasswordHash = @newHash
              WHERE Id = @id",
            new { id, newHash }) > 0;
    }

    public bool SetActive(int id, bool isActive)
    {
        using var connection = ConnectionFactory.CreateConnection();

        return connection.Execute(
            @"UPDATE Users
              SET IsActive = @isActive
              WHERE Id = @id",
            new { id, isActive }) > 0;
    }
}