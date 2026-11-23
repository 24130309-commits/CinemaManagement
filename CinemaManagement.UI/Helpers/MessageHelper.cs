using System;
using System.Windows.Forms;
using CinemaManagement.Core.Exceptions;

namespace CinemaManagement.UI.Helpers;

public static class MessageHelper
{
    public static void Info(
        string message,
        string title = "Thông báo")
    {
        MessageBox.Show(
            message,
            title,
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    public static void Warning(
        string message,
        string title = "Cảnh báo")
    {
        MessageBox.Show(
            message,
            title,
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }

    public static void Error(
        string message,
        string title = "Lỗi")
    {
        MessageBox.Show(
            message,
            title,
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    public static bool Confirm(
        string message,
        string title = "Xác nhận")
    {
        return MessageBox.Show(
            message,
            title,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question) == DialogResult.Yes;
    }

    public static void Handle(Exception exception)
    {
        if (exception is BusinessException)
            Warning(exception.Message);
        else
            Error(
                "Đã xảy ra lỗi. Vui lòng thử lại.\n" +
                exception.Message);
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