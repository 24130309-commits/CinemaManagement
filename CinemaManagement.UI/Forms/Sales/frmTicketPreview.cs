
using System.Drawing;
using System.Drawing.Printing;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.UI.Controls;
using CinemaManagement.UI.Helpers;

namespace CinemaManagement.UI.Forms.Sales;

public partial class frmTicketPreview : Form
{
    private readonly SalesOrderContext _order;
    private readonly ITicketPrintService _tickets;
    private readonly TextBox _preview = new();
    private readonly Button _export = new();

    public frmTicketPreview(
        SalesOrderContext order,
        ITicketPrintService tickets)
    {
        InitializeComponent();

        _order = order;
        _tickets = tickets;

        BuildUi();
        LoadTicket();
        ThemeHelper.FitToWorkingArea(this, new Size(700, 760), new Size(560, 520));
    }

    private void BuildUi()
    {
        Text = "Vé điện tử - Xem trước";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(700, 760);
        MinimumSize = new Size(560, 520);
        BackColor = ThemeHelper.Background;
        Font = ThemeHelper.BodyFont;
        Padding = new Padding(18);

        var title = new PageHeader(
            "Vé điện tử",
            "Xem thông tin vé và xuất bản PDF để gửi hoặc in cho khách hàng.");

        _preview.Dock = DockStyle.Fill;
        _preview.Multiline = true;
        _preview.ReadOnly = true;
        _preview.WordWrap = false;
        _preview.ScrollBars = ScrollBars.Both;
        _preview.Font = new Font("Consolas", 10);
        _preview.BackColor = Color.White;
        _preview.ForeColor = ThemeHelper.Text;
        _preview.BorderStyle = BorderStyle.FixedSingle;

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 62,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(4, 10, 4, 4),
            WrapContents = false,
            BackColor = ThemeHelper.Background
        };

        var close = CreateButton("Đóng", ThemeHelper.Muted);
        close.Click += (_, _) => Close();

        var print = CreateButton("In vé", ThemeHelper.Primary);
        print.Click += (_, _) => PrintTicket();

        _export.Text = "Xuất PDF";
        _export.AutoSize = true;
        _export.Height = 36;
        _export.BackColor = ThemeHelper.Accent;
        _export.ForeColor = ThemeHelper.Primary;
        _export.FlatStyle = FlatStyle.Flat;
        _export.UseVisualStyleBackColor = false;
        _export.FlatAppearance.BorderSize = 0;
        _export.Click += (_, _) => ExportPdf();

        footer.Controls.Add(close);
        footer.Controls.Add(print);
        footer.Controls.Add(_export);

        Controls.Add(_preview);
        Controls.Add(footer);
        Controls.Add(title);
    }

    private void LoadTicket()
    {
        if (_order.BookingId <= 0)
        {
            _preview.Text = "Không tìm thấy mã hóa đơn.";
            _export.Enabled = false;
            return;
        }

        try
        {
            _preview.Text = _tickets.GenerateTicketContent(_order.BookingId);
            _preview.SelectionStart = 0;
            _preview.SelectionLength = 0;
        }
        catch (Exception ex)
        {
            _export.Enabled = false;
            MessageHelper.Handle(ex);
        }
    }

    private void ExportPdf()
    {
        if (_order.BookingId <= 0)
            return;

        using var dialog = new SaveFileDialog
        {
            Title = "Lưu vé điện tử",
            Filter = "PDF (*.pdf)|*.pdf",
            FileName = $"Ve-Cinema-{_order.BookingId}.pdf",
            DefaultExt = "pdf",
            AddExtension = true,
            OverwritePrompt = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            _tickets.ExportTicketToPdf(
                _order.BookingId,
                dialog.FileName);

            MessageHelper.Info("Đã xuất vé PDF thành công.");
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);
        }
    }

    private void PrintTicket()
    {
        if (_order.BookingId <= 0 || string.IsNullOrWhiteSpace(_preview.Text))
            return;

        try
        {
            using var document = new PrintDocument
            {
                DocumentName = $"Vé Cinema #{_order.BookingId}"
            };
            document.DefaultPageSettings.Margins = new Margins(32, 32, 32, 32);

            var lines = _preview.Text
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Split('\n');
            var lineIndex = 0;

            document.PrintPage += (_, e) =>
            {
                var graphics = e.Graphics;
                if (graphics is null)
                {
                    e.HasMorePages = false;
                    return;
                }

                using var font = new Font(FontFamily.GenericMonospace, 9f);
                var lineHeight = font.GetHeight(graphics);
                var y = (float)e.MarginBounds.Top;

                while (lineIndex < lines.Length &&
                       y + lineHeight <= e.MarginBounds.Bottom)
                {
                    graphics.DrawString(
                        lines[lineIndex],
                        font,
                        Brushes.Black,
                        e.MarginBounds.Left,
                        y);
                    y += lineHeight;
                    lineIndex++;
                }

                e.HasMorePages = lineIndex < lines.Length;
            };

            using var dialog = new PrintDialog
            {
                Document = document,
                UseEXDialog = true
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                document.Print();
                MessageHelper.Info("Đã gửi vé đến máy in.");
            }
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex, "Không thể in vé.");
        }
    }

    private static Button CreateButton(string text, Color color)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            Height = 36,
            BackColor = color,
            ForeColor = color == ThemeHelper.Accent
                ? ThemeHelper.Primary
                : Color.White,
            FlatStyle = FlatStyle.Flat,
            Padding = new Padding(14, 3, 14, 3),
            Margin = new Padding(6, 0, 6, 0),
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };

        ThemeHelper.StyleActionButton(button, color);
        return button;
    }
}
