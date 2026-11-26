
using Dapper;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.DAL.Database;

namespace CinemaManagement.DAL.Repositories;

public sealed class PricingRuleRepository
    : BaseRepository<PricingRule>, IPricingRuleRepository
{
    public PricingRuleRepository(SqliteConnectionFactory factory)
        : base(factory, "PricingRules")
    {
    }

    public override int Insert(PricingRule entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ValidateRule(entity);

        using var connection = ConnectionFactory.CreateConnection();

        return connection.ExecuteScalar<int>(
            """
            INSERT INTO PricingRules
            (
                Name,
                SeatType,
                ScreenType,
                DayType,
                TimeFrom,
                TimeTo,
                Multiplier,
                ExtraFee
            )
            VALUES
            (
                @Name,
                @SeatType,
                @ScreenType,
                @DayType,
                @TimeFrom,
                @TimeTo,
                @Multiplier,
                @ExtraFee
            );

            SELECT last_insert_rowid();
            """,
            new
            {
                Name = entity.Name.Trim(),
                SeatType = (int)entity.SeatType,
                ScreenType = (int)entity.ScreenType,
                DayType = entity.DayType.Trim(),
                entity.TimeFrom,
                entity.TimeTo,
                entity.Multiplier,
                entity.ExtraFee
            });
    }

    public override bool Update(PricingRule entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity.Id <= 0)
            return false;

        ValidateRule(entity);

        using var connection = ConnectionFactory.CreateConnection();

        return connection.Execute(
            """
            UPDATE PricingRules
            SET Name = @Name,
                SeatType = @SeatType,
                ScreenType = @ScreenType,
                DayType = @DayType,
                TimeFrom = @TimeFrom,
                TimeTo = @TimeTo,
                Multiplier = @Multiplier,
                ExtraFee = @ExtraFee
            WHERE Id = @Id;
            """,
            new
            {
                entity.Id,
                Name = entity.Name.Trim(),
                SeatType = (int)entity.SeatType,
                ScreenType = (int)entity.ScreenType,
                DayType = entity.DayType.Trim(),
                entity.TimeFrom,
                entity.TimeTo,
                entity.Multiplier,
                entity.ExtraFee
            }) > 0;
    }

    public override bool Delete(int id)
    {
        if (id <= 0)
            return false;

        using var connection = ConnectionFactory.CreateConnection();

        return connection.Execute(
            """
            DELETE FROM PricingRules
            WHERE Id = @Id;
            """,
            new { Id = id }) > 0;
    }

    public IEnumerable<PricingRule> GetByDayAndSeatType(
        string dayType,
        string seatType)
    {
        if (string.IsNullOrWhiteSpace(dayType) ||
            !Enum.TryParse<SeatType>(
                seatType,
                true,
                out var parsedSeatType) ||
            !Enum.IsDefined(parsedSeatType))
        {
            return [];
        }

        using var connection = ConnectionFactory.CreateConnection();

        return connection.Query<PricingRule>(
            """
            SELECT *
            FROM PricingRules
            WHERE (DayType = @DayType OR DayType = 'All')
              AND SeatType = @SeatType
            ORDER BY
                CASE WHEN DayType = @DayType THEN 0 ELSE 1 END,
                Multiplier DESC;
            """,
            new
            {
                DayType = dayType.Trim(),
                SeatType = (int)parsedSeatType
            }).AsList();
    }

    public IEnumerable<PricingRule> GetBySeatAndScreenType(
        SeatType seatType,
        ScreenType screenType,
        string dayType)
    {
        if (!Enum.IsDefined(seatType) ||
            !Enum.IsDefined(screenType) ||
            string.IsNullOrWhiteSpace(dayType))
        {
            return [];
        }

        using var connection = ConnectionFactory.CreateConnection();

        return connection.Query<PricingRule>(
            """
            SELECT *
            FROM PricingRules
            WHERE SeatType = @SeatType
              AND ScreenType = @ScreenType
              AND (DayType = @DayType OR DayType = 'All')
            ORDER BY
                CASE WHEN DayType = @DayType THEN 0 ELSE 1 END,
                Multiplier DESC;
            """,
            new
            {
                SeatType = (int)seatType,
                ScreenType = (int)screenType,
                DayType = dayType.Trim()
            }).AsList();
    }

    private static void ValidateRule(PricingRule entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Name))
            throw new ArgumentException("Tên quy tắc giá không được để trống.");

        if (string.IsNullOrWhiteSpace(entity.DayType))
            throw new ArgumentException("Loại ngày không được để trống.");

        if (!Enum.IsDefined(entity.SeatType))
            throw new ArgumentException("Loại ghế không hợp lệ.");

        if (!Enum.IsDefined(entity.ScreenType))
            throw new ArgumentException("Loại phòng chiếu không hợp lệ.");

        if (entity.Multiplier <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(entity.Multiplier),
                "Hệ số giá phải lớn hơn 0.");

        if (entity.ExtraFee < 0)
            throw new ArgumentOutOfRangeException(
                nameof(entity.ExtraFee),
                "Phụ phí không được âm.");

        if (!TimeSpan.TryParse(entity.TimeFrom, out _) ||
            !TimeSpan.TryParse(entity.TimeTo, out _))
        {
            throw new ArgumentException("Khung giờ áp dụng không hợp lệ.");
        }
    }
}
