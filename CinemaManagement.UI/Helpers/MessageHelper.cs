using System;
using System.Windows.Forms;
using System.Diagnostics;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.UI.Forms;

namespace CinemaManagement.UI.Helpers;

public static class MessageHelper
{
    public static void Info(
        string message,
        string title = "Thông báo")
    {
        Show(message, title, MessageBoxIcon.Information);
    }

    public static void Warning(
        string message,
        string title = "Cảnh báo")
    {
        Show(message, title, MessageBoxIcon.Warning);
    }

    public static void Error(
        string message,
        string title = "Lỗi")
    {
        Show(message, title, MessageBoxIcon.Error);
    }

    public static bool Confirm(
        string message,
        string title = "Xác nhận")
    {
        return Show(message, title, MessageBoxIcon.Question, MessageBoxButtons.YesNo)
            == DialogResult.Yes;
    }

    public static DialogResult Decide(
        string message,
        string title,
        MessageBoxIcon icon,
        MessageBoxButtons buttons)
    {
        return Show(message, title, icon, buttons);
    }

    private static DialogResult Show(
        string message,
        string title,
        MessageBoxIcon icon,
        MessageBoxButtons buttons = MessageBoxButtons.OK)
    {
        using var dialog = new AppMessageDialog(
            title,
            message,
            icon,
            buttons);

        var owner = Form.ActiveForm;
        return owner is null || owner == dialog
            ? dialog.ShowDialog()
            : dialog.ShowDialog(owner);
    }

    public static void Handle(
        Exception exception,
        string? userMessage = null)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is BusinessException)
        {
            Warning(exception.Message);
            return;
        }

        Trace.TraceError("CinemaManagement UI error: {0}", exception);
        Error(userMessage ?? "Đã xảy ra lỗi. Vui lòng thử lại.");
    }

    public static void ShowInfo(
        string message,
        string title = "Thông báo")
    {
        Info(message, title);
    }

    public static void ShowWarning(
        string message,
        string title = "Cảnh báo")
    {
        Warning(message, title);
    }

    public static void ShowError(
        string message,
        string title = "Lỗi")
    {
        Error(message, title);
    }
}
