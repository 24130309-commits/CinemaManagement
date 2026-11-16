
using CinemaManagement.Core.DTOs;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.UI.Helpers;

namespace CinemaManagement.UI.Forms.Reports;

public partial class frmOccupancyReport : Form
{
    private readonly IReportService _reports;
    private readonly IExportService _export;
    private readonly IAuditService _audit;

    private readonly DateTimePicker _from = new()
    {
        Value = DateTime.Today.AddDays(-7),
        Width = 150,
        Format = DateTimePickerFormat.Short
    };

    private readonly DateTimePicker _to = new()
    {
        Value = DateTime.Today,
        Width = 150,
        Format = DateTimePickerFormat.Short
    };

    private readonly DataGridView _grid = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AutoGenerateColumns = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
    };

    private readonly Label _summary = new()
    {
        Dock = DockStyle.Bottom,
        Height = 48,
        Font = new Font("Segoe UI", 10f, FontStyle.Bold),
        ForeColor = ThemeHelper.Primary,
        TextAlign = ContentAlignment.MiddleLeft,
        Padding = new Padding(16, 0, 0, 0),
        BackColor = ThemeHelper.Surface
    };

    private List<OccupancyDto> _data = [];
    private List<OccupancyReportRow> _reportRows = [];

    public frmOccupancyReport(
        IReportService reports,
        IExportService export,
        IAuditService audit)
    {
        _reports = reports;
        _export = export;
        _audit = audit;

        InitializeComponent();

        Text = "Báo cáo tỷ lệ lấp đầy";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(1000, 620);
        Size = new Size(1150, 700);
        BackColor = ThemeHelper.Background;

        BuildLayout();

        _grid.CellFormatting += Grid_CellFormatting;

        Load += (_, _) => LoadData();

        ThemeHelper.Apply(this);
    }

    private void BuildLayout()
    {
        var heading = new Panel
        {
            Dock = DockStyle.Top,
            Height = 82,
            BackColor = ThemeHelper.Surface,
            Padding = new Padding(22, 10, 22, 8)
        };

        heading.Controls.Add(new Label
        {
            Text = "BÁO CÁO TỶ LỆ LẤP ĐẦY",
            Dock = DockStyle.Top,
            Height = 34,
            Font = ThemeHelper.HeadingFont,
            ForeColor = ThemeHelper.Primary
        });

        heading.Controls.Add(new Label
        {
            Text = "Theo dõi hiệu suất sử dụng ghế theo từng suất chiếu.",
            Dock = DockStyle.Top,
            Height = 25,
            Font = ThemeHelper.SmallFont,
            ForeColor = ThemeHelper.Muted
        });

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 66,
            Padding = new Padding(14, 12, 10, 8),
            WrapContents = false,
            BackColor = ThemeHelper.Surface
        };

        toolbar.Controls.Add(new Label
        {
            Text = "Từ ngày",
            AutoSize = true,
            Padding = new Padding(0, 8, 0, 0)
        });

        toolbar.Controls.Add(_from);

        toolbar.Controls.Add(new Label
        {
            Text = "Đến ngày",
            AutoSize = true,
            Padding = new Padding(0, 8, 0, 0)
        });

        toolbar.Controls.Add(_to);

        var loadButton = new Button
        {
            Text = "Xem báo cáo",
            AutoSize = true,
            Height = 34
        };

        loadButton.Click += (_, _) => LoadData();

        var excelButton = new Button
        {
            Text = "Xuất Excel",
            AutoSize = true,
            Height = 34
        };

        excelButton.Click += (_, _) => Export(true);

        var pdfButton = new Button
        {
            Text = "Xuất PDF",
            AutoSize = true,
            Height = 34
        };

        pdfButton.Click += (_, _) => Export(false);

        toolbar.Controls.Add(loadButton);
        toolbar.Controls.Add(excelButton);
        toolbar.Controls.Add(pdfButton);

        var content = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14),
            BackColor = ThemeHelper.Background
        };

        var gridPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeHelper.Surface,
            Padding = new Padding(8)
        };

        gridPanel.Controls.Add(_grid);
        content.Controls.Add(gridPanel);

        Controls.Add(content);
        Controls.Add(_summary);
        Controls.Add(toolbar);
        Controls.Add(heading);
    }

    private void LoadData()
    {
        try
        {
            if (_from.Value.Date > _to.Value.Date)
            {
                MessageHelper.Warning(
                    "Ngày bắt đầu không được lớn hơn ngày kết thúc.");

                return;
            }

            _data = _reports.GetOccupancy(
                _from.Value.Date,
                _to.Value.Date).ToList();

            _reportRows = _data.Select(x =>
            {
                double rate = x.TotalSeats > 0
                    ? (double)x.SoldSeats / x.TotalSeats * 100
                    : 0;

                string status =
                    x.TotalSeats > 0 &&
                    x.SoldSeats >= x.TotalSeats
                        ? "Cháy vé"
                        : rate < 20
                            ? "Vắng khách"
                            : "Bình thường";

                return new OccupancyReportRow(
                    x.ShowtimeId,
                    x.MovieTitle,
                    x.Date,
                    x.TotalSeats,
                    x.SoldSeats,
                    rate,
                    status
                );
            }).ToList();

            _grid.DataSource = null;
            _grid.DataSource = _reportRows;

            ConfigureColumns();
            UpdateSummary();
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);
        }
    }

    private void ConfigureColumns()
    {
        SetColumn(
            nameof(OccupancyReportRow.ShowtimeId),
            "Mã suất chiếu");

        SetColumn(
            nameof(OccupancyReportRow.MovieTitle),
            "Tên phim");

        SetColumn(
            nameof(OccupancyReportRow.StartTime),
            "Ngày chiếu",
            "dd/MM/yyyy HH:mm");

        SetColumn(
            nameof(OccupancyReportRow.TotalSeats),
            "Tổng số ghế",
            "N0");

        SetColumn(
            nameof(OccupancyReportRow.SoldSeats),
            "Ghế đã bán",
            "N0");

        SetColumn(
            nameof(OccupancyReportRow.OccupancyRate),
            "Tỷ lệ lấp đầy (%)",
            "N1");

        SetColumn(
            nameof(OccupancyReportRow.StatusLabel),
            "Trạng thái");
    }

    private void SetColumn(
        string name,
        string header,
        string? format = null)
    {
        if (!_grid.Columns.Contains(name))
            return;

        var column = _grid.Columns[name]!;

        column.HeaderText = header;

        if (format is not null)
            column.DefaultCellStyle.Format = format;
    }

    private void Grid_CellFormatting(
        object? sender,
        DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 ||
            e.ColumnIndex < 0 ||
            e.ColumnIndex >= _grid.Columns.Count)
        {
            return;
        }

        if (_grid.Columns[e.ColumnIndex].Name !=
            nameof(OccupancyReportRow.StatusLabel))
        {
            return;
        }

        var status = Convert.ToString(e.Value);

        if (status == "Cháy vé")
        {
            e.CellStyle.ForeColor = ThemeHelper.Success;
        }
        else if (status == "Vắng khách")
        {
            e.CellStyle.ForeColor = ThemeHelper.Danger;
        }
        else
        {
            e.CellStyle.ForeColor = ThemeHelper.Muted;
        }

        e.CellStyle.Font =
            new Font("Segoe UI", 9f, FontStyle.Bold);
    }

    private void UpdateSummary()
    {
        if (_reportRows.Count == 0)
        {
            _summary.Text = "  Không có dữ liệu trong khoảng thời gian đã chọn.";
            return;
        }

        int totalSeats = _reportRows.Sum(x => x.TotalSeats);
        int soldSeats = _reportRows.Sum(x => x.SoldSeats);

        double overallRate = totalSeats > 0
            ? (double)soldSeats / totalSeats * 100
            : 0;

        int soldOut = _reportRows.Count(
            x => x.StatusLabel == "Cháy vé");

        _summary.Text =
            $"  Tổng suất: {_reportRows.Count:N0}     " +
            $"Ghế đã bán: {soldSeats:N0}/{totalSeats:N0}     " +
            $"Tỷ lệ lấp đầy chung: {overallRate:N1}%     " +
            $"Suất cháy vé: {soldOut:N0}";
    }

    private void Export(bool excel)
    {
        if (_reportRows.Count == 0)
        {
            MessageHelper.Warning(
                "Không có dữ liệu để xuất.");

            return;
        }

        using var dialog = new SaveFileDialog
        {
            Filter = excel
                ? "Excel (*.xlsx)|*.xlsx"
                : "PDF (*.pdf)|*.pdf",

            FileName =
                $"BaoCaoLapDay_{DateTime.Now:yyyyMMdd}"
        };

        if (dialog.ShowDialog() != DialogResult.OK)
            return;

        try
        {
            if (excel)
            {
                _export.ExportToExcel(
                    _reportRows,
                    dialog.FileName);
            }
            else
            {
                _export.ExportToPdf(
                    _reportRows,
                    dialog.FileName);
            }

            _audit.Log(
                "OCCUPANCY_REPORT_EXPORT",
                "Xuất báo cáo occupancy thành công.");

            MessageHelper.Info(
                "Xuất báo cáo thành công.");
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);
        }
    }

    private sealed record OccupancyReportRow(
        int ShowtimeId,
        string MovieTitle,
        DateTime StartTime,
        int TotalSeats,
        int SoldSeats,
        double OccupancyRate,
        string StatusLabel);
}
