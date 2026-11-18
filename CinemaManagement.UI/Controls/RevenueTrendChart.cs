
using System.ComponentModel;
using System.Drawing.Drawing2D;
using CinemaManagement.UI.Helpers;

namespace CinemaManagement.UI.Controls;

/// <summary>
/// Biểu đồ đường doanh thu 7 ngày.
/// Chỉ hiển thị dữ liệu do ReportService cung cấp.
/// </summary>
public sealed class RevenueTrendChart : Control
{
    private IReadOnlyList<(string Label, decimal Value)> _points = [];

    // Dữ liệu được thiết lập bằng code, không lưu trong WinForms Designer.
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

    // Tiêu đề được thiết lập bằng code, không lưu trong WinForms Designer.
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string ChartTitle { get; set; } = "DOANH THU 7 NGÀY GẦN NHẤT";

    public RevenueTrendChart()
    {
        DoubleBuffered = true;
        BackColor = ThemeHelper.Surface;
        ForeColor = ThemeHelper.Primary;
        MinimumSize = new Size(300, 210);
        Margin = new Padding(8);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        using var titleFont = new Font("Segoe UI", 10f, FontStyle.Bold);
        using var labelFont = new Font("Segoe UI", 8f);
        using var titleBrush = new SolidBrush(ThemeHelper.Primary);
        using var mutedBrush = new SolidBrush(ThemeHelper.Muted);
        using var gridPen = new Pen(ThemeHelper.Border, 1);
        using var linePen = new Pen(ThemeHelper.Accent, 3)
        {
            LineJoin = LineJoin.Round
        };
        using var pointBrush = new SolidBrush(ThemeHelper.Accent);

        graphics.DrawString(
            ChartTitle,
            titleFont,
            titleBrush,
            16,
            14
        );

        if (_points.Count == 0)
        {
            graphics.DrawString(
                "Chưa có dữ liệu doanh thu trong 7 ngày gần nhất.",
                labelFont,
                mutedBrush,
                18,
                62
            );

            return;
        }

        var plot = new Rectangle(
            66,
            52,
            Math.Max(120, Width - 88),
            Math.Max(85, Height - 100)
        );

        var max = Math.Max(1m, _points.Max(point => point.Value));

        for (var i = 0; i < 4; i++)
        {
            var y = plot.Top + plot.Height * i / 3;

            graphics.DrawLine(
                gridPen,
                plot.Left,
                y,
                plot.Right,
                y
            );

            var amount = max * (3 - i) / 3;

            graphics.DrawString(
                amount.ToString("N0"),
                labelFont,
                mutedBrush,
                4,
                y - 8
            );
        }

        var positions = new List<PointF>();

        for (var i = 0; i < _points.Count; i++)
        {
            var x = _points.Count == 1
                ? plot.Left + plot.Width / 2f
                : plot.Left + (float)i * plot.Width / (_points.Count - 1);

            var y = plot.Bottom -
                    (float)(_points[i].Value / max) * plot.Height;

            positions.Add(new PointF(x, y));

            var labelSize = graphics.MeasureString(
                _points[i].Label,
                labelFont
            );

            graphics.DrawString(
                _points[i].Label,
                labelFont,
                mutedBrush,
                x - labelSize.Width / 2,
                plot.Bottom + 8
            );
        }

        if (positions.Count > 1)
        {
            graphics.DrawLines(
                linePen,
                positions.ToArray()
            );
        }

        foreach (var point in positions)
        {
            graphics.FillEllipse(
                pointBrush,
                point.X - 4,
                point.Y - 4,
                8,
                8
            );
        }
    }
}
