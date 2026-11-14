using Microsoft.Data.Sqlite;
using CinemaManagement.Core.Constants;

namespace CinemaManagement.DAL.Database;

public sealed class SqliteConnectionFactory
{
    public string DatabasePath { get; }

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

    public SqliteConnection CreateConnection()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            ForeignKeys = true,
            Pooling = true,
            DefaultTimeout = 30
        };

        var connection = new SqliteConnection(builder.ToString());
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys = ON; PRAGMA busy_timeout = 30000;";
        command.ExecuteNonQuery();

        return connection;
    }
}