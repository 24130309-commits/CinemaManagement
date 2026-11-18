﻿
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
        Width = 150
    };

    private readonly DateTimePicker _to = new()
    {
        Value = DateTime.Today,
        Width = 150
    };

    private readonly DataGridView _grid = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AutoGenerateColumns = true,
        AllowUserToAddRows = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
    };

    private List<RevenueByMovieDto> _data = [];

    public frmRevenueReport(
        IReportService reports,
        IExportService export,
        IAuditService audit)
    {
        _reports = reports;
        _export = export;
        _audit = audit;

        InitializeComponent();

        Text = "Báo cáo doanh thu";
        Size = new Size(1100, 680);

        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 76,
            Padding = new Padding(14, 8, 8, 8),
            WrapContents = false,
            BackColor = ThemeHelper.Surface
        };

        bar.Controls.Add(new Label
        {
            Text = "BÁO CÁO DOANH THU",
            AutoSize = true,
            Font = ThemeHelper.SectionFont,
            ForeColor = ThemeHelper.Primary,
            Padding = new Padding(0, 8, 16, 0)
        });

        bar.Controls.Add(new Label
        {
            Text = "Từ ngày",
            AutoSize = true,
            Padding = new Padding(0, 8, 0, 0)
        });

        bar.Controls.Add(_from);

        bar.Controls.Add(new Label
        {
            Text = "Đến ngày",
            AutoSize = true,
            Padding = new Padding(0, 8, 0, 0)
        });

        bar.Controls.Add(_to);

        var load = new Button
        {
            Text = "Xem báo cáo",
            AutoSize = true
        };

        load.Click += (_, _) => LoadData();

        bar.Controls.Add(load);

        var excel = new Button
        {
            Text = "Xuất Excel",
            AutoSize = true
        };

        excel.Click += (_, _) => Export(true);

        bar.Controls.Add(excel);

        var pdf = new Button
        {
            Text = "Xuất PDF",
            AutoSize = true
        };

        pdf.Click += (_, _) => Export(false);

        bar.Controls.Add(pdf);

        Controls.Add(_grid);
        Controls.Add(bar);

        Load += (_, _) => LoadData();

        ThemeHelper.Apply(this);
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
                _to.Value.Date).ToList();

            _grid.DataSource = null;
            _grid.DataSource = _data;

            SetColumn(
                nameof(RevenueByMovieDto.MovieId),
                "Mã phim",
                false);

            SetColumn(
                nameof(RevenueByMovieDto.MovieTitle),
                "Tên phim");

            SetColumn(
                nameof(RevenueByMovieDto.TicketCount),
                "Số vé");

            SetColumn(
                nameof(RevenueByMovieDto.TicketRevenue),
                "Doanh thu vé",
                "N0");

            SetColumn(
                nameof(RevenueByMovieDto.ProductRevenue),
                "Doanh thu sản phẩm",
                "N0");

            SetColumn(
                nameof(RevenueByMovieDto.TotalRevenue),
                "Tổng doanh thu",
                "N0");
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);
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

    private void SetColumn(
        string name,
        string header,
        bool visible)
    {
        if (!_grid.Columns.Contains(name))
            return;

        _grid.Columns[name]!.HeaderText = header;
        _grid.Columns[name]!.Visible = visible;
    }

    private void Export(bool excel)
    {
        if (_data.Count == 0)
        {
            MessageHelper.Warning("Không có dữ liệu để xuất.");
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Filter = excel
                ? "Excel (*.xlsx)|*.xlsx"
                : "PDF (*.pdf)|*.pdf",

            FileName = $"BaoCaoDoanhThu_{DateTime.Now:yyyyMMdd}"
        };

        if (dialog.ShowDialog() != DialogResult.OK)
            return;

        try
        {
            if (excel)
                _export.ExportToExcel(_data, dialog.FileName);
            else
                _export.ExportToPdf(_data, dialog.FileName);

            _audit.Log(
                "REVENUE_REPORT_EXPORT",
                "Xuất báo cáo revenue thành công.");

            MessageHelper.Info("Xuất báo cáo thành công.");
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);
        }
    }
}