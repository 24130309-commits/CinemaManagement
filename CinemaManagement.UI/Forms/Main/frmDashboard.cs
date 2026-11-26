using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Core.DTOs;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.UI.Controls;
using CinemaManagement.UI.Helpers;

namespace CinemaManagement.UI.Forms.Main;

public partial class frmDashboard : Form
{
    private readonly IReportService _reports;
    private readonly IAuditService _auditService;

    private readonly TableLayoutPanel _cards = new()
    {
        Dock = DockStyle.Fill,
        ColumnCount = 4,
        RowCount = 1,
        BackColor = ThemeHelper.Background,
        Margin = Padding.Empty,
        Padding = Padding.Empty
    };

    private readonly StatCard _ticketsCard =
        new("Vé bán hôm nay", "0", "▣")
        { AccentColor = ThemeHelper.Info };

    private readonly StatCard _todayRevenueCard =
        new("Doanh thu hôm nay", "0 đ", "₫")
        { AccentColor = ThemeHelper.Accent };

    private readonly StatCard _weekRevenueCard =
        new("Doanh thu 7 ngày", "0 đ", "↗")
        { AccentColor = ThemeHelper.Accent };

    private readonly StatCard _monthRevenueCard =
        new("Doanh thu 30 ngày", "0 đ", "▤")
        { AccentColor = ThemeHelper.Accent };

    private readonly RevenueTrendChart _chart = new()
    {
        Dock = DockStyle.Fill
    };

    private readonly EmptyStateDataGridView _topMovies = CreateGrid(
        "Doanh thu phim sẽ xuất hiện tại đây khi có giao dịch.");
    private readonly EmptyStateDataGridView _upcoming = CreateGrid(
        "Hiện chưa có suất chiếu sắp tới.");
    private readonly EmptyStateDataGridView _recentLogs = CreateGrid(
        "Chưa có hoạt động nào được ghi nhận.");

    private Panel? _trendSection;
    private Panel? _topMoviesSection;
    private Panel? _upcomingSection;
    private Panel? _recentLogsSection;
    private Panel? _hero;
    private bool? _isCompactLayout;
    private bool? _isNarrowLayout;

    public frmDashboard(
        IReportService reports,
        IAuditService auditService)
    {
        ThemeHelper.ConfigureDpiScaling(this);
        _reports = reports
            ?? throw new ArgumentNullException(nameof(reports));

        _auditService = auditService
            ?? throw new ArgumentNullException(nameof(auditService));

        Text = "Bảng điều khiển";
        StartPosition = FormStartPosition.CenterParent;
        // This form is hosted inside the main workspace; keep the root
        // responsive when the application window is restored or narrow.
        MinimumSize = System.Drawing.Size.Empty;
        Size = new System.Drawing.Size(1220, 900);
        BackColor = ThemeHelper.Background;

        BuildLayout();

        Load += (_, _) => LoadData();

        ThemeHelper.Apply(this);
        ThemeHelper.ApplyDefaultGridStyle(_topMovies);
        ThemeHelper.ApplyDefaultGridStyle(_upcoming);
        ThemeHelper.ApplyDefaultGridStyle(_recentLogs);
    }

    private static EmptyStateDataGridView CreateGrid(string emptyStateText)
    {
        return new EmptyStateDataGridView
        {
            EmptyStateText = emptyStateText,
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AutoGenerateColumns = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false,
            BackgroundColor = ThemeHelper.Surface,
            BorderStyle = BorderStyle.None
        };
    }

    private void BuildLayout()
    {
        var heading = new PageHeader(
            "Hoạt động rạp",
            "Theo dõi doanh thu, phim nổi bật và lịch chiếu sắp tới của rạp.");

        var refresh = new Button
        {
            Text = "Làm mới",
            AutoSize = true,
            Height = 34,
            AccessibleName = "Làm mới dữ liệu tổng quan"
        };
        refresh.Click += (_, _) => LoadData();
        heading.Actions.Add(refresh);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(12, 10, 12, 12),
            BackColor = ThemeHelper.Background,
            Margin = Padding.Empty
        };

        _hero = CreateHero();
        root.Controls.Add(_hero, 0, 0);
        root.SetColumnSpan(_hero, 2);

        AddStatCard(_ticketsCard, 0);
        AddStatCard(_todayRevenueCard, 1);
        AddStatCard(_weekRevenueCard, 2);
        AddStatCard(_monthRevenueCard, 3);

        root.Controls.Add(_cards, 0, 1);
        root.SetColumnSpan(_cards, 2);

        _trendSection = CreateSection(
            "BIỂU ĐỒ DOANH THU",
            _chart);

        _topMoviesSection = CreateSection(
            "TOP PHIM THEO DOANH THU",
            _topMovies);

        _upcomingSection = CreateSection(
            "CÁC SUẤT CHIẾU SẮP TỚI",
            _upcoming);

        _recentLogsSection = CreateSection(
            "NHẬT KÝ HOẠT ĐỘNG GẦN ĐÂY",
            _recentLogs);

        root.Controls.Add(_trendSection, 0, 2);
        root.Controls.Add(_topMoviesSection, 1, 2);
        root.Controls.Add(_upcomingSection, 0, 3);
        root.Controls.Add(_recentLogsSection, 1, 3);

        root.Resize += (_, _) => ApplyResponsiveLayout(root);
        ApplyResponsiveLayout(root);

        Controls.Add(root);
        Controls.Add(heading);
    }

    private static Panel CreateHero()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeHelper.Structural,
            Margin = new Padding(6, 4, 6, 8),
            Padding = new Padding(20, 12, 18, 10),
            AccessibleName = "Tổng quan vận hành hôm nay"
        };
        var kicker = new Label
        {
            Text = "VẬN HÀNH RẠP",
            Location = new Point(20, 12),
            Size = new Size(320, 22),
            ForeColor = Color.FromArgb(225, 151, 121),
            Font = new Font("Segoe UI", 8f, FontStyle.Bold)
        };
        var title = new Label
        {
            Text = "Vận hành hôm nay",
            Location = new Point(18, 34),
            Size = new Size(520, 40),
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 18f, FontStyle.Bold),
            AutoEllipsis = false,
            TextAlign = ContentAlignment.MiddleLeft
        };
        var context = new Label
        {
            Text = $"{DateTime.Today.ToString("dddd, dd MMMM yyyy", CultureInfo.GetCultureInfo("vi-VN"))}  •  Dữ liệu đã ghi nhận",
            Location = new Point(20, 78),
            Size = new Size(740, 38),
            ForeColor = Color.FromArgb(198, 210, 226),
            Font = ThemeHelper.SmallFont,
            AutoEllipsis = false,
            TextAlign = ContentAlignment.TopLeft
        };
        var accent = new Panel
        {
            Dock = DockStyle.Right,
            Width = 5,
            BackColor = ThemeHelper.Accent
        };
        panel.Controls.AddRange([kicker, title, context, accent]);
        void ResizeText()
        {
            var width = Math.Max(80, panel.ClientSize.Width - 44);
            title.Width = width;
            context.Width = width;
            title.Height = TextRenderer.MeasureText(title.Text, title.Font).Height + 10;
            title.Top = kicker.Bottom;
            context.Top = title.Bottom + 2;
            context.Height = Math.Max(
                30,
                TextRenderer.MeasureText(
                    context.Text,
                    context.Font,
                    new Size(width, int.MaxValue),
                    TextFormatFlags.WordBreak | TextFormatFlags.NoPadding).Height + 8);
        }

        panel.Resize += (_, _) => ResizeText();
        ResizeText();
        return panel;
    }

    private void ApplyResponsiveLayout(TableLayoutPanel root)
    {
        var compact = root.ClientSize.Width < 1020;
        var narrow = compact && root.ClientSize.Width < 700;
        if (_isCompactLayout == compact && _isNarrowLayout == narrow)
            return;

        _isCompactLayout = compact;
        _isNarrowLayout = narrow;
        root.SuspendLayout();
        _cards.SuspendLayout();

        root.ColumnStyles.Clear();
        root.RowStyles.Clear();
        _cards.ColumnStyles.Clear();
        _cards.RowStyles.Clear();

        if (compact)
        {
            root.ColumnCount = 1;
            root.RowCount = 6;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            foreach (var height in narrow
                         ? new[] { 130, 430, 250, 240, 220, 220 }
                         : new[] { 130, 220, 250, 240, 220, 220 })
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, height));

            _cards.ColumnCount = narrow ? 1 : 2;
            _cards.RowCount = narrow ? 4 : 2;
            _cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, narrow ? 100 : 50));
            if (!narrow)
                _cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            for (var row = 0; row < _cards.RowCount; row++)
                _cards.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / _cards.RowCount));

            var statCards = new[]
            {
                _ticketsCard,
                _todayRevenueCard,
                _weekRevenueCard,
                _monthRevenueCard
            };

            for (var index = 0; index < statCards.Length; index++)
            {
                _cards.SetCellPosition(
                    statCards[index],
                    narrow
                        ? new TableLayoutPanelCellPosition(0, index)
                        : new TableLayoutPanelCellPosition(index % 2, index / 2));
            }

            root.SetCellPosition(_hero!, new TableLayoutPanelCellPosition(0, 0));
            root.SetColumnSpan(_hero!, 1);
            root.SetCellPosition(_cards, new TableLayoutPanelCellPosition(0, 1));
            root.SetColumnSpan(_cards, 1);
            root.SetCellPosition(_trendSection!, new TableLayoutPanelCellPosition(0, 2));
            root.SetCellPosition(_topMoviesSection!, new TableLayoutPanelCellPosition(0, 3));
            root.SetCellPosition(_upcomingSection!, new TableLayoutPanelCellPosition(0, 4));
            root.SetColumnSpan(_upcomingSection!, 1);
            root.SetCellPosition(_recentLogsSection!, new TableLayoutPanelCellPosition(0, 5));
            root.SetColumnSpan(_recentLogsSection!, 1);
        }
        else
        {
            root.ColumnCount = 2;
            root.RowCount = 4;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 130));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 130));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 250));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 220));

            _cards.ColumnCount = 4;
            _cards.RowCount = 1;
            for (var index = 0; index < 4; index++)
                _cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            _cards.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var statCards = new[]
            {
                _ticketsCard,
                _todayRevenueCard,
                _weekRevenueCard,
                _monthRevenueCard
            };

            for (var index = 0; index < statCards.Length; index++)
            {
                _cards.SetCellPosition(
                    statCards[index],
                    new TableLayoutPanelCellPosition(index, 0));
            }

            root.SetCellPosition(_hero!, new TableLayoutPanelCellPosition(0, 0));
            root.SetColumnSpan(_hero!, 2);
            root.SetCellPosition(_cards, new TableLayoutPanelCellPosition(0, 1));
            root.SetColumnSpan(_cards, 2);
            root.SetCellPosition(_trendSection!, new TableLayoutPanelCellPosition(0, 2));
            root.SetCellPosition(_topMoviesSection!, new TableLayoutPanelCellPosition(1, 2));
            root.SetCellPosition(_upcomingSection!, new TableLayoutPanelCellPosition(0, 3));
            root.SetColumnSpan(_upcomingSection!, 1);
            root.SetCellPosition(_recentLogsSection!, new TableLayoutPanelCellPosition(1, 3));
            root.SetColumnSpan(_recentLogsSection!, 1);
        }

        _cards.ResumeLayout(true);
        root.ResumeLayout(true);
    }

    private void AddStatCard(StatCard card, int column)
    {
        card.Dock = DockStyle.Fill;
        card.Margin = new Padding(6, 4, 6, 8);

        _cards.Controls.Add(card, column, 0);
    }

    private static Panel CreateSection(
        string title,
        Control content)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeHelper.SurfaceRaised,
            Padding = new Padding(13),
            Margin = new Padding(6)
        };

        var caption = new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            Height = 36,
            Font = ThemeHelper.SectionFont,
            ForeColor = ThemeHelper.Primary,
            TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
            BackColor = ThemeHelper.SurfaceAlt,
            Padding = new Padding(10, 0, 4, 0)
        };

        var divider = new Panel
        {
            Dock = DockStyle.Top,
            Height = 1,
            BackColor = ThemeHelper.Accent
        };

        content.Dock = DockStyle.Fill;
        content.Margin = new Padding(0, 8, 0, 0);

        panel.Controls.Add(content);
        panel.Controls.Add(divider);
        panel.Controls.Add(caption);

        panel.Paint += (_, e) =>
        {
            using var pen = new System.Drawing.Pen(ThemeHelper.Border);

            e.Graphics.DrawRectangle(
                pen,
                0,
                0,
                panel.Width - 1,
                panel.Height - 1);
        };

        return panel;
    }

    private void LoadData()
    {
        _chart.EmptyMessage = "Chưa có dữ liệu doanh thu trong 7 ngày gần nhất.";
        _topMovies.EmptyStateText = "Doanh thu phim sẽ xuất hiện tại đây khi có giao dịch.";
        _upcoming.EmptyStateText = "Hiện chưa có suất chiếu sắp tới.";
        _recentLogs.EmptyStateText = "Chưa có hoạt động nào được ghi nhận.";
        DashboardStatsDto stats;
        try
        {
            stats = _reports.GetDashboardStats();

            _ticketsCard.Value =
                Convert.ToString(stats.TicketsToday) ?? "0";

            _todayRevenueCard.Value = string.Format(
                CultureInfo.CurrentCulture,
                "{0:N0} đ",
                stats.RevenueToday);

            _weekRevenueCard.Value = string.Format(
                CultureInfo.CurrentCulture,
                "{0:N0} đ",
                stats.RevenueLast7Days);

            _monthRevenueCard.Value = string.Format(
                CultureInfo.CurrentCulture,
                "{0:N0} đ",
                stats.RevenueLast30Days);

            _topMovies.DataSource =
                ((IEnumerable<RevenueByMovieDto>)stats.TopMovies)
                .ToList();

            ConfigureTopMovieColumns();

            var trend = new List<(string Label, decimal Value)>();

            foreach (var row in stats.RevenueTrend)
            {
                var rawDate = Convert.ToString(
                    row.RevenueDate,
                    CultureInfo.InvariantCulture) ?? string.Empty;

                var amount = Convert.ToDecimal(
                    row.Revenue,
                    CultureInfo.InvariantCulture);

                var label = DateTime.TryParse(rawDate, out var date)
                    ? date.ToString("dd/MM")
                    : rawDate;

                trend.Add((label, amount));
            }

            _chart.Points = trend;

            var upcomingRows = new List<UpcomingShowtimeRow>();

            foreach (var row in stats.UpcomingShowtimes)
                upcomingRows.Add(
                    new UpcomingShowtimeRow(
                        row.ShowtimeId,
                        row.MovieTitle,
                        row.RoomName,
                        row.StartTime,
                        row.SeatsLeft));

            _upcoming.DataSource = upcomingRows;

            ConfigureUpcomingColumns();

        }
        catch (Exception ex)
        {
            Trace.TraceError("Dashboard data could not be loaded: {0}", ex);
            _ticketsCard.Value = "—";
            _todayRevenueCard.Value = "—";
            _weekRevenueCard.Value = "—";
            _monthRevenueCard.Value = "—";
            _chart.EmptyMessage = "Không tải được tổng quan. Hãy chọn Làm mới.";
            _topMovies.DataSource = null;
            _topMovies.EmptyStateText = "Không tải được dữ liệu phim. Hãy chọn Làm mới.";
            _upcoming.DataSource = null;
            _upcoming.EmptyStateText = "Không tải được lịch chiếu. Hãy chọn Làm mới.";
            _recentLogs.DataSource = null;
            _recentLogs.EmptyStateText = "Không tải được nhật ký hoạt động.";
            return;
        }

        try
        {
            _recentLogs.DataSource =
                _auditService.GetRecentLogs(50).ToList();

            _recentLogs.EmptyStateText = "Chưa có hoạt động nào được ghi nhận.";
            ConfigureRecentLogColumns();
        }
        catch (Exception ex)
        {
            Trace.TraceError("Dashboard audit log could not be loaded: {0}", ex);
            _recentLogs.DataSource = null;
            _recentLogs.EmptyStateText = "Không tải được nhật ký hoạt động.";
        }
    }

    private void ConfigureTopMovieColumns()
    {
        SetColumn(_topMovies, nameof(RevenueByMovieDto.MovieId), "Mã phim", false);
        SetColumn(_topMovies, nameof(RevenueByMovieDto.TotalShowtimes), "Số suất", false);
        SetColumn(_topMovies, nameof(RevenueByMovieDto.TotalTicketsSold), "Tổng vé", false);
        SetColumn(_topMovies, nameof(RevenueByMovieDto.TicketRevenue), "Doanh thu vé", false);
        SetColumn(_topMovies, nameof(RevenueByMovieDto.ProductRevenue), "Doanh thu SP", false);
        SetColumn(_topMovies, nameof(RevenueByMovieDto.MovieTitle), "Phim");
        SetColumn(_topMovies, nameof(RevenueByMovieDto.TicketCount), "Vé bán");
        SetColumn(
            _topMovies,
            nameof(RevenueByMovieDto.TotalRevenue),
            "Doanh thu",
            format: "N0");

        ConfigureSummaryGrid(_topMovies);
        SetColumnLayout(_topMovies, nameof(RevenueByMovieDto.MovieTitle), 48);
        SetColumnLayout(_topMovies, nameof(RevenueByMovieDto.TicketCount), 18, center: true);
        SetColumnLayout(_topMovies, nameof(RevenueByMovieDto.TotalRevenue), 34, right: true);
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

    private void ConfigureRecentLogColumns()
    {
        SetColumn(_recentLogs, "Id", "Mã nhật ký", false);
        SetColumn(_recentLogs, "UserId", "Mã người dùng", false);
        SetColumn(_recentLogs, "EntityName", "Đối tượng", false);
        SetColumn(_recentLogs, "EntityId", "Mã đối tượng", false);
        SetColumn(_recentLogs, "Detail", "Chi tiết", false);
        SetColumn(_recentLogs, "Details", "Chi tiết", false);
        SetColumn(_recentLogs, "Username", "Nhân viên");
        SetColumn(_recentLogs, "Action", "Hoạt động");

        SetColumn(
            _recentLogs,
            "CreatedAt",
            "Thời gian",
            format: "dd/MM HH:mm");

        ConfigureSummaryGrid(_recentLogs);
        SetColumnLayout(_recentLogs, "Username", 30);
        SetColumnLayout(_recentLogs, "Action", 28);
        SetColumnLayout(_recentLogs, "CreatedAt", 42);
    }

    private static void ConfigureSummaryGrid(DataGridView grid)
    {
        grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.ColumnHeadersHeight = Math.Max(grid.ColumnHeadersHeight, 40);
        grid.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
    }

    private static void SetColumnLayout(
        DataGridView grid,
        string name,
        float fillWeight,
        bool center = false,
        bool right = false)
    {
        if (!grid.Columns.Contains(name))
            return;

        var column = grid.Columns[name];
        if (column is null)
            return;

        column.FillWeight = fillWeight;
        column.MinimumWidth = 72;
        column.HeaderCell.Style.WrapMode = DataGridViewTriState.False;
        column.DefaultCellStyle.Alignment = center
            ? DataGridViewContentAlignment.MiddleCenter
            : right
                ? DataGridViewContentAlignment.MiddleRight
                : DataGridViewContentAlignment.MiddleLeft;
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

        var column = grid.Columns[name];

        if (column is null)
            return;

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
