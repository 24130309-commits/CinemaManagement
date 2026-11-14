using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.DAL.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly SqliteConnectionFactory _factory;
        public UserRepository(SqliteConnectionFactory factory) => _factory = factory;

        public IEnumerable<User> GetAll()
        {
            var list = new List<User>();
            using var conn = _factory.CreateConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Username, PasswordHash, FullName, Email, Role, IsActive, CreatedAt FROM Users";
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) list.Add(MapUser(reader));
            return list;
        }

        public User GetById(int id)
        {
            using var conn = _factory.CreateConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Username, PasswordHash, FullName, Email, Role, IsActive, CreatedAt FROM Users WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? MapUser(reader) : null;
        }

        public User GetByUsername(string username)
        {
            using var conn = _factory.CreateConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Username, PasswordHash, FullName, Email, Role, IsActive, CreatedAt FROM Users WHERE Username = @username";
            cmd.Parameters.AddWithValue("@username", username);
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? MapUser(reader) : null;
        }

        public bool IsUsernameExists(string username, int? excludeId = null)
        {
            using var conn = _factory.CreateConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = excludeId.HasValue
                ? "SELECT COUNT(1) FROM Users WHERE Username = @username AND Id != @id"
                : "SELECT COUNT(1) FROM Users WHERE Username = @username";
            cmd.Parameters.AddWithValue("@username", username);
            if (excludeId.HasValue) cmd.Parameters.AddWithValue("@id", excludeId.Value);
            return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
        }

        public void Add(User entity)
        {
            using var conn = _factory.CreateConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO Users (Username, PasswordHash, FullName, Email, Role, IsActive)
                                VALUES (@u, @p, @f, @e, @r, @a)";
            cmd.Parameters.AddWithValue("@u", entity.Username);
            cmd.Parameters.AddWithValue("@p", entity.PasswordHash);
            cmd.Parameters.AddWithValue("@f", entity.FullName);
            cmd.Parameters.AddWithValue("@e", (object)entity.Email ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@r", entity.Role.ToString());
            cmd.Parameters.AddWithValue("@a", entity.IsActive ? 1 : 0);
            cmd.ExecuteNonQuery();
        }

        public void Update(User entity)
        {
            using var conn = _factory.CreateConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"UPDATE Users SET PasswordHash = @p, FullName = @f, Email = @e, Role = @r, IsActive = @a WHERE Id = @id";
            cmd.Parameters.AddWithValue("@p", entity.PasswordHash);
            cmd.Parameters.AddWithValue("@f", entity.FullName);
            cmd.Parameters.AddWithValue("@e", (object)entity.Email ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@r", entity.Role.ToString());
            cmd.Parameters.AddWithValue("@a", entity.IsActive ? 1 : 0);
            cmd.Parameters.AddWithValue("@id", entity.Id);
            cmd.ExecuteNonQuery();
        }

        public void Delete(int id)
        {
            using var conn = _factory.CreateConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Users WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        private static User MapUser(SqliteDataReader r) => new User
        {
            Id = r.GetInt32(0),
            Username = r.GetString(1),
            PasswordHash = r.GetString(2),
            FullName = r.GetString(3),
            Email = r.IsDBNull(4) ? null : r.GetString(4),
            Role = Enum.TryParse<UserRole>(r.GetString(5), out var role) ? role : UserRole.Staff,
            IsActive = r.GetInt32(6) == 1,
            CreatedAt = DateTime.TryParse(r.GetString(7), out var dt) ? dt : DateTime.Now
        };
    }
}