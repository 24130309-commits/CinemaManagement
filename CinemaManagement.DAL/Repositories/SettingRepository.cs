
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
        _factory = factory
            ?? throw new ArgumentNullException(nameof(factory));
    }

    public string Get(string key)
    {
        ValidateKey(key);

        using var connection = _factory.CreateConnection();

        return connection.QuerySingleOrDefault<string>(
            """
            SELECT [Value]
            FROM Settings
            WHERE [Key] = @Key;
            """,
            new { Key = key.Trim() }) ?? string.Empty;
    }

    public AppSetting? GetByKey(string key)
    {
        ValidateKey(key);

        using var connection = _factory.CreateConnection();

        return connection.QuerySingleOrDefault<AppSetting>(
            """
            SELECT [Key], [Value]
            FROM Settings
            WHERE [Key] = @Key;
            """,
            new { Key = key.Trim() });
    }

    public void Set(string key, string value)
    {
        ValidateKey(key);

        ArgumentNullException.ThrowIfNull(value);

        using var connection = _factory.CreateConnection();

        connection.Execute(
            """
            INSERT INTO Settings([Key], [Value])
            VALUES (@Key, @Value)
            ON CONFLICT([Key])
            DO UPDATE SET [Value] = excluded.[Value];
            """,
            new
            {
                Key = key.Trim(),
                Value = value
            });
    }

    public void SetMany(IEnumerable<AppSetting> settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var values = settings.ToList();

        if (values.Count == 0)
            return;

        foreach (var setting in values)
        {
            ArgumentNullException.ThrowIfNull(setting);
            ValidateKey(setting.Key);
            ArgumentNullException.ThrowIfNull(setting.Value);
        }

        if (values.Select(x => x.Key.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != values.Count)
            throw new ArgumentException("Duplicate settings keys are not allowed.", nameof(settings));

        using var connection = _factory.CreateConnection();
        using var transaction = connection.BeginTransaction();

        connection.Execute(
            """
            INSERT INTO Settings([Key], [Value])
            VALUES (@Key, @Value)
            ON CONFLICT([Key])
            DO UPDATE SET [Value] = excluded.[Value];
            """,
            values.Select(setting => new
            {
                Key = setting.Key.Trim(),
                Value = setting.Value
            }),
            transaction);

        transaction.Commit();
    }

    public void Upsert(AppSetting setting)
    {
        ArgumentNullException.ThrowIfNull(setting);

        Set(setting.Key, setting.Value);
    }

    public IEnumerable<AppSetting> GetAll()
    {
        using var connection = _factory.CreateConnection();

        return connection.Query<AppSetting>(
            """
            SELECT [Key], [Value]
            FROM Settings
            ORDER BY [Key] COLLATE NOCASE;
            """).AsList();
    }

    private static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException(
                "Khóa cấu hình không được để trống.",
                nameof(key));
    }
}
