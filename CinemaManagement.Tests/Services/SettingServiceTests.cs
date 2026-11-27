using CinemaManagement.BLL.Services;
using CinemaManagement.BLL.Session;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.DAL.Database;
using CinemaManagement.DAL.Repositories;
using Dapper;
using Microsoft.Data.Sqlite;

namespace CinemaManagement.Tests.Services;

[TestFixture]
public sealed class SettingServiceTests
{
    [Test]
    public void SaveOperationalSettings_StoresAllValuesTogether()
    {
        using var database = new TemporarySettingsDatabase();
        var session = CreateAdminSession();
        var service = new SettingService(
            new SettingRepository(new SqliteConnectionFactory(database.Path)),
            session);

        service.SaveOperationalSettings("Cinema North", 5, 30);

        Assert.Multiple(() =>
        {
            Assert.That(service.GetCinemaName(), Is.EqualTo("Cinema North"));
            Assert.That(service.GetHoldMinutes(), Is.EqualTo(5));
            Assert.That(service.GetCleaningMinutes(), Is.EqualTo(30));
        });
    }

    [Test]
    public void SaveOperationalSettings_RejectsInvalidValuesBeforeWriting()
    {
        using var database = new TemporarySettingsDatabase();
        var service = new SettingService(
            new SettingRepository(new SqliteConnectionFactory(database.Path)),
            CreateAdminSession());

        Assert.Throws<BusinessException>(() =>
            service.SaveOperationalSettings("", 12, 30));
        Assert.Throws<BusinessException>(() =>
            service.SaveOperationalSettings("Cinema North", 2, 30));
        Assert.Throws<BusinessException>(() =>
            service.SaveOperationalSettings("Cinema North", 6, 30));

        using var connection = new SqliteConnectionFactory(database.Path).CreateConnection();
        Assert.That(connection.QuerySingle<int>("SELECT COUNT(*) FROM Settings"), Is.Zero);
    }

    private static CurrentUserSession CreateAdminSession()
    {
        var session = new CurrentUserSession();
        session.Set(new User { Id = 1, Username = "admin", Role = UserRole.Admin });
        return session;
    }

    private sealed class TemporarySettingsDatabase : IDisposable
    {
        private readonly string _directory = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), $"cinema-settings-tests-{Guid.NewGuid():N}");

        public string Path { get; }

        public TemporarySettingsDatabase()
        {
            Directory.CreateDirectory(_directory);
            Path = System.IO.Path.Combine(_directory, "settings.db");
            using var connection = new SqliteConnectionFactory(Path).CreateConnection();
            connection.Execute("CREATE TABLE Settings ([Key] TEXT PRIMARY KEY, [Value] TEXT NOT NULL, Description TEXT);");
        }

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, recursive: true);
        }
    }
}
