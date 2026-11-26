
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
        if (roomId <= 0)
            throw new ArgumentException("Mã phòng không hợp lệ.", nameof(roomId));

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

        ValidateSeats(seatList);

        if (seatList.Select(x => x.RoomId).Distinct().Count() != 1)
            throw new ArgumentException("Danh sách ghế phải thuộc cùng một phòng.");

        var roomId = seatList[0].RoomId;

        using var connection = _connectionFactory.CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            var roomExists = connection.ExecuteScalar<bool>(
                """
                SELECT EXISTS(
                    SELECT 1
                    FROM Rooms
                    WHERE Id = @RoomId
                );
                """,
                new { RoomId = roomId },
                transaction);

            if (!roomExists)
                throw new InvalidOperationException("Phòng chiếu không tồn tại.");

            var affectedRows = InsertSeats(
                connection,
                transaction,
                seatList);

            if (affectedRows != seatList.Count)
                throw new InvalidOperationException("Không thể tạo đầy đủ danh sách ghế.");

            transaction.Commit();

            return affectedRows;
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

    public int ReplaceByRoom(
        int roomId,
        IEnumerable<Seat> seats,
        int totalRows,
        int totalCols)
    {
        ArgumentNullException.ThrowIfNull(seats);

        if (roomId <= 0)
            throw new ArgumentException("Mã phòng không hợp lệ.", nameof(roomId));

        if (totalRows <= 0 || totalRows > 26 ||
            totalCols <= 0 || totalCols > 40)
        {
            throw new ArgumentException("Kích thước sơ đồ ghế không hợp lệ.");
        }

        var seatList = seats.ToList();

        if (seatList.Count != totalRows * totalCols)
            throw new ArgumentException("Số lượng ghế không khớp với kích thước sơ đồ.");

        if (seatList.Any(seat =>
                seat.RoomId != roomId ||
                string.IsNullOrWhiteSpace(seat.RowLabel) ||
                seat.RowLabel.Trim().Length > 10 ||
                seat.ColNumber <= 0 ||
                seat.ColNumber > totalCols ||
                !Enum.IsDefined(typeof(SeatType), seat.SeatType)))
        {
            throw new ArgumentException("Danh sách ghế chứa dữ liệu không hợp lệ.");
        }

        var duplicateSeat = seatList
            .GroupBy(seat => new
            {
                Row = seat.RowLabel.Trim().ToUpperInvariant(),
                seat.ColNumber
            })
            .Any(group => group.Count() > 1);

        if (duplicateSeat)
            throw new ArgumentException("Sơ đồ ghế có vị trí bị trùng.");

        var expectedRows = Enumerable
            .Range(0, totalRows)
            .Select(i => ((char)('A' + i)).ToString())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!seatList.Select(x => x.RowLabel.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
                .SetEquals(expectedRows))
        {
            throw new ArgumentException("Nhãn hàng ghế không khớp kích thước sơ đồ.");
        }

        foreach (var row in expectedRows)
        {
            var columns = seatList
                .Where(x => string.Equals(
                    x.RowLabel.Trim(),
                    row,
                    StringComparison.OrdinalIgnoreCase))
                .Select(x => x.ColNumber)
                .OrderBy(x => x)
                .ToArray();

            if (!columns.SequenceEqual(Enumerable.Range(1, totalCols)))
                throw new ArgumentException("Sơ đồ ghế có vị trí bị thiếu.");
        }

        using var connection = _connectionFactory.CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            var roomExists = connection.ExecuteScalar<bool>(
                """
                SELECT EXISTS(
                    SELECT 1
                    FROM Rooms
                    WHERE Id = @RoomId
                );
                """,
                new { RoomId = roomId },
                transaction);

            if (!roomExists)
                throw new InvalidOperationException("Phòng chiếu không tồn tại.");

            var hasTickets = connection.ExecuteScalar<bool>(
                """
                SELECT EXISTS(
                    SELECT 1
                    FROM Tickets t
                    INNER JOIN Seats s ON s.Id = t.SeatId
                    WHERE s.RoomId = @RoomId
                );
                """,
                new { RoomId = roomId },
                transaction);

            if (hasTickets)
                throw new InvalidOperationException(
                    "Không thể thay sơ đồ ghế vì phòng đã có vé trong lịch sử.");

            var hasActiveHolds = connection.ExecuteScalar<bool>(
                """
                SELECT EXISTS(
                    SELECT 1
                    FROM SeatHolds h
                    INNER JOIN Seats s ON s.Id = h.SeatId
                    WHERE s.RoomId = @RoomId
                      AND h.IsReleased = 0
                      AND julianday(h.ExpiresAt) > julianday(@Now)
                );
                """,
                new
                {
                    RoomId = roomId,
                    Now = DateTime.Now.ToString("O")
                },
                transaction);

            if (hasActiveHolds)
                throw new InvalidOperationException(
                    "Không thể thay sơ đồ ghế khi vẫn còn ghế đang được giữ.");

            connection.Execute(
                "DELETE FROM Seats WHERE RoomId = @RoomId;",
                new { RoomId = roomId },
                transaction);

            var affectedRows = InsertSeats(
                connection,
                transaction,
                seatList);

            if (affectedRows != seatList.Count)
                throw new InvalidOperationException("Không thể tạo đầy đủ sơ đồ ghế.");

            var updatedRoom = connection.Execute(
                """
                UPDATE Rooms
                SET TotalRows = @TotalRows,
                    TotalCols = @TotalCols
                WHERE Id = @RoomId;
                """,
                new
                {
                    RoomId = roomId,
                    TotalRows = totalRows,
                    TotalCols = totalCols
                },
                transaction);

            if (updatedRoom != 1)
                throw new InvalidOperationException(
                    "Không thể cập nhật kích thước phòng chiếu.");

            transaction.Commit();

            return affectedRows;
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

    public bool DeleteByRoom(int roomId)
    {
        if (roomId <= 0)
            return false;

        using var connection = _connectionFactory.CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            var hasTickets = connection.ExecuteScalar<bool>(
                """
                SELECT EXISTS(
                    SELECT 1
                    FROM Tickets t
                    INNER JOIN Seats s ON s.Id = t.SeatId
                    WHERE s.RoomId = @RoomId
                );
                """,
                new { RoomId = roomId },
                transaction);

            if (hasTickets)
                throw new InvalidOperationException(
                    "Không thể xóa ghế của phòng đã có vé trong lịch sử.");

            var hasHolds = connection.ExecuteScalar<bool>(
                """
                SELECT EXISTS(
                    SELECT 1
                    FROM SeatHolds h
                    INNER JOIN Seats s ON s.Id = h.SeatId
                    WHERE s.RoomId = @RoomId
                      AND h.IsReleased = 0
                );
                """,
                new { RoomId = roomId },
                transaction);

            if (hasHolds)
                throw new InvalidOperationException(
                    "Không thể xóa ghế đang được giữ.");

            var affected = connection.Execute(
                "DELETE FROM Seats WHERE RoomId = @RoomId;",
                new { RoomId = roomId },
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

    public bool UpdateType(int seatId, SeatType type)
    {
        if (seatId <= 0 || !Enum.IsDefined(typeof(SeatType), type))
            return false;

        using var connection = _connectionFactory.CreateConnection();

        var hasTickets = connection.ExecuteScalar<bool>(
            """
            SELECT EXISTS(
                SELECT 1
                FROM Tickets
                WHERE SeatId = @SeatId
            );
            """,
            new { SeatId = seatId });

        if (hasTickets)
            throw new InvalidOperationException(
                "Không thể đổi loại ghế đã có vé trong lịch sử.");

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

    public int UpdateTypes(IEnumerable<Seat> seats)
    {
        ArgumentNullException.ThrowIfNull(seats);

        var seatList = seats.ToList();

        if (seatList.Count == 0)
            return 0;

        if (seatList.Any(seat =>
                seat.Id <= 0 ||
                !Enum.IsDefined(typeof(SeatType), seat.SeatType)))
        {
            throw new ArgumentException(
                "Danh sách loại ghế chứa dữ liệu không hợp lệ.",
                nameof(seats));
        }

        if (seatList.Select(seat => seat.Id).Distinct().Count() != seatList.Count)
            throw new ArgumentException(
                "Danh sách chứa mã ghế bị trùng.",
                nameof(seats));

        using var connection = _connectionFactory.CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            var affectedRows = 0;

            foreach (var seat in seatList)
            {
                var hasTickets = connection.ExecuteScalar<bool>(
                    """
                    SELECT EXISTS(
                        SELECT 1
                        FROM Tickets
                        WHERE SeatId = @SeatId
                    );
                    """,
                    new { SeatId = seat.Id },
                    transaction);

                if (hasTickets)
                    throw new InvalidOperationException(
                        $"Không thể đổi loại ghế ID {seat.Id} vì ghế đã có vé.");

                var affected = connection.Execute(
                    """
                    UPDATE Seats
                    SET SeatType = @SeatType
                    WHERE Id = @SeatId;
                    """,
                    new
                    {
                        SeatId = seat.Id,
                        SeatType = (int)seat.SeatType
                    },
                    transaction);

                if (affected != 1)
                    throw new InvalidOperationException(
                        $"Không tìm thấy ghế có mã ID {seat.Id}.");

                affectedRows += affected;
            }

            transaction.Commit();

            return affectedRows;
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

    private static void ValidateSeats(IEnumerable<Seat> seats)
    {
        var seatList = seats.ToList();

        if (seatList.Any(seat =>
                seat.RoomId <= 0 ||
                string.IsNullOrWhiteSpace(seat.RowLabel) ||
                seat.RowLabel.Trim().Length > 10 ||
                seat.ColNumber <= 0 ||
                !Enum.IsDefined(typeof(SeatType), seat.SeatType)))
        {
            throw new ArgumentException("Danh sách ghế chứa dữ liệu không hợp lệ.");
        }

        var duplicate = seatList
            .GroupBy(seat => new
            {
                seat.RoomId,
                Row = seat.RowLabel.Trim().ToUpperInvariant(),
                seat.ColNumber
            })
            .Any(group => group.Count() > 1);

        if (duplicate)
            throw new ArgumentException("Danh sách ghế có vị trí bị trùng.");
    }

    private static int InsertSeats(
        System.Data.IDbConnection connection,
        System.Data.IDbTransaction transaction,
        IEnumerable<Seat> seats)
    {
        var affectedRows = 0;

        foreach (var seat in seats)
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
                    RowLabel = seat.RowLabel.Trim().ToUpperInvariant(),
                    seat.ColNumber,
                    SeatType = (int)seat.SeatType
                },
                transaction);
        }

        return affectedRows;
    }
}
