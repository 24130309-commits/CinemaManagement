
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using CinemaManagement.BLL.Session;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.DAL.Database;

namespace CinemaManagement.BLL.Services;

public sealed class BackupService : IBackupService
{
    private static readonly string[] RequiredTables =
    {
        "Users",
        "AuditLogs",
        "Settings",
        "Customers",
        "Genres",
        "Movies",
        "MovieGenres",
        "Rooms",
        "Seats",
        "Showtimes",
        "Products",
        "Promotions",
        "PricingRules",
        "Bookings",
        "Payments",
        "Tickets",
        "BookingProducts",
        "SeatHolds"
    };

    private static readonly string[] RequiredIndexes =
    {
        "IX_Payments_BookingId",
        "IX_Payments_Status_PaidAt",
        "UX_Tickets_Active_Showtime_Seat",
        "IX_AuditLogs_User_CreatedAt",
        "IX_Showtimes_StartTime",
        "IX_Showtimes_Room_Start_End",
        "IX_Tickets_Showtime_Status",
        "IX_Bookings_CreatedAt_Status",
        "IX_SeatHolds_ExpiresAt",
        "IX_SeatHolds_SessionId"
    };

    private readonly SqliteConnectionFactory _factory;
    private readonly CurrentUserSession _session;

    public BackupService(
        SqliteConnectionFactory factory,
        CurrentUserSession session)
    {
        _factory = factory
            ?? throw new ArgumentNullException(nameof(factory));
        _session = session
            ?? throw new ArgumentNullException(nameof(session));
    }

    public void Backup(string destinationPath)
    {
        AuthorizationGuard.EnsureAdministrator(_session, "sao lưu dữ liệu");
        if (string.IsNullOrWhiteSpace(destinationPath))
        {
            throw new BusinessException(
                "Vui lòng chọn nơi lưu bản sao lưu.");
        }

        var fullPath = Path.GetFullPath(destinationPath);
        ValidateDifferentDatabase(fullPath);

        var directory = Path.GetDirectoryName(fullPath);

        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new BusinessException(
                "Đường dẫn sao lưu không hợp lệ.");
        }

        Directory.CreateDirectory(directory);

        var temporaryPath = Path.Combine(
            directory,
            $"{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            using (var source = _factory.CreateConnection())
            using (var destination = CreateConnection(temporaryPath))
            {
                source.BackupDatabase(destination);
            }

            ValidateDatabaseFile(temporaryPath);

            File.Move(temporaryPath, fullPath, true);
        }
        catch (BusinessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new BusinessException(
                "Không thể sao lưu cơ sở dữ liệu. Kiểm tra vị trí lưu và thử lại.",
                ex);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                try
                {
                    File.Delete(temporaryPath);
                }
                catch
                {
                }
            }
        }
    }

    public void Restore(string sourcePath)
    {
        AuthorizationGuard.EnsureAdministrator(_session, "khôi phục dữ liệu");
        if (string.IsNullOrWhiteSpace(sourcePath) ||
            !File.Exists(sourcePath))
        {
            throw new BusinessException(
                "Không tìm thấy tệp sao lưu.");
        }

        var fullPath = Path.GetFullPath(sourcePath);
        ValidateDifferentDatabase(fullPath);

        try
        {
            ValidateDatabaseFile(fullPath);

            SqliteConnection.ClearAllPools();

            using (var source = new SqliteConnection(
                new SqliteConnectionStringBuilder
                {
                    DataSource = fullPath,
                    Mode = SqliteOpenMode.ReadOnly,
                    ForeignKeys = true,
                    DefaultTimeout = 30
                }.ToString()))
            {
                source.Open();

                using var destination = _factory.CreateConnection();

                source.BackupDatabase(destination);
            }

            SqliteConnection.ClearAllPools();

            using var restored = _factory.CreateConnection();
            ValidateConnection(restored);
        }
        catch (BusinessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            SqliteConnection.ClearAllPools();

            throw new BusinessException(
                "Không thể khôi phục cơ sở dữ liệu. Hãy kiểm tra tệp sao lưu và thử lại.",
                ex);
        }
    }

    public void BackupDatabase(string destinationPath)
    {
        Backup(destinationPath);
    }

    public void RestoreDatabase(string sourcePath)
    {
        Restore(sourcePath);
    }

    private void ValidateDifferentDatabase(string path)
    {
        var currentPath = Path.GetFullPath(_factory.DatabasePath);

        if (string.Equals(
            path,
            currentPath,
            StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessException(
                "Tệp sao lưu phải khác tệp cơ sở dữ liệu đang sử dụng.");
        }
    }

    private static SqliteConnection CreateConnection(string path)
    {
        var connection = new SqliteConnection(
            new SqliteConnectionStringBuilder
            {
                DataSource = path,
                ForeignKeys = true,
            DefaultTimeout = 30,
            Pooling = false
            }.ToString());

        connection.Open();
        return connection;
    }

    private static void ValidateDatabaseFile(string path)
    {
        if (!File.Exists(path) || new FileInfo(path).Length == 0)
        {
            throw new BusinessException(
                "Tệp cơ sở dữ liệu không tồn tại hoặc rỗng.");
        }

        using var connection = new SqliteConnection(
            new SqliteConnectionStringBuilder
            {
                DataSource = path,
                Mode = SqliteOpenMode.ReadOnly,
                ForeignKeys = true,
                DefaultTimeout = 30,
                Pooling = false
            }.ToString());

        connection.Open();
        ValidateConnection(connection);
    }

    private static void ValidateConnection(SqliteConnection connection)
    {
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "PRAGMA integrity_check;";

            var result = Convert.ToString(command.ExecuteScalar());

            if (!string.Equals(
                result,
                "ok",
                StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessException(
                    "Tệp cơ sở dữ liệu bị lỗi hoặc không hợp lệ.");
            }
        }

        var existingTables = ReadSchemaNames(
            connection,
            "table");

        var missingTables = RequiredTables
            .Where(table => !existingTables.Contains(table))
            .ToList();

        if (missingTables.Count > 0)
        {
            throw new BusinessException(
                "Tệp cơ sở dữ liệu thiếu bảng: " +
                string.Join(", ", missingTables));
        }

        var existingIndexes = ReadSchemaNames(
            connection,
            "index");

        var missingIndexes = RequiredIndexes
            .Where(index => !existingIndexes.Contains(index))
            .ToList();

        if (missingIndexes.Count > 0)
        {
            throw new BusinessException(
                "Tệp cơ sở dữ liệu thiếu index: " +
                string.Join(", ", missingIndexes));
        }

        using (var command = connection.CreateCommand())
        {
            command.CommandText = "PRAGMA foreign_key_check;";

            using var reader = command.ExecuteReader();

            if (reader.Read())
            {
                throw new BusinessException(
                    "Tệp cơ sở dữ liệu có dữ liệu vi phạm khóa ngoại.");
            }
        }
    }

    private static HashSet<string> ReadSchemaNames(
        SqliteConnection connection,
        string type)
    {
        var names = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        using var command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT name
            FROM sqlite_master
            WHERE type = $type
              AND name NOT LIKE 'sqlite_%';
            """;

        command.Parameters.AddWithValue("$type", type);

        using var reader = command.ExecuteReader();

        while (reader.Read())
            names.Add(reader.GetString(0));

        return names;
    }
}
