
using Dapper;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.DAL.Database;

namespace CinemaManagement.DAL.Repositories;

public sealed class SeatRepository : ISeatRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public SeatRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory
            ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public IEnumerable<Seat> GetByRoom(int roomId)
    {
        using var connection = _connectionFactory.CreateConnection();

        return connection.Query<Seat>(
            """
            SELECT Id, RoomId, RowLabel, ColNumber, SeatType
            FROM Seats
            WHERE RoomId = @RoomId
            ORDER BY RowLabel, ColNumber;
            """,
            new { RoomId = roomId }).AsList();
    }

    public int InsertBatch(IEnumerable<Seat> seats)
    {
        ArgumentNullException.ThrowIfNull(seats);

        var seatList = seats.ToList();

        if (seatList.Count == 0)
            return 0;

        using var connection = _connectionFactory.CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            var affectedRows = 0;

            foreach (var seat in seatList)
            {
                affectedRows += connection.Execute(
                    """
                    INSERT INTO Seats
                    (RoomId, RowLabel, ColNumber, SeatType)
                    VALUES
                    (@RoomId, @RowLabel, @ColNumber, @SeatType);
                    """,
                    new
                    {
                        seat.RoomId,
                        seat.RowLabel,
                        seat.ColNumber,
                        SeatType = (int)seat.SeatType
                    },
                    transaction);
            }

            transaction.Commit();
            return affectedRows;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public int ReplaceByRoom(int roomId, IEnumerable<Seat> seats)
    {
        ArgumentNullException.ThrowIfNull(seats);

        if (roomId <= 0)
            throw new ArgumentException("Mã phòng không hợp lệ.", nameof(roomId));

        var seatList = seats.ToList();

        if (seatList.Any(seat => seat.RoomId != roomId))
            throw new ArgumentException("Danh sách ghế không thuộc phòng đã chọn.");

        using var connection = _connectionFactory.CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            connection.Execute(
                "DELETE FROM Seats WHERE RoomId = @RoomId;",
                new { RoomId = roomId },
                transaction);

            var affectedRows = 0;

            foreach (var seat in seatList)
            {
                affectedRows += connection.Execute(
                    """
                    INSERT INTO Seats
                    (RoomId, RowLabel, ColNumber, SeatType)
                    VALUES
                    (@RoomId, @RowLabel, @ColNumber, @SeatType);
                    """,
                    new
                    {
                        seat.RoomId,
                        seat.RowLabel,
                        seat.ColNumber,
                        SeatType = (int)seat.SeatType
                    },
                    transaction);
            }

            transaction.Commit();
            return affectedRows;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public bool DeleteByRoom(int roomId)
    {
        using var connection = _connectionFactory.CreateConnection();

        return connection.Execute(
            "DELETE FROM Seats WHERE RoomId = @RoomId;",
            new { RoomId = roomId }) > 0;
    }

    public bool UpdateType(int seatId, SeatType type)
    {
        using var connection = _connectionFactory.CreateConnection();

        return connection.Execute(
            """
            UPDATE Seats
            SET SeatType = @SeatType
            WHERE Id = @SeatId;
            """,
            new
            {
                SeatId = seatId,
                SeatType = (int)type
            }) > 0;
    }
}
