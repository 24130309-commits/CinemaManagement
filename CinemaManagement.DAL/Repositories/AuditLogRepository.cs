
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
        Insert(log);
    }

    public override int Insert(AuditLog log)
    {
        ArgumentNullException.ThrowIfNull(log);

        using var connection = ConnectionFactory.CreateConnection();

        return connection.ExecuteScalar<int>(
            @"INSERT INTO AuditLogs
                (UserId, Action, EntityName, EntityId, Detail, CreatedAt)
              VALUES
                (@UserId, @Action, @EntityName, @EntityId, @Detail, @CreatedAt);
              SELECT last_insert_rowid();",
            new
            {
                log.UserId,
                log.Action,
                log.EntityName,
                log.EntityId,
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
            @"SELECT
                a.Id,
                a.UserId,
                COALESCE(u.Username, 'System') AS Username,
                a.Action,
                a.EntityName,
                a.EntityId,
                a.Detail,
                a.CreatedAt
              FROM AuditLogs a
              LEFT JOIN Users u ON a.UserId = u.Id
              WHERE (@userId = 0 OR a.UserId = @userId)
                AND datetime(a.CreatedAt) >= datetime(@from)
                AND datetime(a.CreatedAt) < datetime(@to)
              ORDER BY a.CreatedAt DESC",
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
            @"SELECT
                a.Id,
                a.UserId,
                COALESCE(u.Username, 'System') AS Username,
                a.Action,
                a.EntityName,
                a.EntityId,
                a.Detail,
                a.CreatedAt
              FROM AuditLogs a
              LEFT JOIN Users u ON a.UserId = u.Id
              ORDER BY a.Id DESC
              LIMIT @limit",
            new { limit }).AsList();
    }
}
