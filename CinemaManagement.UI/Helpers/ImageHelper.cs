
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using CinemaManagement.Core.Constants;

namespace CinemaManagement.UI.Helpers;

public static class ImageHelper
{
    public static string SavePoster(
        string sourcePath,
        string? posterDirectory = null)
    {
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException(
                "Không tìm thấy ảnh áp phích.",
                sourcePath);
        }

        var directory = Path.GetFullPath(
            posterDirectory ??
            Path.Combine(
                AppContext.BaseDirectory,
                AppConstants.PosterDirectoryName.Replace(
                    '/',
                    Path.DirectorySeparatorChar)));

        Directory.CreateDirectory(directory);

        var extension = Path.GetExtension(sourcePath).ToLowerInvariant();

        if (extension is not ".jpg" and not ".jpeg" and not ".png")
        {
            throw new InvalidOperationException(
                "Chỉ hỗ trợ ảnh JPG hoặc PNG.");
        }

        var destination = Path.Combine(
            directory,
            Guid.NewGuid().ToString("N") + extension);

        using var image = Image.FromFile(sourcePath);
        image.Save(destination);

        return destination;
    }

    public static Image? LoadUnlocked(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return null;

        using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite);

        using var image = Image.FromStream(stream);

        return new Bitmap(image);
    }

    public static Image? LoadSafe(string? path)
    {
        return LoadUnlocked(path);
    }

    public static Image? CreateThumbnail(
        string? path,
        int width,
        int height)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(width),
                "Kích thước ảnh phải lớn hơn 0.");

        using var source = LoadUnlocked(path);

        if (source is null)
            return null;

        var bitmap = new Bitmap(width, height);

        using var graphics = Graphics.FromImage(bitmap);

        graphics.InterpolationMode =
            InterpolationMode.HighQualityBicubic;

        graphics.Clear(Color.Transparent);

        graphics.DrawImage(
            source,
            new Rectangle(0, 0, width, height));

        return bitmap;
    }
}
