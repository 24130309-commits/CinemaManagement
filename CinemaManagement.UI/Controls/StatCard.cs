
using System.ComponentModel;
using CinemaManagement.UI.Helpers;

namespace CinemaManagement.UI.Controls;

public sealed class StatCard : UserControl
{
    private readonly Label _title = new();
    private readonly Label _value = new();
    private readonly Label _icon = new();

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Title
    {
        get => _title.Text;
        set => _title.Text = value;
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Value
    {
        get => _value.Text;
        set => _value.Text = value;
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string IconText
    {
        get => _icon.Text;
        set => _icon.Text = value;
    }

    public StatCard() : this("Thống kê", "0", "●")
    {
    }

    public StatCard(string title, string value, string icon = "●")
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer,
            true);

        Size = new Size(230, 112);
        MinimumSize = new Size(180, 100);
        BackColor = ThemeHelper.Surface;
        Padding = new Padding(14);
        Margin = new Padding(7);

        _icon.Text = icon;
        _icon.Font = new Font("Segoe UI", 19, FontStyle.Bold);
        _icon.ForeColor = ThemeHelper.Accent;
        _icon.AutoSize = true;
        _icon.Location = new Point(Width - 52, 18);
        _icon.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        _title.Text = title;
        _title.Font = ThemeHelper.SmallFont;
        _title.ForeColor = ThemeHelper.Muted;
        _title.Location = new Point(14, 14);
        _title.AutoSize = true;

        _value.Text = value;
        _value.Font = new Font("Segoe UI", 19, FontStyle.Bold);
        _value.ForeColor = ThemeHelper.Primary;
        _value.Location = new Point(14, 44);
        _value.AutoSize = true;

        Controls.AddRange([_title, _value, _icon]);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        using var pen = new Pen(ThemeHelper.Border);

        e.Graphics.DrawRectangle(
            pen,
            0,
            0,
            Width - 1,
            Height - 1);
    }
}
