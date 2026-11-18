
using Microsoft.Data.Sqlite;
using CinemaManagement.Core.Constants;

namespace CinemaManagement.DAL.Database;

public sealed class SqliteConnectionFactory
{
    public string DatabasePath { get; }

    private readonly string? _connectionString;

    public SqliteConnectionFactory(string? databasePath = null)
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppConstants.DatabaseDirectoryName);

        Directory.CreateDirectory(root);

        DatabasePath = Path.GetFullPath(
            databasePath ?? Path.Combine(root, AppConstants.DatabaseFileName));

        Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath)!);
    }

    private SqliteConnectionFactory(string connectionString, bool isConnectionString)
    {
        _connectionString = connectionString;
        DatabasePath = string.Empty;
    }

    public static SqliteConnectionFactory FromConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException(
                "Connection string không được để trống.",
                nameof(connectionString));

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
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText =
            "PRAGMA foreign_keys = ON; PRAGMA busy_timeout = 30000;";

        command.ExecuteNonQuery();

        return connection;
    }
}
