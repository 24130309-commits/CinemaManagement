using System.ComponentModel;
using System.Drawing.Drawing2D;
using CinemaManagement.UI.Helpers;

namespace CinemaManagement.UI.Controls;

public sealed class RevenueTrendChart : Control
{
    private IReadOnlyList<(string Label, decimal Value)> _points = [];
    private string _emptyMessage = "Chưa có dữ liệu doanh thu trong 7 ngày gần nhất.";

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IReadOnlyList<(string Label, decimal Value)> Points
    {
        get => _points;
        set
        {
            _points = value ?? [];
            Invalidate();
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string ChartTitle { get; set; } =
        "DOANH THU 7 NGÀY GẦN NHẤT";

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string EmptyMessage
    {
        get => _emptyMessage;
        set
        {
            _emptyMessage = value ?? string.Empty;
            Invalidate();
        }
    }

    public RevenueTrendChart()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = ThemeHelper.Surface;
        ForeColor = ThemeHelper.Primary;
        MinimumSize = new Size(260, 190);
        Margin = new Padding(6);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (ClientSize.Width < 100 ||
            ClientSize.Height < 100)
        {
            return;
        }

        var graphics = e.Graphics;

        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        using var titleFont =
            new Font("Segoe UI", 9.5f, FontStyle.Bold);

        using var labelFont =
            new Font("Segoe UI", 8f);

        using var titleBrush =
            new SolidBrush(ThemeHelper.Primary);

        using var mutedBrush =
            new SolidBrush(ThemeHelper.Muted);

        using var gridPen =
            new Pen(ThemeHelper.Border, 1);

        using var linePen =
            new Pen(ThemeHelper.Accent, 2.8f)
            {
                LineJoin = LineJoin.Round
            };

        using var pointBrush =
            new SolidBrush(ThemeHelper.Accent);

        using var pointBorder =
            new Pen(Color.White, 2);

        graphics.DrawString(
            ChartTitle,
            titleFont,
            titleBrush,
            14,
            10);

        if (_points.Count == 0)
        {
            graphics.DrawString(
                _emptyMessage,
                labelFont,
                mutedBrush,
                16,
                58);

            return;
        }

        var plot = new Rectangle(
            66,
            48,
            Math.Max(100, Width - 84),
            Math.Max(70, Height - 92));

        var max = Math.Max(
            1m,
            _points.Max(point => point.Value));

        for (var i = 0; i < 4; i++)
        {
            var y = plot.Top + plot.Height * i / 3;

            graphics.DrawLine(
                gridPen,
                plot.Left,
                y,
                plot.Right,
                y);

            var amount = max * (3 - i) / 3;

            graphics.DrawString(
                FormatAxisValue(amount),
                labelFont,
                mutedBrush,
                3,
                y - 8);
        }

        var positions = new List<PointF>(_points.Count);

        for (var i = 0; i < _points.Count; i++)
        {
            var x = _points.Count == 1
                ? plot.Left + plot.Width / 2f
                : plot.Left +
                  (float)i * plot.Width / (_points.Count - 1);

            var y = plot.Bottom -
                    (float)(_points[i].Value / max) * plot.Height;

            positions.Add(new PointF(x, y));

            var labelSize = graphics.MeasureString(
                _points[i].Label,
                labelFont);

            graphics.DrawString(
                _points[i].Label,
                labelFont,
                mutedBrush,
                x - labelSize.Width / 2,
                plot.Bottom + 7);
        }

        if (positions.Count > 1)
        {
            using var areaPath = new GraphicsPath();

            areaPath.AddLines(positions.ToArray());

            areaPath.AddLine(
                positions[^1].X,
                positions[^1].Y,
                positions[^1].X,
                plot.Bottom);

            areaPath.AddLine(
                positions[^1].X,
                plot.Bottom,
                positions[0].X,
                plot.Bottom);

            areaPath.CloseFigure();

            using var areaBrush = new LinearGradientBrush(
                new Rectangle(
                    plot.Left,
                    plot.Top,
                    plot.Width,
                    plot.Height),
                Color.FromArgb(65, ThemeHelper.Info),
                Color.FromArgb(3, ThemeHelper.Info),
                LinearGradientMode.Vertical);

            graphics.FillPath(
                areaBrush,
                areaPath);

            graphics.DrawLines(
                linePen,
                positions.ToArray());
        }

        foreach (var point in positions)
        {
            graphics.FillEllipse(
                pointBrush,
                point.X - 4.5f,
                point.Y - 4.5f,
                9,
                9);

            graphics.DrawEllipse(
                pointBorder,
                point.X - 4.5f,
                point.Y - 4.5f,
                9,
                9);
        }
    }

    private static string FormatAxisValue(decimal value)
    {
        if (value >= 1_000_000_000m)
            return $"{value / 1_000_000_000m:0.#} tỷ";

        if (value >= 1_000_000m)
            return $"{value / 1_000_000m:0.#} tr";

        if (value >= 1_000m)
            return $"{value / 1_000m:0.#} k";

        return value.ToString("N0");
    }
}
