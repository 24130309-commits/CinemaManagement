
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
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
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false,
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

    public frmOccupancyReport(
        IReportService reports,
        IExportService export,
        IAuditService audit)
    {
        _reports = reports ?? throw new ArgumentNullException(nameof(reports));
        _export = export ?? throw new ArgumentNullException(nameof(export));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));

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

        var exportButton = new Button
        {
            Text = "Xuất CSV",
            AutoSize = true,
            Height = 34
        };

        exportButton.Click += (_, _) => Export();

        toolbar.Controls.Add(loadButton);
        toolbar.Controls.Add(exportButton);

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

            _data = _reports.GetOccupancyReport(
                _from.Value.Date,
                _to.Value.Date.AddDays(1).AddTicks(-1)).ToList();

            _grid.DataSource = null;
            _grid.DataSource = _data;

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
        SetColumn(nameof(OccupancyDto.ShowtimeId), "Mã suất chiếu");
        SetColumn(nameof(OccupancyDto.MovieTitle), "Tên phim");
        SetColumn(nameof(OccupancyDto.Date), "Ngày chiếu", "dd/MM/yyyy");
        SetColumn(nameof(OccupancyDto.StartTime), "Giờ bắt đầu");
        SetColumn(nameof(OccupancyDto.TotalSeats), "Tổng số ghế", "N0");
        SetColumn(nameof(OccupancyDto.SoldSeats), "Ghế đã bán", "N0");
        SetColumn(nameof(OccupancyDto.OccupancyRate), "Tỷ lệ lấp đầy (%)", "N1");
        SetColumn(nameof(OccupancyDto.TotalPrice), "Doanh thu", "N0");

        if (!_grid.Columns.Contains("StatusLabel"))
        {
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "StatusLabel",
                HeaderText = "Trạng thái",
                ReadOnly = true
            });
        }

        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (row.DataBoundItem is OccupancyDto item)
            {
                row.Cells["StatusLabel"].Value = GetStatus(item);
            }
        }
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

    private static string GetStatus(OccupancyDto item)
    {
        if (item.TotalSeats > 0 &&
            item.SoldSeats >= item.TotalSeats)
        {
            return "Cháy vé";
        }

        return item.OccupancyRate < 20
            ? "Vắng khách"
            : "Bình thường";
    }

    private void Grid_CellFormatting(
        object? sender,
        DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 ||
            e.ColumnIndex < 0 ||
            _grid.Columns[e.ColumnIndex].Name != "StatusLabel")
        {
            return;
        }

        var status = Convert.ToString(e.Value);

        e.CellStyle.ForeColor = status switch
        {
            "Cháy vé" => ThemeHelper.Success,
            "Vắng khách" => ThemeHelper.Danger,
            _ => ThemeHelper.Muted
        };

        e.CellStyle.Font = new Font(
            "Segoe UI",
            9f,
            FontStyle.Bold);
    }

    private void UpdateSummary()
    {
        if (_data.Count == 0)
        {
            _summary.Text =
                "  Không có dữ liệu trong khoảng thời gian đã chọn.";

            return;
        }

        int totalSeats = _data.Sum(x => x.TotalSeats);
        int soldSeats = _data.Sum(x => x.SoldSeats);

        double overallRate = totalSeats > 0
            ? (double)soldSeats / totalSeats * 100
            : 0;

        int soldOut = _data.Count(
            x => x.TotalSeats > 0 &&
                 x.SoldSeats >= x.TotalSeats);

        _summary.Text =
            $"  Tổng suất: {_data.Count:N0}     " +
            $"Ghế đã bán: {soldSeats:N0}/{totalSeats:N0}     " +
            $"Tỷ lệ lấp đầy chung: {overallRate:N1}%     " +
            $"Suất cháy vé: {soldOut:N0}";
    }

    private void Export()
    {
        if (_data.Count == 0)
        {
            MessageHelper.Warning("Không có dữ liệu để xuất.");
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Filter = "CSV (*.csv)|*.csv",
            FileName = $"BaoCaoLapDay_{DateTime.Now:yyyyMMdd}.csv"
        };

        if (dialog.ShowDialog() != DialogResult.OK)
            return;

        try
        {
            _export.ExportToCsv(_data, dialog.FileName);

            _audit.Log(
                "OCCUPANCY_REPORT_EXPORT",
                "Xuất báo cáo tỷ lệ lấp đầy dạng CSV.");

            MessageHelper.Info("Xuất báo cáo thành công.");
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);
        }
    }
}
