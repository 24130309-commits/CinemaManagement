using System.Drawing;
using System.IO;

namespace CinemaManagement.UI.Helpers
{
    public static class ImageHelper
    {
        public static Image LoadSafe(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            return Image.FromStream(fs);
        }
    }
}