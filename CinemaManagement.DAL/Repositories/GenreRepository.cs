
using Dapper;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.DAL.Database;

namespace CinemaManagement.DAL.Repositories;

public sealed class GenreRepository : BaseRepository<Genre>, IGenreRepository
{
    public GenreRepository(SqliteConnectionFactory factory)
        : base(factory, "Genres")
    {
    }

    public override int Insert(Genre entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ValidateName(entity.Name);

        var name = entity.Name.Trim();

        if (IsNameExists(name))
            throw new InvalidOperationException(
                "Tên thể loại đã tồn tại.");

        using var connection = ConnectionFactory.CreateConnection();

        return connection.ExecuteScalar<int>(
            """
            INSERT INTO Genres(Name)
            VALUES(@Name);

            SELECT last_insert_rowid();
            """,
            new { Name = name });
    }

    public override bool Update(Genre entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ValidateName(entity.Name);

        if (entity.Id <= 0)
            throw new ArgumentException("Mã thể loại không hợp lệ.");

        var name = entity.Name.Trim();

        if (IsNameExists(name, entity.Id))
            throw new InvalidOperationException(
                "Tên thể loại đã tồn tại.");

        using var connection = ConnectionFactory.CreateConnection();

        return connection.Execute(
            """
            UPDATE Genres
            SET Name = @Name
            WHERE Id = @Id;
            """,
            new
            {
                entity.Id,
                Name = name
            }) > 0;
    }

    public override bool Delete(int id)
    {
        if (id <= 0)
            return false;

        if (IsInUse(id))
            throw new InvalidOperationException(
                "Không thể xóa thể loại đang được sử dụng bởi phim.");

        using var connection = ConnectionFactory.CreateConnection();

        return connection.Execute(
            "DELETE FROM Genres WHERE Id = @Id;",
            new { Id = id }) > 0;
    }

    public bool IsNameExists(string name, int? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        using var connection = ConnectionFactory.CreateConnection();

        return connection.ExecuteScalar<bool>(
            """
            SELECT EXISTS
            (
                SELECT 1
                FROM Genres
                WHERE Name = @Name COLLATE NOCASE
                  AND (@ExcludeId IS NULL OR Id <> @ExcludeId)
            );
            """,
            new
            {
                Name = name.Trim(),
                ExcludeId = excludeId
            });
    }

    public bool IsInUse(int genreId)
    {
        if (genreId <= 0)
            return false;

        using var connection = ConnectionFactory.CreateConnection();

        return connection.ExecuteScalar<bool>(
            """
            SELECT EXISTS
            (
                SELECT 1
                FROM MovieGenres
                WHERE GenreId = @GenreId
            );
            """,
            new { GenreId = genreId });
    }

    private static void ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Tên thể loại không được để trống.");

        if (name.Trim().Length > 100)
            throw new ArgumentException(
                "Tên thể loại không được vượt quá 100 ký tự.");
    }
}
