
using Dapper;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.DAL.Database;

namespace CinemaManagement.DAL.Repositories;

public class MovieRepository : BaseRepository<Movie>, IMovieRepository
{
    public MovieRepository(SqliteConnectionFactory connectionFactory)
        : base(connectionFactory, "Movies")
    {
    }

    public override IEnumerable<Movie> GetAll()
    {
        using var connection = ConnectionFactory.CreateConnection();

        return connection.Query<Movie>(
            """
            SELECT *
            FROM Movies
            ORDER BY Id DESC;
            """).AsList();
    }

    public IEnumerable<Movie> GetMoviesByStatus(MovieStatus status)
    {
        using var connection = ConnectionFactory.CreateConnection();

        return connection.Query<Movie>(
            """
            SELECT *
            FROM Movies
            WHERE Status = @Status
            ORDER BY Id DESC;
            """,
            new { Status = (int)status }).AsList();
    }

    public bool IsTitleExists(string title, int? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            return false;

        using var connection = ConnectionFactory.CreateConnection();

        return connection.ExecuteScalar<bool>(
            """
            SELECT EXISTS
            (
                SELECT 1
                FROM Movies
                WHERE LOWER(TRIM(Title)) = LOWER(TRIM(@Title))
                  AND (@ExcludeId IS NULL OR Id <> @ExcludeId)
            );
            """,
            new
            {
                Title = title.Trim(),
                ExcludeId = excludeId
            });
    }

    public override int Insert(Movie entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        using var connection = ConnectionFactory.CreateConnection();

        return connection.ExecuteScalar<int>(
            """
            INSERT INTO Movies
            (
                Title,
                DurationMinutes,
                Description,
                PosterPath,
                TrailerUrl,
                AgeLimit,
                ReleaseDate,
                Status
            )
            VALUES
            (
                @Title,
                @DurationMinutes,
                @Description,
                @PosterPath,
                @TrailerUrl,
                @AgeLimit,
                @ReleaseDate,
                @Status
            );

            SELECT last_insert_rowid();
            """,
            new
            {
                entity.Title,
                entity.DurationMinutes,
                entity.Description,
                entity.PosterPath,
                entity.TrailerUrl,
                entity.AgeLimit,
                ReleaseDate = entity.ReleaseDate.ToString("O"),
                Status = (int)entity.Status
            });
    }

    // Giữ tên Add để tương thích với code cũ của B.
    public int Add(Movie entity)
    {
        return Insert(entity);
    }

    public override bool Update(Movie entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        using var connection = ConnectionFactory.CreateConnection();

        var affectedRows = connection.Execute(
            """
            UPDATE Movies
            SET
                Title = @Title,
                DurationMinutes = @DurationMinutes,
                Description = @Description,
                PosterPath = @PosterPath,
                TrailerUrl = @TrailerUrl,
                AgeLimit = @AgeLimit,
                ReleaseDate = @ReleaseDate,
                Status = @Status
            WHERE Id = @Id;
            """,
            new
            {
                entity.Id,
                entity.Title,
                entity.DurationMinutes,
                entity.Description,
                entity.PosterPath,
                entity.TrailerUrl,
                entity.AgeLimit,
                ReleaseDate = entity.ReleaseDate.ToString("O"),
                Status = (int)entity.Status
            });

        return affectedRows > 0;
    }

    public override bool Delete(int id)
    {
        using var connection = ConnectionFactory.CreateConnection();

        var affectedRows = connection.Execute(
            "DELETE FROM Movies WHERE Id = @Id;",
            new { Id = id });

        return affectedRows > 0;
    }
}
