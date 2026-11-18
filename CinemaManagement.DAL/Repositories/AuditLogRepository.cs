
using Dapper;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.DAL.Database;

namespace CinemaManagement.DAL.Repositories;

public sealed class AuditLogRepository
    : BaseRepository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(SqliteConnectionFactory factory)
        : base(factory, "AuditLogs")
    {
    }

    public void Add(AuditLog log)
    {
        ArgumentNullException.ThrowIfNull(log);

        using var connection = ConnectionFactory.CreateConnection();

        connection.Execute(
            @"INSERT INTO AuditLogs(UserId, Action, Detail, CreatedAt)
              VALUES (@UserId, @Action, @Detail, @CreatedAt)",
            new
            {
                log.UserId,
                log.Action,
                log.Detail,
                CreatedAt = log.CreatedAt.ToString("O")
            });
    }

    public override int Insert(AuditLog log)
    {
        ArgumentNullException.ThrowIfNull(log);

        using var connection = ConnectionFactory.CreateConnection();

        return connection.ExecuteScalar<int>(
            @"INSERT INTO AuditLogs(UserId, Action, Detail, CreatedAt)
              VALUES (@UserId, @Action, @Detail, @CreatedAt);
              SELECT last_insert_rowid();",
            new
            {
                log.UserId,
                log.Action,
                log.Detail,
                CreatedAt = log.CreatedAt.ToString("O")
            });
    }

    public IEnumerable<AuditLog> Search(
        int userId,
        DateTime from,
        DateTime to)
    {
        using var connection = ConnectionFactory.CreateConnection();

        return connection.Query<AuditLog>(
            @"SELECT Id, UserId, Action, Detail, CreatedAt
              FROM AuditLogs
              WHERE (@userId = 0 OR UserId = @userId)
                AND datetime(CreatedAt) >= datetime(@from)
                AND datetime(CreatedAt) < datetime(@to)
              ORDER BY CreatedAt DESC",
            new
            {
                userId,
                from = from.ToString("O"),
                to = to.Date.AddDays(1).ToString("O")
            }).AsList();
    }

    public IEnumerable<AuditLog> GetRecentLogs(int limit = 100)
    {
        if (limit < 1)
            throw new ArgumentOutOfRangeException(nameof(limit));

        using var connection = ConnectionFactory.CreateConnection();

        return connection.Query<AuditLog>(
            @"SELECT Id, UserId, Action, Detail, CreatedAt
              FROM AuditLogs
              ORDER BY Id DESC
              LIMIT @limit",
            new { limit }).AsList();
    }
}
