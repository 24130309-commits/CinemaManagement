
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Core.DTOs;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.UI.Helpers;

namespace CinemaManagement.UI.Forms.Reports;

public partial class frmRevenueReport : Form
{
    private readonly IReportService _reports;
    private readonly IExportService _export;
    private readonly IAuditService _audit;

    private readonly DateTimePicker _from = new()
    {
        Value = DateTime.Today.AddDays(-30),
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

    private List<RevenueByMovieDto> _data = [];

    public frmRevenueReport(
        IReportService reports,
        IExportService export,
        IAuditService audit)
    {
        _reports = reports ?? throw new ArgumentNullException(nameof(reports));
        _export = export ?? throw new ArgumentNullException(nameof(export));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));

        InitializeComponent();

        Text = "Báo cáo doanh thu";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(950, 580);
        Size = new Size(1100, 680);
        BackColor = ThemeHelper.Background;

        BuildLayout();

        Load += (_, _) => LoadData();

        ThemeHelper.Apply(this);
    }

    private void BuildLayout()
    {
        var heading = new Panel
        {
            Dock = DockStyle.Top,
            Height = 76,
            Padding = new Padding(18, 12, 12, 8),
            BackColor = ThemeHelper.Surface
        };

        heading.Controls.Add(new Label
        {
            Text = "BÁO CÁO DOANH THU",
            Dock = DockStyle.Top,
            Height = 36,
            Font = ThemeHelper.HeadingFont,
            ForeColor = ThemeHelper.Primary
        });

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 66,
            Padding = new Padding(14, 12, 8, 8),
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

        var load = new Button
        {
            Text = "Xem báo cáo",
            AutoSize = true,
            Height = 34
        };

        load.Click += (_, _) => LoadData();

        var export = new Button
        {
            Text = "Xuất CSV",
            AutoSize = true,
            Height = 34
        };

        export.Click += (_, _) => Export();

        toolbar.Controls.Add(load);
        toolbar.Controls.Add(export);

        var content = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14),
            BackColor = ThemeHelper.Background
        };

        var gridPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(8),
            BackColor = ThemeHelper.Surface
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

            _data = _reports.GetRevenueByMovie(
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
        SetColumn(
            nameof(RevenueByMovieDto.MovieId),
            "Mã phim",
            false);

        SetColumn(
            nameof(RevenueByMovieDto.MovieTitle),
            "Tên phim");

        SetColumn(
            nameof(RevenueByMovieDto.TotalShowtimes),
            "Số suất chiếu",
            true,
            "N0");

        SetColumn(
            nameof(RevenueByMovieDto.TotalTicketsSold),
            "Tổng vé bán",
            true,
            "N0");

        SetColumn(
            nameof(RevenueByMovieDto.TicketCount),
            "Số vé",
            true,
            "N0");

        SetColumn(
            nameof(RevenueByMovieDto.TicketRevenue),
            "Doanh thu vé",
            true,
            "N0");

        SetColumn(
            nameof(RevenueByMovieDto.ProductRevenue),
            "Doanh thu sản phẩm",
            true,
            "N0");

        SetColumn(
            nameof(RevenueByMovieDto.TotalRevenue),
            "Tổng doanh thu",
            true,
            "N0");
    }

    private void SetColumn(
        string name,
        string header,
        bool visible = true,
        string? format = null)
    {
        if (!_grid.Columns.Contains(name))
            return;

        var column = _grid.Columns[name]!;

        column.HeaderText = header;
        column.Visible = visible;

        if (format is not null)
            column.DefaultCellStyle.Format = format;
    }

    private void UpdateSummary()
    {
        if (_data.Count == 0)
        {
            _summary.Text = "  Không có dữ liệu trong khoảng thời gian đã chọn.";
            return;
        }

        decimal totalTicketRevenue = _data.Sum(x => x.TicketRevenue);
        decimal totalProductRevenue = _data.Sum(x => x.ProductRevenue);
        decimal totalRevenue = _data.Sum(x => x.TotalRevenue);
        int totalTickets = _data.Sum(x => x.TotalTicketsSold);

        _summary.Text =
            $"  Số phim: {_data.Count:N0}     " +
            $"Vé bán: {totalTickets:N0}     " +
            $"Doanh thu vé: {totalTicketRevenue:N0}     " +
            $"Doanh thu sản phẩm: {totalProductRevenue:N0}     " +
            $"Tổng: {totalRevenue:N0} VNĐ";
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
            FileName = $"BaoCaoDoanhThu_{DateTime.Now:yyyyMMdd}.csv"
        };

        if (dialog.ShowDialog() != DialogResult.OK)
            return;

        try
        {
            _export.ExportToCsv(_data, dialog.FileName);

            _audit.Log(
                "REVENUE_REPORT_EXPORT",
                "Xuất báo cáo doanh thu dạng CSV.");

            MessageHelper.Info("Xuất báo cáo thành công.");
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);
        }
    }
}
