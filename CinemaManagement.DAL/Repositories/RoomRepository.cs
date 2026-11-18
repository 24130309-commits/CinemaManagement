using Dapper;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.DAL.Database;

namespace CinemaManagement.DAL.Repositories;

public sealed class RoomRepository
    : BaseRepository<Room>, IRoomRepository
{
    public RoomRepository(SqliteConnectionFactory factory)
        : base(factory, "Rooms")
    {
    }

    public override IEnumerable<Room> GetAll()
    {
        using var connection = ConnectionFactory.CreateConnection();

        return connection.Query<Room>(
            "SELECT * FROM Rooms ORDER BY Name").AsList();
    }

    public override Room? GetById(int id)
    {
        using var connection = ConnectionFactory.CreateConnection();

        return connection.QuerySingleOrDefault<Room>(
            "SELECT * FROM Rooms WHERE Id=@id",
            new { id });
    }

    public IEnumerable<Room> GetActive()
    {
        using var connection = ConnectionFactory.CreateConnection();

        return connection.Query<Room>(
            "SELECT * FROM Rooms WHERE IsActive=1 ORDER BY Name").AsList();
    }

    public bool IsRoomNameExists(
        string name,
        int? excludeId = null)
    {
        using var connection = ConnectionFactory.CreateConnection();

        return connection.ExecuteScalar<bool>(
            """
            SELECT EXISTS(
                SELECT 1
                FROM Rooms
                WHERE Name=@name COLLATE NOCASE
                AND (@excludeId IS NULL OR Id<>@excludeId)
            )
            """,
            new
            {
                name = name.Trim(),
                excludeId
            });
    }

    public override int Insert(Room room)
    {
        using var connection = ConnectionFactory.CreateConnection();

        return connection.ExecuteScalar<int>(
            """
            INSERT INTO Rooms(
                Name,
                TotalRows,
                TotalCols,
                ScreenType,
                IsActive
            )
            VALUES(
                @Name,
                @TotalRows,
                @TotalCols,
                @ScreenType,
                @IsActive
            );

            SELECT last_insert_rowid();
            """,
            new
            {
                room.Name,
                room.TotalRows,
                room.TotalCols,
                ScreenType = (int)room.ScreenType,
                IsActive = room.IsActive ? 1 : 0
            });
    }

    public int Add(Room room) => Insert(room);

    public override bool Update(Room room)
    {
        using var connection = ConnectionFactory.CreateConnection();

        return connection.Execute(
            """
            UPDATE Rooms
            SET Name=@Name,
                TotalRows=@TotalRows,
                TotalCols=@TotalCols,
                ScreenType=@ScreenType,
                IsActive=@IsActive
            WHERE Id=@Id;
            """,
            new
            {
                room.Id,
                room.Name,
                room.TotalRows,
                room.TotalCols,
                ScreenType = (int)room.ScreenType,
                IsActive = room.IsActive ? 1 : 0
            }) > 0;
    }

    public override bool Delete(int id)
    {
        using var connection = ConnectionFactory.CreateConnection();

        return connection.Execute(
            "DELETE FROM Rooms WHERE Id=@id",
            new { id }) > 0;
    }
}