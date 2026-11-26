
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
        ValidatePromotion(entity);

        if (IsCodeExists(entity.Code))
            throw new InvalidOperationException("Mã khuyến mãi đã tồn tại.");

        using var connection = ConnectionFactory.CreateConnection();

        return connection.ExecuteScalar<int>(
            """
            INSERT INTO Promotions
            (
                Code,
                Description,
                Type,
                Value,
                MaxDiscountAmount,
                StartDate,
                EndDate,
                MinOrder,
                UsageLimit,
                UsedCount,
                IsActive
            )
            VALUES
            (
                @Code,
                @Description,
                @Type,
                @Value,
                @MaxDiscountAmount,
                @StartDate,
                @EndDate,
                @MinOrder,
                @UsageLimit,
                @UsedCount,
                @IsActive
            );

            SELECT last_insert_rowid();
            """,
            new
            {
                Code = entity.Code.Trim(),
                Description = entity.Description,
                entity.Type,
                entity.Value,
                entity.MaxDiscountAmount,
                StartDate = entity.StartDate.ToString("O"),
                EndDate = entity.EndDate.ToString("O"),
                entity.MinOrder,
                entity.UsageLimit,
                entity.UsedCount,
                IsActive = entity.IsActive ? 1 : 0
            });
    }

    public override bool Update(Promotion entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity.Id <= 0)
            return false;

        ValidatePromotion(entity);

        if (IsCodeExists(entity.Code, entity.Id))
            throw new InvalidOperationException("Mã khuyến mãi đã tồn tại.");

        using var connection = ConnectionFactory.CreateConnection();

        return connection.Execute(
            """
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
                IsActive = @IsActive
            WHERE Id = @Id;
            """,
            new
            {
                entity.Id,
                Code = entity.Code.Trim(),
                Description = entity.Description,
                entity.Type,
                entity.Value,
                entity.MaxDiscountAmount,
                StartDate = entity.StartDate.ToString("O"),
                EndDate = entity.EndDate.ToString("O"),
                entity.MinOrder,
                entity.UsageLimit,
                IsActive = entity.IsActive ? 1 : 0
            }) > 0;
    }

    public override bool Delete(int id)
    {
        if (id <= 0)
            return false;

        using var connection = ConnectionFactory.CreateConnection();
        using var transaction = connection.BeginTransaction();

        var usedCount = connection.ExecuteScalar<int?>(
            """
            SELECT UsedCount
            FROM Promotions
            WHERE Id = @Id;
            """,
            new { Id = id },
            transaction);

        if (!usedCount.HasValue)
        {
            transaction.Rollback();
            return false;
        }

        if (usedCount.Value > 0)
        {
            transaction.Rollback();
            return false;
        }

        var affected = connection.Execute(
            """
            DELETE FROM Promotions
            WHERE Id = @Id
              AND UsedCount = 0;
            """,
            new { Id = id },
            transaction);

        transaction.Commit();

        return affected > 0;
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

        return connection.ExecuteScalar<bool>(
            """
            SELECT EXISTS
            (
                SELECT 1
                FROM Promotions
                WHERE Code = @Code COLLATE NOCASE
                  AND (@ExcludeId IS NULL OR Id <> @ExcludeId)
            );
            """,
            new
            {
                Code = code.Trim(),
                ExcludeId = excludeId
            });
    }

    private static void ValidatePromotion(Promotion entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Code))
            throw new ArgumentException("Mã khuyến mãi không được để trống.");

        if (entity.StartDate >= entity.EndDate)
            throw new ArgumentException("Ngày kết thúc phải sau ngày bắt đầu.");

        if (entity.Value <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(entity.Value),
                "Giá trị khuyến mãi phải lớn hơn 0.");

        if (entity.MaxDiscountAmount < 0)
            throw new ArgumentOutOfRangeException(
                nameof(entity.MaxDiscountAmount));

        if (entity.MinOrder < 0)
            throw new ArgumentOutOfRangeException(
                nameof(entity.MinOrder));

        if (entity.UsageLimit < 0)
            throw new ArgumentOutOfRangeException(
                nameof(entity.UsageLimit));

        if (entity.UsedCount < 0)
            throw new ArgumentOutOfRangeException(
                nameof(entity.UsedCount));

        if (entity.UsageLimit > 0 &&
            entity.UsedCount > entity.UsageLimit)
        {
            throw new ArgumentException(
                "Số lượt đã sử dụng không được vượt giới hạn.");
        }
    }
}
