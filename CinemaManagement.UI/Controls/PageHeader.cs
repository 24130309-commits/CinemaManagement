using System.ComponentModel;
using CinemaManagement.UI.Helpers;

namespace CinemaManagement.UI.Controls;

public sealed class PageHeader : UserControl
{
    private readonly Label _title = new();
    private readonly Label _description = new();
    private readonly FlowLayoutPanel _actions = new();

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Title
    {
        get => _title.Text;
        set => _title.Text = value;
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Description
    {
        get => _description.Text;
        set => _description.Text = value;
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Control.ControlCollection Actions => _actions.Controls;

    public PageHeader(string title, string description)
    {
        ThemeHelper.ConfigureDpiScaling(this);
        MinimumSize = new Size(0, 82);
        Height = 86;
        Dock = DockStyle.Top;
        BackColor = ThemeHelper.Surface;
        Padding = new Padding(ThemeHelper.Spacing.Large, 10, ThemeHelper.Spacing.Large, 9);
        AccessibleRole = AccessibleRole.Grouping;
        AccessibleName = title;

        _title.Text = title;
        _title.Dock = DockStyle.Top;
        // Reserve enough vertical space for Segoe UI glyphs. The previous
        // 31px row clipped Vietnamese accents at common Windows DPI settings.
        _title.Height = 44;
        _title.Font = new Font("Segoe UI Semibold", 17f, FontStyle.Bold);
        _title.ForeColor = ThemeHelper.Primary;
        _title.TextAlign = ContentAlignment.MiddleLeft;
        _title.AutoEllipsis = true;
        _title.AccessibleRole = AccessibleRole.StaticText;
        _title.AccessibleName = title;

        _description.Text = description;
        _description.Dock = DockStyle.Fill;
        _description.TextAlign = ContentAlignment.MiddleLeft;
        _description.Font = new Font("Segoe UI", 9.5f);
        _description.ForeColor = ThemeHelper.Muted;
        _description.AutoEllipsis = false;
        _description.AccessibleRole = AccessibleRole.StaticText;

        _actions.Dock = DockStyle.Right;
        _actions.AutoSize = true;
        _actions.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _actions.FlowDirection = FlowDirection.RightToLeft;
        _actions.WrapContents = true;
        _actions.Padding = new Padding(4);
        _actions.BackColor = ThemeHelper.Surface;

        Resize += (_, _) =>
        {
            _actions.MaximumSize = new Size(
                Math.Max(120, ClientSize.Width / 2),
                Math.Max(34, ClientSize.Height - 8));
        };

        Controls.Add(_description);
        Controls.Add(_title);
        Controls.Add(_actions);

        Paint += (_, e) =>
        {
            using var pen = new Pen(ThemeHelper.Border);
            e.Graphics.DrawLine(pen, 0, Height - 1, Width, Height - 1);
        };
    }
}
