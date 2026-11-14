using Dapper;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.DAL.Database;

namespace CinemaManagement.DAL.Repositories;

public sealed class SettingRepository : ISettingRepository
{
    private readonly SqliteConnectionFactory _factory;

    public SettingRepository(SqliteConnectionFactory factory)
    {
        _factory = factory;
    }

    public string Get(string key)
    {
        using var connection = _factory.CreateConnection();

        return connection.QuerySingleOrDefault<string>(
            @"SELECT Value
              FROM Settings
              WHERE [Key] = @key",
            new { key }) ?? string.Empty;
    }

    public void Set(string key, string value)
    {
        using var connection = _factory.CreateConnection();

        connection.Execute(
            @"INSERT INTO Settings([Key], [Value])
              VALUES (@key, @value)
              ON CONFLICT([Key])
              DO UPDATE SET Value = excluded.Value",
            new { key, value });
    }

    public IEnumerable<AppSetting> GetAll()
    {
        using var connection = _factory.CreateConnection();

        return connection.Query<AppSetting>(
            @"SELECT [Key], [Value]
              FROM Settings
              ORDER BY [Key]").AsList();
    }
}