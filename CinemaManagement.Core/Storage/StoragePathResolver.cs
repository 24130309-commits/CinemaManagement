
using System;
using System.IO;

namespace CinemaManagement.Core.Storage;

public static class StoragePathResolver
{
    private const string DataFolderName = "Data";
    private const string DatabaseFileName = "CinemaManagement.db";

    public static string DataRoot { get; } = FindDataRoot();

    public static string DatabasePath =>
        Path.Combine(DataRoot, DatabaseFileName);

    public static string PostersDirectory =>
        Path.Combine(DataRoot, "Posters");

    public static string MoviesDirectory =>
        Path.Combine(DataRoot, "Movies");

    private static string FindDataRoot()
    {
        // 1. Ưu tiên đường dẫn được cấu hình bên ngoài.
        var configured = Environment.GetEnvironmentVariable(
            "CINEMA_DATA_DIR");

        if (!string.IsNullOrWhiteSpace(configured))
            return Path.GetFullPath(configured);

        // 2. Khi chạy bản Publish: Data nằm cạnh EXE.
        var besideApp = Path.Combine(
            AppContext.BaseDirectory,
            DataFolderName);

        if (File.Exists(Path.Combine(besideApp, DatabaseFileName)))
            return besideApp;

        // 3. Khi chạy từ Rider: tìm Data trong thư mục dự án
        // hoặc các thư mục cha.
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current != null)
        {
            var candidate = Path.Combine(
                current.FullName,
                DataFolderName);

            if (File.Exists(Path.Combine(candidate, DatabaseFileName)))
                return candidate;

            current = current.Parent;
        }

        // Không tìm thấy database: báo lỗi rõ ràng,
        // không âm thầm tạo database trắng.
        throw new FileNotFoundException(
            "Không tìm thấy Data/CinemaManagement.db. " +
            "Hãy kiểm tra thư mục Data và database demo.");
    }

    public static string ResolvePath(string? storedPath)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
            return string.Empty;

        if (Path.IsPathRooted(storedPath))
            return storedPath;

        return Path.GetFullPath(
            Path.Combine(DataRoot, storedPath));
    }

    public static string ToRelativePath(string fullPath)
    {
        return Path.GetRelativePath(
            DataRoot,
            Path.GetFullPath(fullPath))
            .Replace('\\', '/');
    }
}
