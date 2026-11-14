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
        using var connection = ConnectionFactory.CreateConnection();

        connection.Execute(
            @"INSERT INTO AuditLogs
              (UserId, Action, Detail, CreatedAt)
              VALUES
              (@UserId, @Action, @Detail, @CreatedAt)",
            log);
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
                AND CreatedAt >= @from
                AND CreatedAt <= @to
              ORDER BY CreatedAt DESC",
            new
            {
                userId,
                from = from.ToString("O"),
                to = to.ToString("O")
            }).AsList();
    }

    public override int Insert(AuditLog log)
    {
        using var connection = ConnectionFactory.CreateConnection();

        return connection.ExecuteScalar<int>(
            @"INSERT INTO AuditLogs
              (UserId, Action, Detail, CreatedAt)
              VALUES
              (@UserId, @Action, @Detail, @CreatedAt);

              SELECT last_insert_rowid();",
            log);
    }

    public override bool Update(AuditLog log)
    {
        throw new NotSupportedException(
            "Nhật ký hoạt động không hỗ trợ chỉnh sửa.");
    }

    public override bool Delete(int id)
    {
        throw new NotSupportedException(
            "Nhật ký hoạt động không hỗ trợ xóa trực tiếp.");
    }
}