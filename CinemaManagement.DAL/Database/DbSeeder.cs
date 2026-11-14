using Dapper;
using BCrypt.Net;
using CinemaManagement.Core.Constants;
using CinemaManagement.Core.Enums;

namespace CinemaManagement.DAL.Database;

public sealed class DbSeeder
{
    private readonly SqliteConnectionFactory _factory;

    public DbSeeder(SqliteConnectionFactory factory) => _factory = factory;

    public void SeedIfEmpty()
    {
        using var connection = _factory.CreateConnection();
        using var transaction = connection.BeginTransaction();

        if (connection.ExecuteScalar<long>(
                "SELECT COUNT(*) FROM Users",
                transaction: transaction) == 0)
        {
            connection.Execute(
                @"INSERT INTO Users
                  (Username, PasswordHash, FullName, Role, IsActive)
                  VALUES
                  (@Username, @PasswordHash, @FullName, @Role, 1)",
                new
                {
                    Username = AppConstants.DefaultAdminUsername,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(AppConstants.DefaultAdminPassword),
                    FullName = "Quản trị viên",
                    Role = (int)UserRole.Admin
                },
                transaction);
        }

        var defaults = new[]
        {
            new
            {
                Key = AppConstants.SettingHoldMinutes,
                Value = AppConstants.DefaultHoldMinutes.ToString()
            },
            new
            {
                Key = AppConstants.SettingCleaningMinutes,
                Value = AppConstants.DefaultCleaningMinutes.ToString()
            },
            new
            {
                Key = AppConstants.SettingCinemaName,
                Value = AppConstants.DefaultCinemaName
            }
        };

        connection.Execute(
            "INSERT OR IGNORE INTO Settings([Key], [Value]) VALUES (@Key, @Value)",
            defaults,
            transaction);

        if (connection.ExecuteScalar<long>(
                "SELECT COUNT(*) FROM Genres",
                transaction: transaction) == 0)
        {
            connection.Execute(
                "INSERT INTO Genres(Name) VALUES ('Hành động'), ('Tâm lý'), ('Hoạt hình')",
                transaction: transaction);
        }

        if (connection.ExecuteScalar<long>(
                "SELECT COUNT(*) FROM Movies",
                transaction: transaction) == 0)
        {
            connection.Execute(
                @"INSERT INTO Movies
                  (Title, DurationMinutes, Description, PosterPath, TrailerUrl,
                   AgeLimit, ReleaseDate, Status)
                  VALUES
                  ('Đêm chiếu đầu tiên', 125,
                   'Phim mẫu để kiểm tra hệ thống quản lý rạp.',
                   '', '', 13, @ReleaseDate, @Status)",
                new
                {
                    ReleaseDate = DateTime.Today.ToString("yyyy-MM-dd"),
                    Status = (int)MovieStatus.NowShowing
                },
                transaction);

            var movieId = connection.ExecuteScalar<long>(
                "SELECT last_insert_rowid()",
                transaction: transaction);

            var genreId = connection.ExecuteScalar<long>(
                "SELECT Id FROM Genres ORDER BY Id LIMIT 1",
                transaction: transaction);

            connection.Execute(
                "INSERT OR IGNORE INTO MovieGenres(MovieId, GenreId) VALUES (@movieId, @genreId)",
                new { movieId, genreId },
                transaction);
        }

        if (connection.ExecuteScalar<long>(
                "SELECT COUNT(*) FROM Rooms",
                transaction: transaction) == 0)
        {
            connection.Execute(
                @"INSERT INTO Rooms
                  (Name, TotalRows, TotalCols, ScreenType, IsActive)
                  VALUES ('Phòng 1', 6, 8, @ScreenType, 1)",
                new { ScreenType = (int)ScreenType.TwoD },
                transaction);

            var roomId = connection.ExecuteScalar<long>(
                "SELECT last_insert_rowid()",
                transaction: transaction);

            for (var row = 0; row < 6; row++)
            {
                var rowLabel = ((char)('A' + row)).ToString();

                for (var col = 1; col <= 8; col++)
                {
                    connection.Execute(
                        @"INSERT INTO Seats
                          (RoomId, RowLabel, ColNumber, SeatType)
                          VALUES (@roomId, @rowLabel, @col, @seatType)",
                        new
                        {
                            roomId,
                            rowLabel,
                            col,
                            seatType = (int)SeatType.Normal
                        },
                        transaction);
                }
            }
        }

        if (connection.ExecuteScalar<long>(
                "SELECT COUNT(*) FROM Showtimes",
                transaction: transaction) == 0 &&
            connection.ExecuteScalar<long>(
                "SELECT COUNT(*) FROM Movies",
                transaction: transaction) > 0 &&
            connection.ExecuteScalar<long>(
                "SELECT COUNT(*) FROM Rooms",
                transaction: transaction) > 0)
        {
            var movieId = connection.ExecuteScalar<long>(
                "SELECT Id FROM Movies ORDER BY Id LIMIT 1",
                transaction: transaction);

            var roomId = connection.ExecuteScalar<long>(
                "SELECT Id FROM Rooms ORDER BY Id LIMIT 1",
                transaction: transaction);

            var start = DateTime.Today.AddDays(1).AddHours(18);
            var end = start.AddMinutes(125 + AppConstants.DefaultCleaningMinutes);

            connection.Execute(
                @"INSERT INTO Showtimes
                  (MovieId, RoomId, StartTime, EndTime, BasePrice, IsActive)
                  VALUES
                  (@movieId, @roomId, @start, @end, @price, 1)",
                new
                {
                    movieId,
                    roomId,
                    start = start.ToString("O"),
                    end = end.ToString("O"),
                    price = 85000m
                },
                transaction);
        }

        if (connection.ExecuteScalar<long>(
                "SELECT COUNT(*) FROM Products",
                transaction: transaction) == 0)
        {
            connection.Execute(
                @"INSERT INTO Products(Name, Price, Category, IsActive)
                  VALUES
                  ('Bắp rang bơ', 45000, 'Bắp', 1),
                  ('Nước suối', 20000, 'Nước', 1),
                  ('Combo bắp + nước', 60000, 'Combo', 1)",
                transaction: transaction);
        }

        transaction.Commit();
    }
}