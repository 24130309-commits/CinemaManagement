﻿using CinemaManagement.BLL.Session;
using CinemaManagement.Core.Enums;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.UI.Forms.Auth;
using CinemaManagement.UI.Forms.Management;
using CinemaManagement.UI.Forms.Reports;
using CinemaManagement.UI.Helpers;

namespace CinemaManagement.UI.Forms.Main;

public partial class frmMain : Form
{
    private readonly CurrentUserSession _session;
    private readonly IAuthService _auth;
    private readonly IAuditService _audit;
    private readonly FormFactory _forms;
    private readonly Label _clock = new();
    private readonly Label _welcome = new();
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 1000 };

    public bool LogoutRequested { get; private set; }

    public frmMain(
        CurrentUserSession session,
        IAuthService auth,
        IAuditService audit,
        FormFactory forms)
    {
        _session = session;
        _auth = auth;
        _audit = audit;
        _forms = forms;

        InitializeComponent();
        BuildUi();

        _timer.Tick += (_, _) =>
            _clock.Text = DateTime.Now.ToString("dddd, dd/MM/yyyy  |  HH:mm:ss");

        _clock.Text = DateTime.Now.ToString("dddd, dd/MM/yyyy  |  HH:mm:ss");
        _timer.Start();

        FormClosed += (_, _) => _timer.Dispose();
    }

    private void BuildUi()
    {
        Text = "Cinema Management";
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(1120, 720);
        BackColor = ThemeHelper.Background;

        var menu = BuildMenu();
        MainMenuStrip = menu;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            BackColor = ThemeHelper.Background,
            Margin = System.Windows.Forms.Padding.Empty,
            Padding = System.Windows.Forms.Padding.Empty
        };

        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 238));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        root.Controls.Add(menu, 0, 0);
        root.SetColumnSpan(menu, 2);

        var sidebar = BuildSidebar();
        root.Controls.Add(sidebar, 0, 1);

        var content = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeHelper.Background
        };

        var header = BuildHeader();
        var body = BuildHomeContent();

        content.Controls.Add(body);
        content.Controls.Add(header);

        root.Controls.Add(content, 1, 1);

        Controls.Add(root);
        ThemeHelper.Apply(this);
    }

    private MenuStrip BuildMenu()
    {
        var menu = new MenuStrip();
        var system = new ToolStripMenuItem("Hệ thống");

        system.DropDownItems.Add(
            "Đổi mật khẩu",
            null,
            (_, _) => _forms.ShowDialog<frmChangePassword>(this));

        if (_session.IsInRole(UserRole.Admin))
        {
            system.DropDownItems.Add(
                "Cài đặt & sao lưu",
                null,
                (_, _) => _forms.ShowDialog<frmSettings>(this));
        }

        system.DropDownItems.Add(new ToolStripSeparator());

        system.DropDownItems.Add(
            "Đăng xuất",
            null,
            (_, _) => Logout());

        var manage = new ToolStripMenuItem("Quản lý");

        if (_session.IsInRole(UserRole.Admin))
        {
            manage.DropDownItems.Add(
                "Quản lý người dùng",
                null,
                (_, _) => _forms.ShowDialog<frmUserManage>(this));
        }

        if (_session.IsInRole(UserRole.Admin, UserRole.Manager))
        {
            var reports = new ToolStripMenuItem("Báo cáo");

            reports.DropDownItems.Add(
                "Doanh thu",
                null,
                (_, _) => _forms.ShowDialog<frmRevenueReport>(this));

            reports.DropDownItems.Add(
                "Tỷ lệ lấp đầy",
                null,
                (_, _) => _forms.ShowDialog<frmOccupancyReport>(this));

            manage.DropDownItems.Add(reports);
        }

        var help = new ToolStripMenuItem("Trợ giúp");

        help.DropDownItems.Add(
            "Thông tin nhóm / phiên bản",
            null,
            (_, _) => MessageHelper.Info(
                "Cinema Management\n" +
                "Đồ án Lập trình .NET\n" +
                "Phân công: A – Nền tảng/Hệ thống/Báo cáo; " +
                "B – Danh mục/Lịch chiếu/Giá; C – Bán vé/Thanh toán\n" +
                "Phiên bản 1.0.0",
                "Thông tin phần mềm"));

        menu.Items.AddRange([system, manage, help]);

        return menu;
    }

    private Control BuildSidebar()
    {
        var sidebar = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeHelper.PrimaryDark,
            Padding = new Padding(14, 18, 14, 14)
        };

        var brand = new Panel
        {
            Dock = DockStyle.Top,
            Height = 88,
            BackColor = ThemeHelper.PrimaryDark
        };

        brand.Controls.Add(new Label
        {
            Text = "CINEMA",
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(8, 5),
            AutoSize = true
        });

        brand.Controls.Add(new Label
        {
            Text = "MANAGEMENT SYSTEM",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(191, 219, 254),
            Location = new Point(10, 43),
            AutoSize = true
        });

        var nav = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = ThemeHelper.PrimaryDark,
            Padding = new Padding(0, 12, 0, 0)
        };

        AddNav(
            nav,
            "▦   Tổng quan",
            () => OpenDashboard(),
            _session.IsInRole(UserRole.Admin, UserRole.Manager));

        AddNav(
            nav,
            "♙   Quản lý người dùng",
            () => OpenUsers(),
            _session.IsInRole(UserRole.Admin));

        AddNav(
            nav,
            "▤   Báo cáo doanh thu",
            () => OpenRevenue(),
            _session.IsInRole(UserRole.Admin, UserRole.Manager));

        AddNav(
            nav,
            "▥   Tỷ lệ lấp đầy",
            () => OpenOccupancy(),
            _session.IsInRole(UserRole.Admin, UserRole.Manager));

        AddNav(
            nav,
            "⚙   Cài đặt hệ thống",
            () => OpenSettings(),
            _session.IsInRole(UserRole.Admin));

        var account = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 92,
            BackColor = ThemeHelper.PrimaryDark,
            Padding = new Padding(8)
        };

        _welcome.Text = _session.User is null
            ? "Tài khoản"
            : $"{_session.User.FullName}\n{_session.User.Role}";

        _welcome.Dock = DockStyle.Fill;
        _welcome.ForeColor = Color.White;
        _welcome.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        _welcome.TextAlign = ContentAlignment.MiddleLeft;

        account.Controls.Add(_welcome);

        sidebar.Controls.Add(nav);
        sidebar.Controls.Add(account);
        sidebar.Controls.Add(brand);

        return sidebar;
    }

    private static void AddNav(
        FlowLayoutPanel parent,
        string text,
        Action action,
        bool visible)
    {
        if (!visible)
            return;

        var button = new Button
        {
            Text = text,
            Tag = "sidebar",
            Width = 204,
            Height = 44,
            Margin = new Padding(0, 3, 0, 3),
            TextAlign = ContentAlignment.MiddleLeft,
            FlatStyle = FlatStyle.Flat
        };

        button.Click += (_, _) => action();

        parent.Controls.Add(button);
    }

    private Control BuildHeader()
    {
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 78,
            BackColor = ThemeHelper.Surface,
            Padding = new Padding(26, 12, 26, 12)
        };

        header.Controls.Add(new Label
        {
            Text = "TRUNG TÂM ĐIỀU HÀNH",
            Dock = DockStyle.Left,
            Width = 310,
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            ForeColor = ThemeHelper.Primary,
            TextAlign = ContentAlignment.MiddleLeft
        });

        _clock.Dock = DockStyle.Right;
        _clock.Width = 280;
        _clock.Font = ThemeHelper.BodyFont;
        _clock.ForeColor = ThemeHelper.Muted;
        _clock.TextAlign = ContentAlignment.MiddleRight;

        header.Controls.Add(_clock);

        return header;
    }

    private Control BuildHomeContent()
    {
        var body = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(30),
            BackColor = ThemeHelper.Background
        };

        var title = new Label
        {
            Text = $"Xin chào, {_session.User?.FullName ?? "bạn"}!",
            Font = new Font("Segoe UI", 23, FontStyle.Bold),
            ForeColor = ThemeHelper.Primary,
            AutoSize = true,
            Location = new Point(30, 30)
        };

        var subtitle = new Label
        {
            Text = "Chúc bạn một ngày làm việc hiệu quả. Chọn một chức năng để bắt đầu.",
            Font = new Font("Segoe UI", 10f),
            ForeColor = ThemeHelper.Muted,
            AutoSize = true,
            Location = new Point(32, 78)
        };

        var quickActions = new FlowLayoutPanel
        {
            Location = new Point(30, 142),
            Size = new Size(900, 190),
            WrapContents = true,
            BackColor = ThemeHelper.Background
        };

        if (_session.IsInRole(UserRole.Admin, UserRole.Manager))
        {
            quickActions.Controls.Add(
                CreateActionCard(
                    "Bảng điều khiển",
                    "Xem tình hình hoạt động và các chỉ số chính",
                    "Mở bảng điều khiển",
                    OpenDashboard));
        }

        if (_session.IsInRole(UserRole.Admin))
        {
            quickActions.Controls.Add(
                CreateActionCard(
                    "Quản lý nhân sự",
                    "Tạo tài khoản, cập nhật vai trò và trạng thái",
                    "Quản lý người dùng",
                    OpenUsers));
        }

        if (_session.IsInRole(UserRole.Admin, UserRole.Manager))
        {
            quickActions.Controls.Add(
                CreateActionCard(
                    "Báo cáo",
                    "Theo dõi doanh thu và hiệu suất lấp đầy",
                    "Xem báo cáo",
                    OpenRevenue));
        }

        body.Controls.Add(title);
        body.Controls.Add(subtitle);
        body.Controls.Add(quickActions);

        return body;
    }

    private static Control CreateActionCard(
        string title,
        string description,
        string buttonText,
        Action action)
    {
        var card = new Panel
        {
            Size = new Size(245, 166),
            BackColor = ThemeHelper.Surface,
            Padding = new Padding(16),
            Margin = new Padding(0, 0, 16, 14)
        };

        card.Controls.Add(new Label
        {
            Text = title,
            Location = new Point(16, 14),
            Size = new Size(210, 28),
            Font = ThemeHelper.SectionFont,
            ForeColor = ThemeHelper.Primary
        });

        card.Controls.Add(new Label
        {
            Text = description,
            Location = new Point(16, 48),
            Size = new Size(210, 48),
            Font = ThemeHelper.SmallFont,
            ForeColor = ThemeHelper.Muted
        });

        var button = new Button
        {
            Text = buttonText,
            Location = new Point(16, 108),
            Size = new Size(210, 38)
        };

        button.Click += (_, _) => action();

        card.Controls.Add(button);

        return card;
    }

    private void OpenDashboard() =>
        _forms.ShowDialog<frmDashboard>(this);

    private void OpenUsers() =>
        _forms.ShowDialog<frmUserManage>(this);

    private void OpenRevenue() =>
        _forms.ShowDialog<frmRevenueReport>(this);

    private void OpenOccupancy() =>
        _forms.ShowDialog<frmOccupancyReport>(this);

    private void OpenSettings() =>
        _forms.ShowDialog<frmSettings>(this);

    private void Logout()
    {
        if (!MessageHelper.Confirm("Bạn có chắc chắn muốn đăng xuất?"))
            return;

        try
        {
            _audit.Log("LOGOUT", "Đăng xuất hệ thống.");
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);
        }
        finally
        {
            _auth.Logout();
            LogoutRequested = true;
            Close();
        }
    }
}