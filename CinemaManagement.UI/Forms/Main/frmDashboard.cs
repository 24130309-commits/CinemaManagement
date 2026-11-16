﻿using System.Collections;
using System.Globalization;
using CinemaManagement.Core.DTOs;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.UI.Controls;
using CinemaManagement.UI.Helpers;

namespace CinemaManagement.UI.Forms.Main;

public partial class frmDashboard : Form
{
    private readonly IReportService _reports;

    private readonly FlowLayoutPanel _cards = new()
    {
        Dock = DockStyle.Fill,
        Padding = new Padding(4),
        WrapContents = false,
        AutoScroll = true,
        BackColor = ThemeHelper.Background
    };

    private readonly RevenueTrendChart _chart = new()
    {
        Dock = DockStyle.Fill
    };

    private readonly DataGridView _topMovies = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AutoGenerateColumns = true
    };

    private readonly DataGridView _upcoming = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AutoGenerateColumns = true
    };

    public frmDashboard(IReportService reports)
    {
        _reports = reports;

        InitializeComponent();

        Text = "Bảng điều khiển";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(1050, 680);
        Size = new Size(1220, 790);

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
            BackColor = ThemeHelper.Surface,
            Padding = new Padding(24, 12, 24, 8)
        };

        heading.Controls.Add(new Label
        {
            Text = "Tổng quan hoạt động",
            Dock = DockStyle.Top,
            Height = 32,
            Font = ThemeHelper.HeadingFont,
            ForeColor = ThemeHelper.Primary
        });

        heading.Controls.Add(new Label
        {
            Text = "Theo dõi doanh thu, phim nổi bật và lịch chiếu sắp tới của rạp.",
            Dock = DockStyle.Top,
            Height = 24,
            Font = ThemeHelper.SmallFont,
            ForeColor = ThemeHelper.Muted
        });

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Padding = new Padding(16),
            BackColor = ThemeHelper.Background
        };

        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46));

        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 132));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 53));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 47));

        root.Controls.Add(_cards, 0, 0);
        root.SetColumnSpan(_cards, 2);

        root.Controls.Add(
            CreateSection("BIỂU ĐỒ DOANH THU", _chart),
            0,
            1);

        root.Controls.Add(
            CreateSection("TOP PHIM THEO DOANH THU", _topMovies),
            1,
            1);

        root.Controls.Add(
            CreateSection("CÁC SUẤT CHIẾU SẮP TỚI", _upcoming),
            0,
            2);

        root.SetColumnSpan(
            root.GetControlFromPosition(0, 2)!,
            2);

        Controls.Add(root);
        Controls.Add(heading);

        heading.BringToFront();
    }

    private static Panel CreateSection(
        string title,
        Control content)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeHelper.Surface,
            Padding = new Padding(12),
            Margin = new Padding(8)
        };

        var caption = new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            Height = 32,
            Font = ThemeHelper.SectionFont,
            ForeColor = ThemeHelper.Primary,
            TextAlign = ContentAlignment.MiddleLeft
        };

        content.Dock = DockStyle.Fill;

        panel.Controls.Add(content);
        panel.Controls.Add(caption);

        return panel;
    }

    private void LoadData()
    {
        try
        {
            dynamic stats = _reports.GetDashboardStats();

            _cards.Controls.Clear();

            _cards.Controls.Add(new StatCard(
                "Vé bán hôm nay",
                Convert.ToString(stats.TicketsToday) ?? "0",
                "▣"));

            _cards.Controls.Add(new StatCard(
                "Doanh thu hôm nay",
                string.Format("{0:N0} đ", stats.RevenueToday),
                "₫"));

            _cards.Controls.Add(new StatCard(
                "Doanh thu 7 ngày",
                string.Format("{0:N0} đ", stats.RevenueLast7Days),
                "↗"));

            _cards.Controls.Add(new StatCard(
                "Doanh thu 30 ngày",
                string.Format("{0:N0} đ", stats.RevenueLast30Days),
                "▤"));

            _topMovies.DataSource =
                ((IEnumerable<RevenueByMovieDto>)stats.TopMovies).ToList();

            ConfigureTopMovieColumns();

            var trend = new List<(string Label, decimal Value)>();

            foreach (var row in (IEnumerable)stats.RevenueTrend)
            {
                if (row is not IDictionary<string, object> values)
                    continue;

                var rawDate = Convert.ToString(
                    values["RevenueDate"],
                    CultureInfo.InvariantCulture) ?? string.Empty;

                var amount = Convert.ToDecimal(
                    values["Revenue"],
                    CultureInfo.InvariantCulture);

                var label = DateTime.TryParse(rawDate, out var date)
                    ? date.ToString("dd/MM")
                    : rawDate;

                trend.Add((label, amount));
            }

            _chart.Points = trend;

            var upcomingRows = new List<UpcomingShowtimeRow>();

            foreach (var row in (IEnumerable)stats.UpcomingShowtimes)
            {
                if (row is IDictionary<string, object> values)
                {
                    upcomingRows.Add(new UpcomingShowtimeRow(
                        Convert.ToInt32(values["ShowtimeId"]),
                        Convert.ToString(values["MovieTitle"]) ?? string.Empty,
                        Convert.ToString(values["RoomName"]) ?? string.Empty,
                        DateTime.Parse(
                            Convert.ToString(values["StartTime"])
                            ?? DateTime.Now.ToString("O")),
                        Convert.ToInt32(values["SeatsLeft"])));
                }
            }

            _upcoming.DataSource = upcomingRows;

            ConfigureUpcomingColumns();
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);
        }
    }

    private void ConfigureTopMovieColumns()
    {
        SetColumn(
            _topMovies,
            nameof(RevenueByMovieDto.MovieId),
            "Mã phim",
            false);

        SetColumn(
            _topMovies,
            nameof(RevenueByMovieDto.MovieTitle),
            "Tên phim");

        SetColumn(
            _topMovies,
            nameof(RevenueByMovieDto.TicketCount),
            "Số vé");

        SetColumn(
            _topMovies,
            nameof(RevenueByMovieDto.TicketRevenue),
            "Doanh thu vé",
            format: "N0");

        SetColumn(
            _topMovies,
            nameof(RevenueByMovieDto.ProductRevenue),
            "Doanh thu bắp nước",
            format: "N0");

        SetColumn(
            _topMovies,
            nameof(RevenueByMovieDto.TotalRevenue),
            "Tổng doanh thu",
            format: "N0");
    }

    private void ConfigureUpcomingColumns()
    {
        SetColumn(
            _upcoming,
            nameof(UpcomingShowtimeRow.ShowtimeId),
            "Mã suất",
            false);

        SetColumn(
            _upcoming,
            nameof(UpcomingShowtimeRow.MovieTitle),
            "Tên phim");

        SetColumn(
            _upcoming,
            nameof(UpcomingShowtimeRow.RoomName),
            "Phòng");

        SetColumn(
            _upcoming,
            nameof(UpcomingShowtimeRow.StartTime),
            "Thời gian",
            format: "dd/MM HH:mm");

        SetColumn(
            _upcoming,
            nameof(UpcomingShowtimeRow.SeatsLeft),
            "Ghế còn");
    }

    private static void SetColumn(
        DataGridView grid,
        string name,
        string header,
        bool visible = true,
        string? format = null)
    {
        if (!grid.Columns.Contains(name))
            return;

        var column = grid.Columns[name]!;

        column.HeaderText = header;
        column.Visible = visible;

        if (format is not null)
            column.DefaultCellStyle.Format = format;
    }

    private sealed record UpcomingShowtimeRow(
        int ShowtimeId,
        string MovieTitle,
        string RoomName,
        DateTime StartTime,
        int SeatsLeft);
}