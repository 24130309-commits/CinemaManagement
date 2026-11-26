using System.Drawing;
using System.Windows.Forms;
using CinemaManagement.UI.Controls;
using CinemaManagement.UI.Helpers;

namespace CinemaManagement.UI.Forms;

internal sealed class AppMessageDialog : Form
{
    public AppMessageDialog(
        string title,
        string message,
        MessageBoxIcon icon,
        MessageBoxButtons buttons)
    {
        Text = title;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        MaximizeBox = false;
        MinimizeBox = false;
        var messageSize = TextRenderer.MeasureText(
            message,
            ThemeHelper.BodyFont,
            new Size(390, int.MaxValue),
            TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
        var workingAreaHeight = Screen.PrimaryScreen?.WorkingArea.Height ?? 900;
        var maximumBodyHeight = Math.Max(88, workingAreaHeight - 240);
        var bodyHeight = Math.Clamp(messageSize.Height + 24, 88, maximumBodyHeight);

        ClientSize = new Size(510, bodyHeight + 162);
        MinimumSize = new Size(420, 230);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = ThemeHelper.Background;
        Font = ThemeHelper.BodyFont;
        AccessibleRole = AccessibleRole.Dialog;
        AccessibleName = title;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(14),
            BackColor = ThemeHelper.Background
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));

        var header = new PageHeader(title, "Cinema Management");
        header.Dock = DockStyle.Fill;

        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(8, 12, 8, 8),
            BackColor = ThemeHelper.Surface
        };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 58));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var (symbol, color, softColor) = icon switch
        {
            MessageBoxIcon.Error or MessageBoxIcon.Hand or MessageBoxIcon.Stop =>
                ("×", ThemeHelper.Danger, ThemeHelper.DangerSoft),
            MessageBoxIcon.Warning or MessageBoxIcon.Exclamation =>
                ("!", ThemeHelper.Warning, ThemeHelper.WarningSoft),
            MessageBoxIcon.Question =>
                ("?", ThemeHelper.Accent, ThemeHelper.AccentSoft),
            _ => ("i", ThemeHelper.Info, ThemeHelper.InfoSoft)
        };

        var marker = new Label
        {
            Text = symbol,
            Dock = DockStyle.Top,
            Size = new Size(44, 44),
            BackColor = softColor,
            ForeColor = color,
            Font = new Font("Segoe UI Semibold", 22f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            AccessibleRole = AccessibleRole.Graphic,
            AccessibleName = "Biểu tượng thông báo"
        };

        var content = new TextBox
        {
            Text = message,
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            WordWrap = true,
            ScrollBars = ScrollBars.Vertical,
            BorderStyle = BorderStyle.None,
            BackColor = ThemeHelper.Surface,
            ForeColor = ThemeHelper.Text,
            Font = ThemeHelper.BodyFont,
            AccessibleRole = AccessibleRole.Text,
            AccessibleName = "Nội dung thông báo"
        };

        body.Controls.Add(marker, 0, 0);
        body.Controls.Add(content, 1, 0);

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 10, 0, 0),
            BackColor = ThemeHelper.Background
        };

        if (buttons == MessageBoxButtons.OK)
        {
            var close = CreateButton("Đóng", ThemeHelper.Accent);
            close.DialogResult = DialogResult.OK;
            ThemeHelper.StyleActionButton(close, ThemeHelper.ActionButtonRole.Primary);
            footer.Controls.Add(close);
            AcceptButton = close;
            CancelButton = close;
        }
        else
        {
            var confirm = CreateButton(
                buttons == MessageBoxButtons.YesNoCancel
                    ? "Thanh toán thành công"
                    : "Xác nhận",
                ThemeHelper.Accent);
            confirm.DialogResult = DialogResult.Yes;
            ThemeHelper.StyleActionButton(confirm, ThemeHelper.ActionButtonRole.Primary);
            footer.Controls.Add(confirm);

            var cancel = CreateButton("Hủy", ThemeHelper.Muted);
            cancel.DialogResult = buttons == MessageBoxButtons.YesNoCancel
                ? DialogResult.Cancel
                : DialogResult.No;
            ThemeHelper.StyleActionButton(cancel, ThemeHelper.ActionButtonRole.Secondary);
            footer.Controls.Add(cancel);

            if (buttons == MessageBoxButtons.YesNoCancel)
            {
                var fail = CreateButton("Thất bại", ThemeHelper.Danger);
                fail.DialogResult = DialogResult.No;
                ThemeHelper.StyleActionButton(fail, ThemeHelper.ActionButtonRole.Danger);
                footer.Controls.Add(fail);
            }

            // Keyboard confirmation defaults to the non-destructive option.
            AcceptButton = cancel;
            CancelButton = cancel;
        }

        root.Controls.Add(header, 0, 0);
        root.Controls.Add(body, 0, 1);
        root.Controls.Add(footer, 0, 2);
        Controls.Add(root);
    }

    private static Button CreateButton(string text, Color color)
    {
        var button = new Button
        {
            Text = text,
            Size = new Size(
                Math.Max(118, TextRenderer.MeasureText(text, ThemeHelper.BodyFont).Width + 26),
                38),
            Margin = new Padding(6, 0, 0, 0),
            BackColor = color,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false,
            Cursor = Cursors.Hand,
            AccessibleRole = AccessibleRole.PushButton
        };
        button.FlatAppearance.BorderSize = 0;
        return button;
    }
}
