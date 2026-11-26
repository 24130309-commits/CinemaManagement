
using Dapper;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.DAL.Database;

namespace CinemaManagement.DAL.Repositories;

public sealed class SeatHoldRepository : ISeatHoldRepository
{
    private readonly SqliteConnectionFactory _factory;

    public SeatHoldRepository(SqliteConnectionFactory factory)
    {
        _factory = factory
            ?? throw new ArgumentNullException(nameof(factory));
    }

    public List<SeatHold> GetActiveHoldsByShowtime(int showtimeId)
    {
        if (showtimeId <= 0)
            return [];

        using var connection = _factory.CreateConnection();

        return connection.Query<SeatHold>(
            """
            SELECT
                Id,
                ShowtimeId,
                SeatId,
                CustomerId,
                SessionId,
                HoldTime,
                ExpiresAt,
                IsReleased
            FROM SeatHolds
            WHERE ShowtimeId = @ShowtimeId
              AND IsReleased = 0
              AND julianday(ExpiresAt) > julianday(@Now)
            ORDER BY ExpiresAt;
            """,
            new
            {
                ShowtimeId = showtimeId,
                Now = DateTime.Now.ToString("O")
            }).AsList();
    }

    public SeatHold? GetById(int id)
    {
        if (id <= 0)
            return null;

        using var connection = _factory.CreateConnection();

        return connection.QuerySingleOrDefault<SeatHold>(
            """
            SELECT
                Id,
                ShowtimeId,
                SeatId,
                CustomerId,
                SessionId,
                HoldTime,
                ExpiresAt,
                IsReleased
            FROM SeatHolds
            WHERE Id = @Id;
            """,
            new { Id = id });
    }

    public void Add(SeatHold seatHold)
    {
        ArgumentNullException.ThrowIfNull(seatHold);

        if (!TryAcquireOrExtend(seatHold))
        {
            throw new InvalidOperationException(
                "Ghế không khả dụng hoặc đang được phiên khác giữ chỗ.");
        }
    }

    public bool TryAcquireOrExtend(SeatHold seatHold)
    {
        ArgumentNullException.ThrowIfNull(seatHold);
        ValidateSeatHold(seatHold);

        using var connection = _factory.CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            var now = DateTime.Now;

            var seatAvailable = connection.ExecuteScalar<bool>(
                """
                SELECT EXISTS
                (
                    SELECT 1
                    FROM Showtimes st
                    INNER JOIN Rooms r ON r.Id = st.RoomId
                    INNER JOIN Seats s ON s.RoomId = st.RoomId
                    WHERE st.Id = @ShowtimeId
                      AND st.IsActive = 1
                      AND r.IsActive = 1
                      AND julianday(st.StartTime) > julianday(@Now)
                      AND s.Id = @SeatId
                      AND s.SeatType <> @AisleSeatType
                );
                """,
                new
                {
                    seatHold.ShowtimeId,
                    seatHold.SeatId,
                    Now = now.ToString("O"),
                    AisleSeatType = (int)SeatType.Aisle
                },
                transaction);

            if (!seatAvailable)
            {
                transaction.Rollback();
                return false;
            }

            var alreadySold = connection.ExecuteScalar<bool>(
                """
                SELECT EXISTS
                (
                    SELECT 1
                    FROM Tickets t
                    INNER JOIN Bookings b ON b.Id = t.BookingId
                    WHERE t.ShowtimeId = @ShowtimeId
                      AND t.SeatId = @SeatId
                      AND t.Status = 'Active'
                      AND b.Status = @PaidStatus
                );
                """,
                new
                {
                    seatHold.ShowtimeId,
                    seatHold.SeatId,
                    PaidStatus = (int)BookingStatus.Paid
                },
                transaction);

            if (alreadySold)
            {
                transaction.Rollback();
                return false;
            }

            var affected = connection.Execute(
                """
                INSERT INTO SeatHolds
                (
                    ShowtimeId,
                    SeatId,
                    CustomerId,
                    SessionId,
                    HoldTime,
                    ExpiresAt,
                    IsReleased
                )
                VALUES
                (
                    @ShowtimeId,
                    @SeatId,
                    @CustomerId,
                    @SessionId,
                    @HoldTime,
                    @ExpiresAt,
                    0
                )
                ON CONFLICT(ShowtimeId, SeatId)
                DO UPDATE SET
                    CustomerId = excluded.CustomerId,
                    SessionId = excluded.SessionId,
                    HoldTime = excluded.HoldTime,
                    ExpiresAt = excluded.ExpiresAt,
                    IsReleased = 0
                WHERE SeatHolds.IsReleased = 1
                   OR julianday(SeatHolds.ExpiresAt) <= julianday(@Now)
                   OR (
                       SeatHolds.CustomerId = excluded.CustomerId
                       AND SeatHolds.SessionId = excluded.SessionId
                   );
                """,
                new
                {
                    seatHold.ShowtimeId,
                    seatHold.SeatId,
                    seatHold.CustomerId,
                    SessionId = seatHold.SessionId.Trim(),
                    HoldTime = now.ToString("O"),
                    ExpiresAt = seatHold.ExpiresAt.ToString("O"),
                    Now = now.ToString("O")
                },
                transaction);

            if (affected == 0)
            {
                transaction.Rollback();
                return false;
            }

            seatHold.Id = connection.ExecuteScalar<int>(
                """
                SELECT Id
                FROM SeatHolds
                WHERE ShowtimeId = @ShowtimeId
                  AND SeatId = @SeatId;
                """,
                new
                {
                    seatHold.ShowtimeId,
                    seatHold.SeatId
                },
                transaction);

            transaction.Commit();
            return true;
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

    public void Update(SeatHold seatHold)
    {
        ArgumentNullException.ThrowIfNull(seatHold);

        if (seatHold.Id <= 0 ||
            seatHold.CustomerId <= 0 ||
            string.IsNullOrWhiteSpace(seatHold.SessionId))
        {
            throw new ArgumentException("Thông tin giữ ghế không hợp lệ.");
        }

        if (!seatHold.IsReleased && seatHold.ExpiresAt <= DateTime.Now)
            throw new ArgumentException("Thời gian giữ ghế phải nằm trong tương lai.");

        using var connection = _factory.CreateConnection();

        var affected = connection.Execute(
            """
            UPDATE SeatHolds
            SET HoldTime = @HoldTime,
                ExpiresAt = @ExpiresAt,
                IsReleased = @IsReleased
            WHERE Id = @Id
              AND CustomerId = @CustomerId
              AND SessionId = @SessionId
              AND IsReleased = 0;
            """,
            new
            {
                seatHold.Id,
                seatHold.CustomerId,
                SessionId = seatHold.SessionId.Trim(),
                HoldTime = seatHold.HoldTime.ToString("O"),
                ExpiresAt = seatHold.ExpiresAt.ToString("O"),
                IsReleased = seatHold.IsReleased ? 1 : 0
            });

        if (affected == 0)
        {
            throw new InvalidOperationException(
                "Không tìm thấy lượt giữ ghế hoặc bạn không sở hữu lượt giữ ghế này.");
        }
    }

    public bool ReleaseIfOwned(int holdId, string sessionId)
    {
        if (holdId <= 0 || string.IsNullOrWhiteSpace(sessionId))
            return false;

        using var connection = _factory.CreateConnection();

        var affected = connection.Execute(
            """
            UPDATE SeatHolds
            SET IsReleased = 1
            WHERE Id = @HoldId
              AND SessionId = @SessionId
              AND IsReleased = 0;
            """,
            new
            {
                HoldId = holdId,
                SessionId = sessionId.Trim()
            });

        return affected > 0;
    }

    public void ReleaseExpiredHolds()
    {
        using var connection = _factory.CreateConnection();

        connection.Execute(
            """
            UPDATE SeatHolds
            SET IsReleased = 1
            WHERE IsReleased = 0
              AND julianday(ExpiresAt) <= julianday(@Now);
            """,
            new
            {
                Now = DateTime.Now.ToString("O")
            });
    }

    private static void ValidateSeatHold(SeatHold seatHold)
    {
        if (seatHold.ShowtimeId <= 0 ||
            seatHold.SeatId <= 0 ||
            seatHold.CustomerId <= 0 ||
            string.IsNullOrWhiteSpace(seatHold.SessionId))
        {
            throw new ArgumentException(
                "Thông tin giữ ghế không hợp lệ.",
                nameof(seatHold));
        }

        if (seatHold.ExpiresAt <= DateTime.Now)
        {
            throw new ArgumentException(
                "Thời gian giữ ghế phải nằm trong tương lai.",
                nameof(seatHold));
        }
    }
}
