
using Dapper;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.DAL.Database;

namespace CinemaManagement.DAL.Repositories;

public sealed class PromotionRepository
    : BaseRepository<Promotion>, IPromotionRepository
{
    public PromotionRepository(SqliteConnectionFactory factory)
        : base(factory, "Promotions")
    {
    }

    public override int Insert(Promotion entity)
    {
        const string sql = """
            INSERT INTO Promotions
                (Code, Type, Value, StartDate, EndDate,
                 MinOrder, UsageLimit, UsedCount)
            VALUES
                (@Code, @Type, @Value, @StartDate, @EndDate,
                 @MinOrder, @UsageLimit, @UsedCount);

            SELECT last_insert_rowid();
            """;

        using var connection = ConnectionFactory.CreateConnection();

        return connection.ExecuteScalar<int>(sql, new
        {
            entity.Code,
            entity.Type,
            entity.Value,
            StartDate = entity.StartDate.ToString("yyyy-MM-dd HH:mm:ss"),
            EndDate = entity.EndDate.ToString("yyyy-MM-dd HH:mm:ss"),
            entity.MinOrder,
            entity.UsageLimit,
            entity.UsedCount
        });
    }

    public override bool Update(Promotion entity)
    {
        const string sql = """
            UPDATE Promotions
            SET Code = @Code,
                Type = @Type,
                Value = @Value,
                StartDate = @StartDate,
                EndDate = @EndDate,
                MinOrder = @MinOrder,
                UsageLimit = @UsageLimit,
                UsedCount = @UsedCount
            WHERE Id = @Id;
            """;

        using var connection = ConnectionFactory.CreateConnection();

        return connection.Execute(sql, new
        {
            entity.Id,
            entity.Code,
            entity.Type,
            entity.Value,
            StartDate = entity.StartDate.ToString("yyyy-MM-dd HH:mm:ss"),
            EndDate = entity.EndDate.ToString("yyyy-MM-dd HH:mm:ss"),
            entity.MinOrder,
            entity.UsageLimit,
            entity.UsedCount
        }) > 0;
    }

    public override bool Delete(int id)
    {
        using var connection = ConnectionFactory.CreateConnection();

        return connection.Execute(
            "DELETE FROM Promotions WHERE Id = @id;",
            new { id }) > 0;
    }

    public Promotion? GetByCode(string code)
    {
        using var connection = ConnectionFactory.CreateConnection();

        return connection.QuerySingleOrDefault<Promotion>(
            """
            SELECT *
            FROM Promotions
            WHERE Code = @code COLLATE NOCASE;
            """,
            new { code = code.Trim() });
    }

    public bool IsCodeExists(string code, int? excludeId = null)
    {
        using var connection = ConnectionFactory.CreateConnection();

        return connection.ExecuteScalar<int>(
            """
            SELECT COUNT(1)
            FROM Promotions
            WHERE Code = @code COLLATE NOCASE
              AND (@excludeId IS NULL OR Id <> @excludeId);
            """,
            new { code = code.Trim(), excludeId }) > 0;
    }
}
