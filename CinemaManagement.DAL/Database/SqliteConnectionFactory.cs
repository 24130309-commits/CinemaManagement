
using Microsoft.Data.Sqlite;
using CinemaManagement.Core.Storage;

namespace CinemaManagement.DAL.Database;

public sealed class SqliteConnectionFactory
{
    public string DatabasePath { get; }

    private readonly string? _connectionString;

    public SqliteConnectionFactory(string? databasePath = null)
    {
        if (!string.IsNullOrWhiteSpace(databasePath))
        {
            DatabasePath = Path.GetFullPath(databasePath);
        }
        else
        {
            DatabasePath = StoragePathResolver.DatabasePath;
        }

        var directory = Path.GetDirectoryName(DatabasePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private SqliteConnectionFactory(
        string connectionString,
        bool useConnectionString)
    {
        _connectionString = connectionString;
        DatabasePath = string.Empty;
    }

    public static SqliteConnectionFactory FromConnectionString(
        string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException(
                "Connection string không được để trống.",
                nameof(connectionString));
        }

        return new SqliteConnectionFactory(connectionString, true);
    }

    public SqliteConnection CreateConnection()
    {
        var connectionString = _connectionString;

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = DatabasePath,
                ForeignKeys = true,
                Pooling = true,
                DefaultTimeout = 30
            };

            connectionString = builder.ToString();
        }

        var connection = new SqliteConnection(connectionString);

        try
        {
            connection.Open();

            using var command = connection.CreateCommand();

            command.CommandText = """
                PRAGMA foreign_keys = ON;
                PRAGMA busy_timeout = 30000;
                """;

            command.ExecuteNonQuery();

            return connection;
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }
}
