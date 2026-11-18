
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

    // Tìm kiếm phim theo từ khóa, thể loại và trạng thái.
    public IEnumerable<Movie> Search(
        string keyword,
        int? genreId = null,
        MovieStatus? status = null)
    {
        using var connection = ConnectionFactory.CreateConnection();

        return connection.Query<Movie>(
            """
            SELECT DISTINCT m.*
            FROM Movies m
            LEFT JOIN MovieGenres mg ON mg.MovieId = m.Id
            WHERE
                (
                    @Keyword = ''
                    OR m.Title LIKE @SearchKeyword
                )
                AND
                (
                    @GenreId IS NULL
                    OR mg.GenreId = @GenreId
                )
                AND
                (
                    @Status IS NULL
                    OR m.Status = @Status
                )
            ORDER BY m.Id DESC;
            """,
            new
            {
                Keyword = (keyword ?? string.Empty).Trim(),
                SearchKeyword = $"%{(keyword ?? string.Empty).Trim()}%",
                GenreId = genreId,
                Status = status.HasValue ? (int?)status.Value : null
            }).AsList();
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

    // Lấy danh sách mã thể loại của một phim.
    public IEnumerable<int> GetGenreIds(int movieId)
    {
        using var connection = ConnectionFactory.CreateConnection();

        return connection.Query<int>(
            """
            SELECT GenreId
            FROM MovieGenres
            WHERE MovieId = @MovieId
            ORDER BY GenreId;
            """,
            new { MovieId = movieId }).AsList();
    }

    // Cập nhật danh sách thể loại của một phim.
    public void SetGenres(int movieId, IEnumerable<int> genreIds)
    {
        ArgumentNullException.ThrowIfNull(genreIds);

        if (movieId <= 0)
            throw new ArgumentException("Mã phim không hợp lệ.", nameof(movieId));

        var ids = genreIds.Distinct().ToList();

        if (ids.Any(id => id <= 0))
            throw new ArgumentException("Mã thể loại không hợp lệ.", nameof(genreIds));

        using var connection = ConnectionFactory.CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            // Xóa các thể loại cũ của phim.
            connection.Execute(
                """
                DELETE FROM MovieGenres
                WHERE MovieId = @MovieId;
                """,
                new { MovieId = movieId },
                transaction);

            // Thêm danh sách thể loại mới.
            foreach (var genreId in ids)
            {
                connection.Execute(
                    """
                    INSERT INTO MovieGenres (MovieId, GenreId)
                    VALUES (@MovieId, @GenreId);
                    """,
                    new
                    {
                        MovieId = movieId,
                        GenreId = genreId
                    },
                    transaction);
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
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
