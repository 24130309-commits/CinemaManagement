
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
        ArgumentNullException.ThrowIfNull(entity);

        const string sql = """
            INSERT INTO Promotions
                (Code, Description, Type, Value, MaxDiscountAmount,
                 StartDate, EndDate, MinOrder, UsageLimit, UsedCount, IsActive)
            VALUES
                (@Code, @Description, @Type, @Value, @MaxDiscountAmount,
                 @StartDate, @EndDate, @MinOrder, @UsageLimit, @UsedCount, @IsActive);

            SELECT last_insert_rowid();
            """;

        using var connection = ConnectionFactory.CreateConnection();

        return connection.ExecuteScalar<int>(sql, new
        {
            entity.Code,
            entity.Description,
            entity.Type,
            entity.Value,
            entity.MaxDiscountAmount,
            StartDate = entity.StartDate.ToString("O"),
            EndDate = entity.EndDate.ToString("O"),
            entity.MinOrder,
            entity.UsageLimit,
            entity.UsedCount,
            entity.IsActive
        });
    }

    public override bool Update(Promotion entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        const string sql = """
            UPDATE Promotions
            SET Code = @Code,
                Description = @Description,
                Type = @Type,
                Value = @Value,
                MaxDiscountAmount = @MaxDiscountAmount,
                StartDate = @StartDate,
                EndDate = @EndDate,
                MinOrder = @MinOrder,
                UsageLimit = @UsageLimit,
                UsedCount = @UsedCount,
                IsActive = @IsActive
            WHERE Id = @Id;
            """;

        using var connection = ConnectionFactory.CreateConnection();

        return connection.Execute(sql, new
        {
            entity.Id,
            entity.Code,
            entity.Description,
            entity.Type,
            entity.Value,
            entity.MaxDiscountAmount,
            StartDate = entity.StartDate.ToString("O"),
            EndDate = entity.EndDate.ToString("O"),
            entity.MinOrder,
            entity.UsageLimit,
            entity.UsedCount,
            entity.IsActive
        }) > 0;
    }

    public override bool Delete(int id)
    {
        using var connection = ConnectionFactory.CreateConnection();

        return connection.Execute(
            "DELETE FROM Promotions WHERE Id = @Id;",
            new { Id = id }) > 0;
    }

    public Promotion? GetByCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        using var connection = ConnectionFactory.CreateConnection();

        return connection.QuerySingleOrDefault<Promotion>(
            """
            SELECT *
            FROM Promotions
            WHERE Code = @Code COLLATE NOCASE;
            """,
            new { Code = code.Trim() });
    }

    public bool IsCodeExists(string code, int? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        using var connection = ConnectionFactory.CreateConnection();

        return connection.ExecuteScalar<int>(
            """
            SELECT COUNT(1)
            FROM Promotions
            WHERE Code = @Code COLLATE NOCASE
              AND (@ExcludeId IS NULL OR Id <> @ExcludeId);
            """,
            new
            {
                Code = code.Trim(),
                ExcludeId = excludeId
            }) > 0;
    }
}
