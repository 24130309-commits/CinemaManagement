
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

    public IEnumerable<Movie> Search(
        string keyword,
        int? genreId = null,
        MovieStatus? status = null)
    {
        using var connection = ConnectionFactory.CreateConnection();

        var normalizedKeyword = (keyword ?? string.Empty).Trim();

        return connection.Query<Movie>(
            """
            SELECT DISTINCT m.*
            FROM Movies m
            LEFT JOIN MovieGenres mg ON mg.MovieId = m.Id
            WHERE
                (
                    @Keyword = ''
                    OR m.Title LIKE @SearchKeyword
                    OR m.Description LIKE @SearchKeyword
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
                Keyword = normalizedKeyword,
                SearchKeyword = $"%{normalizedKeyword}%",
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

    public IEnumerable<int> GetGenreIds(int movieId)
    {
        if (movieId <= 0)
            return Enumerable.Empty<int>();

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

    public void SetGenres(int movieId, IEnumerable<int> genreIds)
    {
        ArgumentNullException.ThrowIfNull(genreIds);

        if (movieId <= 0)
            throw new ArgumentException(
                "Mã phim không hợp lệ.", nameof(movieId));

        var ids = genreIds.Distinct().ToList();

        if (ids.Any(id => id <= 0))
            throw new ArgumentException(
                "Mã thể loại không hợp lệ.", nameof(genreIds));

        using var connection = ConnectionFactory.CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            var movieExists = connection.ExecuteScalar<bool>(
                "SELECT EXISTS(SELECT 1 FROM Movies WHERE Id = @MovieId);",
                new { MovieId = movieId },
                transaction);

            if (!movieExists)
                throw new InvalidOperationException(
                    "Không tìm thấy phim cần gán thể loại.");

            ValidateGenres(connection, transaction, ids);

            connection.Execute(
                "DELETE FROM MovieGenres WHERE MovieId = @MovieId;",
                new { MovieId = movieId },
                transaction);

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

    public int SaveWithGenres(Movie entity, IEnumerable<int>? genreIds)
    {
        ArgumentNullException.ThrowIfNull(entity);

        ValidateMovie(entity);

        var ids = genreIds?.Distinct().ToList();

        if (ids?.Any(id => id <= 0) == true)
            throw new ArgumentException(
                "Mã thể loại không hợp lệ.", nameof(genreIds));

        using var connection = ConnectionFactory.CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            if (ids != null)
                ValidateGenres(connection, transaction, ids);

            int movieId;

            if (entity.Id <= 0)
            {
                movieId = connection.ExecuteScalar<int>(
                    """
                    INSERT INTO Movies
                    (
                        Title, DurationMinutes, Description, PosterPath,
                        TrailerUrl, VideoPath, AgeLimit, ReleaseDate, Status
                    )
                    VALUES
                    (
                        @Title, @DurationMinutes, @Description, @PosterPath,
                        @TrailerUrl, @VideoPath, @AgeLimit, @ReleaseDate, @Status
                    );

                    SELECT last_insert_rowid();
                    """,
                    GetParameters(entity),
                    transaction);
            }
            else
            {
                movieId = entity.Id;

                var affectedRows = connection.Execute(
                    """
                    UPDATE Movies
                    SET
                        Title = @Title,
                        DurationMinutes = @DurationMinutes,
                        Description = @Description,
                        PosterPath = @PosterPath,
                        TrailerUrl = @TrailerUrl,
                        VideoPath = @VideoPath,
                        AgeLimit = @AgeLimit,
                        ReleaseDate = @ReleaseDate,
                        Status = @Status
                    WHERE Id = @Id;
                    """,
                    GetParameters(entity),
                    transaction);

                if (affectedRows == 0)
                    throw new InvalidOperationException(
                        "Không tìm thấy phim cần cập nhật.");
            }

            if (ids != null)
            {
                connection.Execute(
                    "DELETE FROM MovieGenres WHERE MovieId = @MovieId;",
                    new { MovieId = movieId },
                    transaction);

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
            }

            transaction.Commit();

            entity.Id = movieId;

            return movieId;
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

    public override int Insert(Movie entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        ValidateMovie(entity);

        using var connection = ConnectionFactory.CreateConnection();

        return connection.ExecuteScalar<int>(
            """
            INSERT INTO Movies
            (
                Title, DurationMinutes, Description, PosterPath,
                TrailerUrl, VideoPath, AgeLimit, ReleaseDate, Status
            )
            VALUES
            (
                @Title, @DurationMinutes, @Description, @PosterPath,
                @TrailerUrl, @VideoPath, @AgeLimit, @ReleaseDate, @Status
            );

            SELECT last_insert_rowid();
            """,
            GetParameters(entity));
    }

    public int Add(Movie entity)
    {
        return Insert(entity);
    }

    public override bool Update(Movie entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        ValidateMovie(entity);

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
                VideoPath = @VideoPath,
                AgeLimit = @AgeLimit,
                ReleaseDate = @ReleaseDate,
                Status = @Status
            WHERE Id = @Id;
            """,
            GetParameters(entity));

        return affectedRows > 0;
    }

    public override bool Delete(int id)
    {
        if (id <= 0)
            return false;

        using var connection = ConnectionFactory.CreateConnection();

        var affectedRows = connection.Execute(
            "DELETE FROM Movies WHERE Id = @Id;",
            new { Id = id });

        return affectedRows > 0;
    }

    private static object GetParameters(Movie entity)
    {
        return new
        {
            entity.Id,
            entity.Title,
            entity.DurationMinutes,
            entity.Description,
            entity.PosterPath,
            entity.TrailerUrl,
            entity.VideoPath,
            entity.AgeLimit,
            ReleaseDate = entity.ReleaseDate.ToString("O"),
            Status = (int)entity.Status
        };
    }

    private static void ValidateMovie(Movie entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Title))
            throw new ArgumentException("Tên phim không được để trống.");

        if (entity.DurationMinutes <= 0)
            throw new ArgumentException("Thời lượng phim phải lớn hơn 0.");

        if (entity.AgeLimit < 0)
            throw new ArgumentException("Giới hạn độ tuổi không hợp lệ.");
    }

    private static void ValidateGenres(
        System.Data.IDbConnection connection,
        System.Data.IDbTransaction transaction,
        List<int> genreIds)
    {
        if (genreIds.Count == 0)
            return;

        var existingCount = connection.ExecuteScalar<int>(
            """
            SELECT COUNT(*)
            FROM Genres
            WHERE Id IN @Ids;
            """,
            new { Ids = genreIds },
            transaction);

        if (existingCount != genreIds.Count)
            throw new InvalidOperationException(
                "Một hoặc nhiều thể loại không tồn tại.");
    }
}
