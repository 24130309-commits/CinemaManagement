using Dapper;

namespace CinemaManagement.DAL.Database;

/// <summary>
/// Verifies that the configured, existing database is available.
/// The application treats its schema as a fixed contract and must not
/// create, migrate, or rewrite it during startup.
/// </summary>
public sealed class DbInitializer
{
    private readonly SqliteConnectionFactory _factory;

    public DbInitializer(SqliteConnectionFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public void Initialize()
    {
        if (string.IsNullOrWhiteSpace(_factory.DatabasePath) ||
            !File.Exists(_factory.DatabasePath))
        {
            throw new FileNotFoundException(
                "Không tìm thấy cơ sở dữ liệu hiện có. Ứng dụng không tự tạo hoặc thay thế database.",
                _factory.DatabasePath);
        }

        using var connection = _factory.CreateConnection();

        var tableCount = connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%';");

        if (tableCount == 0)
        {
            throw new InvalidOperationException(
                "Cơ sở dữ liệu hiện có không chứa bảng ứng dụng nào. Không có thay đổi nào được thực hiện.");
        }
    }
}
