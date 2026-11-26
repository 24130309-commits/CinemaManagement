using System.ComponentModel;
using CinemaManagement.UI.Helpers;

namespace CinemaManagement.UI.Controls;

public sealed class EmptyStateDataGridView : DataGridView
{
    private string _emptyStateText = "Chưa có dữ liệu để hiển thị.";

    [Browsable(true)]
    [Category("Appearance")]
    [DefaultValue("Chưa có dữ liệu để hiển thị.")]
    public string EmptyStateText
    {
        get => _emptyStateText;
        set
        {
            _emptyStateText = value ?? string.Empty;
            Invalidate();
        }
    }

    public EmptyStateDataGridView()
    {
        DoubleBuffered = true;
        BackgroundColor = ThemeHelper.Surface;
        DataSourceChanged += (_, _) => Invalidate();
        RowsAdded += (_, _) => Invalidate();
        RowsRemoved += (_, _) => Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (Rows.Count != 0 || string.IsNullOrWhiteSpace(_emptyStateText))
            return;

        var content = new Rectangle(
            0,
            ColumnHeadersVisible ? ColumnHeadersHeight : 0,
            ClientSize.Width,
            Math.Max(0, ClientSize.Height - (ColumnHeadersVisible ? ColumnHeadersHeight : 0)));

        if (content.Height <= 0)
            return;

        if (content.Width < 80 || content.Height < 72)
        {
            TextRenderer.DrawText(
                e.Graphics,
                _emptyStateText,
                Font,
                content,
                ThemeHelper.Muted,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis |
                TextFormatFlags.NoPrefix);
            return;
        }

        var iconSize = 30;
        var groupTop = content.Top + (content.Height - 66) / 2;
        var icon = new Rectangle(
            content.Left + (content.Width - iconSize) / 2,
            groupTop,
            iconSize,
            iconSize);

        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using (var fill = new SolidBrush(ThemeHelper.AccentSoft))
            e.Graphics.FillEllipse(fill, icon);
        using (var outline = new Pen(Color.FromArgb(222, ThemeHelper.Accent)))
            e.Graphics.DrawEllipse(outline, icon);

        using var iconFont = new Font("Segoe UI", 12F, FontStyle.Bold);
        TextRenderer.DrawText(
            e.Graphics,
            "i",
            iconFont,
            icon,
            ThemeHelper.Accent,
            TextFormatFlags.HorizontalCenter |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.NoPrefix);

        var messageBounds = new Rectangle(
            content.Left + 12,
            groupTop + iconSize + 7,
            Math.Max(0, content.Width - 24),
            29);
        TextRenderer.DrawText(
            e.Graphics,
            _emptyStateText,
            Font,
            messageBounds,
            ThemeHelper.Muted,
            TextFormatFlags.HorizontalCenter |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPrefix);
    }
}
