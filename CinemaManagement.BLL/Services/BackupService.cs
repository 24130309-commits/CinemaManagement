using Microsoft.Data.Sqlite;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.DAL.Database;

namespace CinemaManagement.BLL.Services;

public sealed class BackupService : IBackupService
{
    private readonly SqliteConnectionFactory _factory;

    public BackupService(SqliteConnectionFactory factory)
    {
        _factory = factory;
    }

    public void Backup(string destinationPath)
    {
        if (string.IsNullOrWhiteSpace(destinationPath))
            throw new BusinessException(
                "Vui lòng chọn nơi lưu bản sao lưu.");

        var full = Path.GetFullPath(destinationPath);

        if (string.Equals(
                full,
                _factory.DatabasePath,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessException(
                "Tệp sao lưu phải khác tệp cơ sở dữ liệu đang sử dụng.");
        }

        var directory = Path.GetDirectoryName(full);

        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        using var source = _factory.CreateConnection();

        using var destination = new SqliteConnection(
            new SqliteConnectionStringBuilder
            {
                DataSource = full,
                ForeignKeys = true
            }.ToString());

        destination.Open();
        source.BackupDatabase(destination);
    }

    public void Restore(string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath) ||
            !File.Exists(sourcePath))
        {
            throw new BusinessException("Không tìm thấy tệp sao lưu.");
        }

        var full = Path.GetFullPath(sourcePath);

        if (string.Equals(
                full,
                _factory.DatabasePath,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessException(
                "Tệp khôi phục phải khác tệp cơ sở dữ liệu đang sử dụng.");
        }

        try
        {
            SqliteConnection.ClearAllPools();

            using var source = new SqliteConnection(
                new SqliteConnectionStringBuilder
                {
                    DataSource = full,
                    Mode = SqliteOpenMode.ReadOnly
                }.ToString());

            source.Open();

            using (var cmd = source.CreateCommand())
            {
                cmd.CommandText = "PRAGMA integrity_check;";

                if (!string.Equals(
                        Convert.ToString(cmd.ExecuteScalar()),
                        "ok",
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new BusinessException(
                        "Tệp sao lưu bị lỗi hoặc không phải SQLite hợp lệ.");
                }
            }

            using var destination = _factory.CreateConnection();

            source.BackupDatabase(destination);
        }
        catch (BusinessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new BusinessException(
                "Không thể khôi phục cơ sở dữ liệu: " + ex.Message,
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
}