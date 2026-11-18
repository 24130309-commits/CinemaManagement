using Dapper;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.DAL.Database;

namespace CinemaManagement.DAL.Repositories;

public sealed class GenreRepository
    : BaseRepository<Genre>, IGenreRepository
{
    public GenreRepository(SqliteConnectionFactory factory)
        : base(factory, "Genres")
    {
    }

    public override int Insert(Genre entity)
    {
        using var connection = ConnectionFactory.CreateConnection();

        return connection.ExecuteScalar<int>(
            "INSERT INTO Genres(Name) VALUES(@Name); SELECT last_insert_rowid();",
            new { Name = entity.Name.Trim() });
    }

    public override bool Update(Genre entity)
    {
        using var connection = ConnectionFactory.CreateConnection();

        return connection.Execute(
            "UPDATE Genres SET Name=@Name WHERE Id=@Id",
            entity) > 0;
    }

    public override bool Delete(int id)
    {
        using var connection = ConnectionFactory.CreateConnection();

        return connection.Execute(
            "DELETE FROM Genres WHERE Id=@id",
            new { id }) > 0;
    }

    public bool IsNameExists(string name, int? excludeId = null)
    {
        using var connection = ConnectionFactory.CreateConnection();

        return connection.ExecuteScalar<bool>(
            """
            SELECT EXISTS(
                SELECT 1
                FROM Genres
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

    public bool IsInUse(int genreId)
    {
        using var connection = ConnectionFactory.CreateConnection();

        return connection.ExecuteScalar<bool>(
            """
            SELECT EXISTS(
                SELECT 1
                FROM MovieGenres
                WHERE GenreId=@genreId
            )
            """,
            new { genreId });
    }
}