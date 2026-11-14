using System.Windows.Forms;

namespace CinemaManagement.UI.Helpers
{
    public static class MessageHelper
    {
        public static void ShowInfo(string message, string title = "Thông báo")
            => MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);

        public static void ShowWarning(string message, string title = "Cảnh báo")
            => MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);

        public static void ShowError(string message, string title = "Lỗi")
            => MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);

        public static bool Confirm(string message, string title = "Xác nhận")
            => MessageBox.Show(message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
    }
}