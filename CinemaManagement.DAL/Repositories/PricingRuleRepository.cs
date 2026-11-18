
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
        const string sql = """
            INSERT INTO PricingRules
                (SeatType, ScreenType, DayType, TimeFrom, TimeTo, Multiplier)
            VALUES
                (@SeatType, @ScreenType, @DayType, @TimeFrom, @TimeTo, @Multiplier);

            SELECT last_insert_rowid();
            """;

        using var connection = ConnectionFactory.CreateConnection();

        return connection.ExecuteScalar<int>(sql, new
        {
            SeatType = (int)entity.SeatType,
            ScreenType = (int)entity.ScreenType,
            entity.DayType,
            entity.TimeFrom,
            entity.TimeTo,
            entity.Multiplier
        });
    }

    public override bool Update(PricingRule entity)
    {
        const string sql = """
            UPDATE PricingRules
            SET SeatType = @SeatType,
                ScreenType = @ScreenType,
                DayType = @DayType,
                TimeFrom = @TimeFrom,
                TimeTo = @TimeTo,
                Multiplier = @Multiplier
            WHERE Id = @Id;
            """;

        using var connection = ConnectionFactory.CreateConnection();

        return connection.Execute(sql, new
        {
            entity.Id,
            SeatType = (int)entity.SeatType,
            ScreenType = (int)entity.ScreenType,
            entity.DayType,
            entity.TimeFrom,
            entity.TimeTo,
            entity.Multiplier
        }) > 0;
    }

    public override bool Delete(int id)
    {
        using var connection = ConnectionFactory.CreateConnection();

        return connection.Execute(
            "DELETE FROM PricingRules WHERE Id = @id;",
            new { id }) > 0;
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
            WHERE (DayType = @dayType OR DayType = 'All')
              AND SeatType = @seatType
            ORDER BY
                CASE WHEN DayType = @dayType THEN 0 ELSE 1 END,
                Multiplier DESC;
            """,
            new
            {
                dayType,
                seatType = (int)parsedSeatType
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
            WHERE SeatType = @seatType
              AND ScreenType = @screenType
              AND (DayType = @dayType OR DayType = 'All')
            ORDER BY
                CASE WHEN DayType = @dayType THEN 0 ELSE 1 END,
                Multiplier DESC;
            """,
            new
            {
                seatType = (int)seatType,
                screenType = (int)screenType,
                dayType
            }).AsList();
    }
}
