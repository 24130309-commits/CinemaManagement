
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

        if (string.IsNullOrWhiteSpace(log.Action))
            throw new ArgumentException("Hành động nhật ký không được để trống.");

        using var connection = ConnectionFactory.CreateConnection();

        return connection.ExecuteScalar<int>(
            """
            INSERT INTO AuditLogs
            (
                UserId,
                Username,
                Action,
                EntityName,
                EntityId,
                Detail,
                CreatedAt
            )
            VALUES
            (
                @UserId,
                @Username,
                @Action,
                @EntityName,
                @EntityId,
                @Detail,
                @CreatedAt
            );

            SELECT last_insert_rowid();
            """,
            new
            {
                log.UserId,
                Username = string.IsNullOrWhiteSpace(log.Username)
                    ? "System"
                    : log.Username.Trim(),
                Action = log.Action.Trim(),
                log.EntityName,
                log.EntityId,
                log.Detail,
                CreatedAt = log.CreatedAt.ToString("O")
            });
    }

    public override IEnumerable<AuditLog> GetAll()
    {
        using var connection = ConnectionFactory.CreateConnection();

        return connection.Query<AuditLog>(
            """
            SELECT
                Id,
                UserId,
                Username,
                Action,
                EntityName,
                EntityId,
                Detail,
                CreatedAt
            FROM AuditLogs
            ORDER BY CreatedAt DESC, Id DESC;
            """).AsList();
    }

    public IEnumerable<AuditLog> Search(
        int userId,
        DateTime from,
        DateTime to)
    {
        if (userId < 0)
            throw new ArgumentOutOfRangeException(nameof(userId));

        if (from.Date > to.Date)
            throw new ArgumentException(
                "Ngày bắt đầu không được sau ngày kết thúc.");

        using var connection = ConnectionFactory.CreateConnection();

        return connection.Query<AuditLog>(
            """
            SELECT
                a.Id,
                a.UserId,
                CASE
                    WHEN COALESCE(a.Username, '') <> ''
                        THEN a.Username
                    ELSE COALESCE(u.Username, 'System')
                END AS Username,
                a.Action,
                a.EntityName,
                a.EntityId,
                a.Detail,
                a.CreatedAt
            FROM AuditLogs a
            LEFT JOIN Users u ON a.UserId = u.Id
            WHERE (@UserId = 0 OR a.UserId = @UserId)
              AND datetime(a.CreatedAt) >= datetime(@From)
              AND datetime(a.CreatedAt) < datetime(@To)
            ORDER BY a.CreatedAt DESC, a.Id DESC;
            """,
            new
            {
                UserId = userId,
                From = from.Date.ToString("O"),
                To = to.Date.AddDays(1).ToString("O")
            }).AsList();
    }
}
