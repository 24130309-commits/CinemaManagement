namespace CinemaManagement.UI.Helpers;

public static class MessageHelper
{
    public static void Info(string message, string title = "Thông báo") =>
        MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);

    public static void Warning(string message, string title = "Cảnh báo") =>
        MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);

    public static void Error(string message, string title = "Lỗi") =>
        MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);

    public static bool Confirm(string message, string title = "Xác nhận") =>
        MessageBox.Show(message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question)
        == DialogResult.Yes;

    public static void Handle(Exception exception)
    {
        if (exception is CinemaManagement.Core.Exceptions.BusinessException)
            Warning(exception.Message);
        else
            Error("Đã xảy ra lỗi. Vui lòng thử lại.\n" + exception.Message);
    }
}