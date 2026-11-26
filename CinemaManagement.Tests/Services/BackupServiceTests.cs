using Dapper;
using Microsoft.Data.Sqlite;
using CinemaManagement.BLL.Services;
using CinemaManagement.BLL.Session;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.DAL.Database;

namespace CinemaManagement.Tests.Services;

[TestFixture]
public sealed class BackupServiceTests
{
    private string _directory = null!;
    private string _activePath = null!;
    private BackupService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"cinema-backup-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
        _activePath = Path.Combine(_directory, "active.db");
        CreateDatabase(_activePath);

        var session = new CurrentUserSession();
        session.Set(new User { Id = 1, Username = "admin", Role = UserRole.Admin });
        _service = new BackupService(new SqliteConnectionFactory(_activePath), session);
    }

    [TearDown]
    public void TearDown()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    [Test]
    public void Backup_CreatesValidatedCopyWithoutChangingSource()
    {
        InsertMarker(_activePath, "source-marker");
        var backupPath = Path.Combine(_directory, "backup.db");

        _service.Backup(backupPath);

        Assert.That(File.Exists(backupPath), Is.True);
        Assert.That(ReadMarker(backupPath), Is.EqualTo("source-marker"));
        Assert.That(ReadMarker(_activePath), Is.EqualTo("source-marker"));
    }

    [Test]
    public void Restore_ReplacesOnlyTheIsolatedActiveDatabase()
    {
        var backupPath = Path.Combine(_directory, "backup.db");
        CreateDatabase(backupPath);
        InsertMarker(backupPath, "restored-marker");
        InsertMarker(_activePath, "old-marker");

        _service.Restore(backupPath);

        Assert.That(ReadMarker(_activePath), Is.EqualTo("restored-marker"));
    }

    [Test]
    public void Backup_RejectsStaffBeforeCreatingDestination()
    {
        var staff = new CurrentUserSession();
        staff.Set(new User { Id = 2, Username = "staff", Role = UserRole.Staff });
        var service = new BackupService(new SqliteConnectionFactory(_activePath), staff);
        var destination = Path.Combine(_directory, "denied.db");

        Assert.Throws<BusinessException>(() => service.Backup(destination));
        Assert.That(File.Exists(destination), Is.False);
    }

    private static void CreateDatabase(string path)
    {
        var schema = typeof(SqliteConnectionFactory).Assembly
            .GetManifestResourceNames()
            .Single(name => name.EndsWith("Database.Schema.sql", StringComparison.Ordinal));
        using var resource = typeof(SqliteConnectionFactory).Assembly
            .GetManifestResourceStream(schema)!;
        using var reader = new StreamReader(resource);
        var sql = reader.ReadToEnd();

        using var connection = new SqliteConnection($"Data Source={path};Foreign Keys=True");
        connection.Open();
        connection.Execute(sql);
    }

    private static void InsertMarker(string path, string value)
    {
        using var connection = new SqliteConnection($"Data Source={path};Foreign Keys=True");
        connection.Open();
        connection.Execute(
            "INSERT INTO Settings([Key], [Value], Description) VALUES ('test-marker', @Value, 'isolated test');",
            new { Value = value });
    }

    private static string ReadMarker(string path)
    {
        using var connection = new SqliteConnection(
            $"Data Source={path};Mode=ReadOnly;Foreign Keys=True");
        connection.Open();
        return connection.ExecuteScalar<string>(
            "SELECT [Value] FROM Settings WHERE [Key] = 'test-marker';")!;
    }
}
