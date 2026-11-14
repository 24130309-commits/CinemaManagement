using System.Reflection;
using Dapper;

namespace CinemaManagement.DAL.Database;

public sealed class DbInitializer
{
    private readonly SqliteConnectionFactory _factory;

    public DbInitializer(SqliteConnectionFactory factory) => _factory = factory;

    public void Initialize()
    {
        var assembly = Assembly.GetExecutingAssembly();

        var resource = assembly.GetManifestResourceNames()
                           .SingleOrDefault(x => x.EndsWith(
                               "Schema.sql",
                               StringComparison.OrdinalIgnoreCase))
                       ?? throw new InvalidOperationException(
                           "Không tìm thấy tài nguyên Schema.sql.");

        using var stream = assembly.GetManifestResourceStream(resource)!;
        using var reader = new StreamReader(stream);

        var schema = reader.ReadToEnd();

        using var connection = _factory.CreateConnection();

        connection.Execute(schema);
        connection.Execute("PRAGMA journal_mode = WAL;");
    }
}