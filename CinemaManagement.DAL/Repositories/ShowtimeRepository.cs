
using Dapper;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.DAL.Database;

namespace CinemaManagement.DAL.Repositories;

public sealed class ShowtimeRepository : BaseRepository<Showtime>, IShowtimeRepository
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
        if (id <= 0)
            return null;

        using var connection = ConnectionFactory.CreateConnection();

        return connection.QuerySingleOrDefault<Showtime>(
            SelectSql + " WHERE s.Id = @Id;",
            new { Id = id });
    }

    public override IEnumerable<Showtime> GetAll()
    {
        using var connection = ConnectionFactory.CreateConnection();

        return connection.Query<Showtime>(
            SelectSql + " ORDER BY s.StartTime DESC;").AsList();
    }

    public override int Insert(Showtime entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        ValidateShowtime(entity);

        using var connection = ConnectionFactory.CreateConnection();
        using var transaction = connection.BeginTransaction(deferred: false);

        try
        {
            ValidateReferences(connection, transaction, entity);

            if (HasConflict(
                    connection,
                    transaction,
                    entity.RoomId,
                    entity.StartTime,
                    entity.EndTime))
            {
                throw new InvalidOperationException(
                    "Phòng chiếu đã có suất chiếu trùng thời gian.");
            }

            var id = connection.ExecuteScalar<int>(
                """
                INSERT INTO Showtimes
                    (MovieId, RoomId, StartTime, EndTime, BasePrice, IsActive)
                VALUES
                    (@MovieId, @RoomId, @StartTime, @EndTime, @BasePrice, @IsActive);

                SELECT last_insert_rowid();
                """,
                GetParameters(entity),
                transaction);

            transaction.Commit();

            return id;
        }
        catch
        {
            try
            {
                transaction.Rollback();
            }
            catch
            {
            }

            throw;
        }
    }

    public override bool Update(Showtime entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity.Id <= 0)
            throw new ArgumentException("Mã suất chiếu không hợp lệ.");

        ValidateShowtime(entity);

        using var connection = ConnectionFactory.CreateConnection();
        using var transaction = connection.BeginTransaction(deferred: false);

        try
        {
            var existing = connection.QuerySingleOrDefault<Showtime>(
                """
                SELECT *
                FROM Showtimes
                WHERE Id = @Id;
                """,
                new { entity.Id },
                transaction);

            if (existing == null)
                return false;

            var hasTickets = connection.ExecuteScalar<bool>(
                """
                SELECT EXISTS(
                    SELECT 1
                    FROM Tickets
                    WHERE ShowtimeId = @ShowtimeId
                );
                """,
                new { ShowtimeId = entity.Id },
                transaction);

            if (hasTickets &&
                (existing.MovieId != entity.MovieId ||
                 existing.RoomId != entity.RoomId ||
                existing.StartTime != entity.StartTime ||
                existing.EndTime != entity.EndTime ||
                existing.BasePrice != entity.BasePrice))
            {
                throw new InvalidOperationException(
                    "Không thể thay đổi phim, phòng hoặc thời gian của suất chiếu đã có vé.");
            }

            ValidateReferences(connection, transaction, entity);

            if (HasConflict(
                    connection,
                    transaction,
                    entity.RoomId,
                    entity.StartTime,
                    entity.EndTime,
                    entity.Id))
            {
                throw new InvalidOperationException(
                    "Phòng chiếu đã có suất chiếu trùng thời gian.");
            }

            var affected = connection.Execute(
                """
                UPDATE Showtimes
                SET MovieId = @MovieId,
                    RoomId = @RoomId,
                    StartTime = @StartTime,
                    EndTime = @EndTime,
                    BasePrice = @BasePrice,
                    IsActive = @IsActive
                WHERE Id = @Id;
                """,
                GetParameters(entity),
                transaction);

            transaction.Commit();

            return affected > 0;
        }
        catch
        {
            try
            {
                transaction.Rollback();
            }
            catch
            {
            }

            throw;
        }
    }

    public override bool Delete(int id)
    {
        if (id <= 0)
            return false;

        using var connection = ConnectionFactory.CreateConnection();

        var hasTickets = connection.ExecuteScalar<bool>(
            """
            SELECT EXISTS(
                SELECT 1
                FROM Tickets
                WHERE ShowtimeId = @Id
            );
            """,
            new { Id = id });

        if (hasTickets)
            throw new InvalidOperationException(
                "Không thể xóa suất chiếu đã phát sinh vé.");

        var hasBookings = connection.ExecuteScalar<bool>(
            """
            SELECT EXISTS(
                SELECT 1
                FROM Bookings
                WHERE ShowtimeId = @Id
            );
            """,
            new { Id = id });

        if (hasBookings)
            throw new InvalidOperationException(
                "Không thể xóa suất chiếu đã có đơn đặt vé.");

        return connection.Execute(
            "DELETE FROM Showtimes WHERE Id = @Id;",
            new { Id = id }) > 0;
    }

    public IEnumerable<Showtime> GetByMovieId(int movieId)
    {
        if (movieId <= 0)
            return Enumerable.Empty<Showtime>();

        using var connection = ConnectionFactory.CreateConnection();

        return connection.Query<Showtime>(
            SelectSql + """
                WHERE s.MovieId = @MovieId
                ORDER BY s.StartTime;
                """,
            new { MovieId = movieId }).AsList();
    }

    public IEnumerable<Showtime> GetByDate(DateTime date)
    {
        using var connection = ConnectionFactory.CreateConnection();

        return connection.Query<Showtime>(
            SelectSql + """
                WHERE date(s.StartTime) = date(@Date)
                ORDER BY s.StartTime;
                """,
            new { Date = date.ToString("yyyy-MM-dd") }).AsList();
    }

    public bool HasConflict(
        int roomId,
        DateTime startTime,
        DateTime endTime,
        int? excludeShowtimeId = null)
    {
        if (roomId <= 0 || endTime <= startTime)
            return true;

        using var connection = ConnectionFactory.CreateConnection();

        return HasConflict(
            connection,
            null,
            roomId,
            startTime,
            endTime,
            excludeShowtimeId);
    }

    public bool HasTickets(int showtimeId)
    {
        if (showtimeId <= 0)
            return false;

        using var connection = ConnectionFactory.CreateConnection();

        return connection.ExecuteScalar<bool>(
            """
            SELECT EXISTS(
                SELECT 1
                FROM Tickets
                WHERE ShowtimeId = @ShowtimeId
            );
            """,
            new { ShowtimeId = showtimeId });
    }

    private static bool HasConflict(
        System.Data.IDbConnection connection,
        System.Data.IDbTransaction? transaction,
        int roomId,
        DateTime startTime,
        DateTime endTime,
        int? excludeShowtimeId = null)
    {
        return connection.ExecuteScalar<bool>(
            """
            SELECT EXISTS(
                SELECT 1
                FROM Showtimes
                WHERE RoomId = @RoomId
                  AND IsActive = 1
                  AND StartTime < @EndTime
                  AND EndTime > @StartTime
                  AND (@ExcludeId IS NULL OR Id <> @ExcludeId)
            );
            """,
            new
            {
                RoomId = roomId,
                StartTime = FormatDate(startTime),
                EndTime = FormatDate(endTime),
                ExcludeId = excludeShowtimeId
            },
            transaction);
    }

    private static void ValidateShowtime(Showtime entity)
    {
        if (entity.MovieId <= 0)
            throw new ArgumentException("Phim không hợp lệ.");

        if (entity.RoomId <= 0)
            throw new ArgumentException("Phòng chiếu không hợp lệ.");

        if (entity.EndTime <= entity.StartTime)
            throw new ArgumentException(
                "Thời gian kết thúc phải sau thời gian bắt đầu.");

        if (entity.BasePrice < 0)
            throw new ArgumentException("Giá vé không được âm.");
    }

    private static void ValidateReferences(
        System.Data.IDbConnection connection,
        System.Data.IDbTransaction transaction,
        Showtime entity)
    {
        var movieExists = connection.ExecuteScalar<bool>(
            """
            SELECT EXISTS(
                SELECT 1
                FROM Movies
                WHERE Id = @MovieId
            );
            """,
            new { entity.MovieId },
            transaction);

        if (!movieExists)
            throw new InvalidOperationException("Phim không tồn tại.");

        var roomAvailable = connection.ExecuteScalar<bool>(
            """
            SELECT EXISTS(
                SELECT 1
                FROM Rooms
                WHERE Id = @RoomId
                  AND (@RequireActive = 0 OR IsActive = 1)
            );
            """,
            new
            {
                entity.RoomId,
                RequireActive = entity.IsActive ? 1 : 0
            },
            transaction);

        if (!roomAvailable)
            throw new InvalidOperationException(
                entity.IsActive
                    ? "Phòng chiếu không tồn tại hoặc đang ngừng hoạt động."
                    : "Phòng chiếu không tồn tại.");
    }

    private static object GetParameters(Showtime entity)
    {
        return new
        {
            entity.Id,
            entity.MovieId,
            entity.RoomId,
            StartTime = FormatDate(entity.StartTime),
            EndTime = FormatDate(entity.EndTime),
            entity.BasePrice,
            IsActive = entity.IsActive ? 1 : 0
        };
    }

    private static string FormatDate(DateTime dateTime)
    {
        return dateTime.ToString("yyyy-MM-dd HH:mm:ss");
    }
}
