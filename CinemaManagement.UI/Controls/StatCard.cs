using System.ComponentModel;
using System.Drawing.Drawing2D;
using CinemaManagement.UI.Helpers;

namespace CinemaManagement.UI.Controls;

public sealed class StatCard : UserControl
{
    private readonly Label _title = new();
    private readonly Label _value = new();
    private readonly Label _icon = new();
    private Color _accentColor = ThemeHelper.Accent;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color AccentColor
    {
        get => _accentColor;
        set
        {
            _accentColor = value;
            _icon.ForeColor = value;
            Invalidate();
        }
    }

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
        set
        {
            _value.Text = value;
            Invalidate();
        }
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
        ThemeHelper.ConfigureDpiScaling(this);
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);

        Size = new Size(220, 112);
        MinimumSize = new Size(150, 100);
        BackColor = ThemeHelper.SurfaceRaised;
        Padding = new Padding(14);
        Margin = new Padding(7);

        _title.Text = title;
        _title.Font = new Font("Segoe UI", 9.5f);
        _title.ForeColor = ThemeHelper.Muted;
        _title.AutoEllipsis = true;
        _title.TextAlign = ContentAlignment.MiddleLeft;
        _title.AccessibleRole = AccessibleRole.StaticText;
        _title.AccessibleName = title;

        _value.Text = value;
        _value.Font = new Font("Segoe UI", 18f, FontStyle.Bold);
        _value.ForeColor = ThemeHelper.Primary;
        _value.AutoEllipsis = true;
        _value.TextAlign = ContentAlignment.MiddleLeft;
        _value.AccessibleRole = AccessibleRole.StaticText;
        _value.AccessibleName = $"Giá trị {title}";

        _icon.Text = icon;
        _icon.Font = new Font("Segoe UI", 17f, FontStyle.Bold);
        _icon.ForeColor = ThemeHelper.Accent;
        _icon.BackColor = ThemeHelper.AccentSoft;
        _icon.TextAlign = ContentAlignment.MiddleCenter;
        _icon.AccessibleRole = AccessibleRole.None;
        _icon.TabStop = false;

        Controls.AddRange([_title, _value, _icon]);

        LayoutChildren();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        LayoutChildren();
    }

    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        LayoutChildren();
    }

    private void LayoutChildren()
    {
        if (_title.IsDisposed ||
            _value.IsDisposed ||
            _icon.IsDisposed)
        {
            return;
        }

        var scale = DeviceDpi / 96f;
        int Scale(int value) => (int)Math.Round(value * scale);

        var inset = Math.Min(Scale(10), Math.Max(4, ClientSize.Height / 10));
        var iconSize = Math.Min(Scale(40), Math.Max(28, ClientSize.Height - 2 * inset));
        var left = Scale(12);
        var titleHeight = Math.Max(
            Scale(20),
            TextRenderer.MeasureText("Ag", _title.Font, Size.Empty, TextFormatFlags.NoPadding).Height + Scale(4));
        var valueHeight = Math.Max(
            Scale(30),
            TextRenderer.MeasureText("0", _value.Font, Size.Empty, TextFormatFlags.NoPadding).Height + Scale(4));
        var gap = Scale(3);
        var contentWidth = Math.Max(Scale(70), ClientSize.Width - 2 * left);
        var iconLeft = ClientSize.Width - iconSize - inset;
        var availableLabelWidth = Math.Max(Scale(70), iconLeft - left - Scale(8));

        _title.SetBounds(left, inset, availableLabelWidth, titleHeight);

        var valueTop = inset + titleHeight + gap;
        var valueBottom = Math.Max(valueTop + Scale(20), ClientSize.Height - inset);
        var fittedValueHeight = Math.Min(valueHeight, valueBottom - valueTop);
        if (valueTop + fittedValueHeight > ClientSize.Height - inset)
            valueTop = Math.Max(0, ClientSize.Height - inset - fittedValueHeight);

        _value.SetBounds(left, valueTop, contentWidth, fittedValueHeight);
        _icon.SetBounds(iconLeft, inset, iconSize, iconSize);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        using var borderPen = new Pen(ThemeHelper.Border);
        using var accentBrush = new SolidBrush(_accentColor);
        using var topRule = new SolidBrush(Color.FromArgb(34, _accentColor));
        e.Graphics.FillRectangle(topRule, 1, 1, Math.Max(0, Width - 2), 3);
        e.Graphics.FillRectangle(accentBrush, 1, 1, 4, Math.Max(0, Height - 2));
        e.Graphics.DrawRectangle(borderPen, 0, 0, Math.Max(0, Width - 1), Math.Max(0, Height - 1));
    }
}
