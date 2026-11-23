
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

        const string sql = """
            INSERT INTO PricingRules
                (Name, SeatType, ScreenType, DayType, TimeFrom, TimeTo, Multiplier, ExtraFee)
            VALUES
                (@Name, @SeatType, @ScreenType, @DayType, @TimeFrom, @TimeTo, @Multiplier, @ExtraFee);

            SELECT last_insert_rowid();
            """;

        using var connection = ConnectionFactory.CreateConnection();

        return connection.ExecuteScalar<int>(sql, new
        {
            entity.Name,
            SeatType = (int)entity.SeatType,
            ScreenType = (int)entity.ScreenType,
            entity.DayType,
            entity.TimeFrom,
            entity.TimeTo,
            entity.Multiplier,
            entity.ExtraFee
        });
    }

    public override bool Update(PricingRule entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        const string sql = """
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
            """;

        using var connection = ConnectionFactory.CreateConnection();

        return connection.Execute(sql, new
        {
            entity.Id,
            entity.Name,
            SeatType = (int)entity.SeatType,
            ScreenType = (int)entity.ScreenType,
            entity.DayType,
            entity.TimeFrom,
            entity.TimeTo,
            entity.Multiplier,
            entity.ExtraFee
        }) > 0;
    }

    public override bool Delete(int id)
    {
        using var connection = ConnectionFactory.CreateConnection();

        return connection.Execute(
            "DELETE FROM PricingRules WHERE Id = @Id;",
            new { Id = id }) > 0;
    }

    public IEnumerable<PricingRule> GetByDayAndSeatType(
        string dayType,
        string seatType)
    {
        if (!Enum.TryParse<SeatType>(
                seatType,
                true,
                out var parsedSeatType))
        {
            return Enumerable.Empty<PricingRule>();
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
                DayType = dayType,
                SeatType = (int)parsedSeatType
            }).AsList();
    }

    public IEnumerable<PricingRule> GetBySeatAndScreenType(
        SeatType seatType,
        ScreenType screenType,
        string dayType)
    {
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
                DayType = dayType
            }).AsList();
    }
}
