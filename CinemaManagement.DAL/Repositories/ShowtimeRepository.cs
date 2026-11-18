
using Dapper;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.DAL.Database;

namespace CinemaManagement.DAL.Repositories;

public sealed class ShowtimeRepository
    : BaseRepository<Showtime>, IShowtimeRepository
{
    private const string SelectSql = """
        SELECT
            s.Id,
            s.MovieId,
            s.RoomId,
            s.StartTime,
            s.EndTime,
            s.BasePrice,
            s.IsActive,
            m.Title AS MovieTitle,
            r.Name AS RoomName
        FROM Showtimes s
        INNER JOIN Movies m ON m.Id = s.MovieId
        INNER JOIN Rooms r ON r.Id = s.RoomId
        """;

    public ShowtimeRepository(SqliteConnectionFactory factory)
        : base(factory, "Showtimes")
    {
    }

    public override Showtime? GetById(int id)
    {
        using var connection = ConnectionFactory.CreateConnection();

        return connection.QuerySingleOrDefault<Showtime>(
            SelectSql + " WHERE s.Id = @id;",
            new { id });
    }

    public override IEnumerable<Showtime> GetAll()
    {
        using var connection = ConnectionFactory.CreateConnection();

        return connection.Query<Showtime>(
            SelectSql + " ORDER BY s.StartTime DESC;").AsList();
    }

    public override int Insert(Showtime entity)
    {
        const string sql = """
            INSERT INTO Showtimes
                (MovieId, RoomId, StartTime, EndTime, BasePrice, IsActive)
            VALUES
                (@MovieId, @RoomId, @StartTime, @EndTime, @BasePrice, @IsActive);

            SELECT last_insert_rowid();
            """;

        using var connection = ConnectionFactory.CreateConnection();

        return connection.ExecuteScalar<int>(sql, new
        {
            entity.MovieId,
            entity.RoomId,
            StartTime = entity.StartTime.ToString("yyyy-MM-dd HH:mm:ss"),
            EndTime = entity.EndTime.ToString("yyyy-MM-dd HH:mm:ss"),
            entity.BasePrice,
            IsActive = entity.IsActive ? 1 : 0
        });
    }

    public override bool Update(Showtime entity)
    {
        const string sql = """
            UPDATE Showtimes
            SET MovieId = @MovieId,
                RoomId = @RoomId,
                StartTime = @StartTime,
                EndTime = @EndTime,
                BasePrice = @BasePrice,
                IsActive = @IsActive
            WHERE Id = @Id;
            """;

        using var connection = ConnectionFactory.CreateConnection();

        return connection.Execute(sql, new
        {
            entity.Id,
            entity.MovieId,
            entity.RoomId,
            StartTime = entity.StartTime.ToString("yyyy-MM-dd HH:mm:ss"),
            EndTime = entity.EndTime.ToString("yyyy-MM-dd HH:mm:ss"),
            entity.BasePrice,
            IsActive = entity.IsActive ? 1 : 0
        }) > 0;
    }

    public override bool Delete(int id)
    {
        using var connection = ConnectionFactory.CreateConnection();

        return connection.Execute(
            "DELETE FROM Showtimes WHERE Id = @id;",
            new { id }) > 0;
    }

    public IEnumerable<Showtime> GetByMovieId(int movieId)
    {
        using var connection = ConnectionFactory.CreateConnection();

        return connection.Query<Showtime>(
            SelectSql + """
                 WHERE s.MovieId = @movieId
                 ORDER BY s.StartTime;
                """,
            new { movieId }).AsList();
    }

    public IEnumerable<Showtime> GetByDate(DateTime date)
    {
        using var connection = ConnectionFactory.CreateConnection();

        return connection.Query<Showtime>(
            SelectSql + """
                 WHERE date(s.StartTime) = date(@date)
                 ORDER BY s.StartTime;
                """,
            new { date = date.ToString("yyyy-MM-dd") }).AsList();
    }

    public bool HasConflict(
        int roomId,
        DateTime startTime,
        DateTime endTime,
        int? excludeShowtimeId = null)
    {
        using var connection = ConnectionFactory.CreateConnection();

        return connection.ExecuteScalar<int>(
            """
            SELECT COUNT(1)
            FROM Showtimes
            WHERE RoomId = @roomId
              AND IsActive = 1
              AND StartTime < @endTime
              AND EndTime > @startTime
              AND (@excludeId IS NULL OR Id <> @excludeId);
            """,
            new
            {
                roomId,
                startTime = startTime.ToString("yyyy-MM-dd HH:mm:ss"),
                endTime = endTime.ToString("yyyy-MM-dd HH:mm:ss"),
                excludeId = excludeShowtimeId
            }) > 0;
    }
}
