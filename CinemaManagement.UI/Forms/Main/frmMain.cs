
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using CinemaManagement.BLL.Session;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.Core.Enums;
using CinemaManagement.UI.Forms.Auth;
using CinemaManagement.UI.Forms.Catalog;
using CinemaManagement.UI.Forms.Customers;
using CinemaManagement.UI.Forms.Management;
using CinemaManagement.UI.Forms.Products;
using CinemaManagement.UI.Forms.Reports;
using CinemaManagement.UI.Forms.Sales;
using CinemaManagement.UI.Forms.Main;
using CinemaManagement.UI.Forms.Media;
using CinemaManagement.UI.Helpers;

namespace CinemaManagement.UI.Forms;

public partial class frmMain : Form
{
    private readonly FormFactory _formFactory;
    private readonly IAuthService _authService;
    private readonly IGenreService _genreService;
    private readonly CurrentUserSession _session;

    private readonly List<Button> _menuButtons = new();
    private readonly List<Control> _menuSections = new();
    private readonly Dictionary<Button, string> _menuIcons = new();
    private readonly ToolTip _navigationToolTip = new()
    {
        Active = false,
        ShowAlways = true
    };

    private Panel _sidebar = null!;
    private Panel _brandPanel = null!;
    private Panel _profilePanel = null!;
    private Panel _menuViewport = null!;
    private Label _brandTitle = null!;
    private Label _brandSubtitle = null!;
    private Label _brandMark = null!;
    private Label _profileName = null!;
    private Label _profileRole = null!;
    private Label _profileAvatar = null!;
    private Button _sidebarToggle = null!;
    private bool _sidebarCollapsed;
    private Panel _pnlContent = null!;
    private Button? _activeMenuButton;

    private static readonly Color Sidebar =
        ThemeHelper.PrimaryDark;

    private static readonly Color SidebarHover =
        Color.FromArgb(30, 41, 59);

    private static readonly Color SidebarSelected =
        Color.FromArgb(255, 138, 50);

    private static readonly Color SidebarText =
        Color.FromArgb(203, 213, 225);

    private static readonly Font BrandMarkFont =
        new("Segoe UI", 22f, FontStyle.Bold);

    private static readonly Font BrandMarkCollapsedFont =
        new("Segoe UI", 18f, FontStyle.Bold);

    private static readonly Font NavigationFont =
        new("Segoe UI", 9f, FontStyle.Regular);

    private static readonly Font NavigationActiveFont =
        new("Segoe UI", 9f, FontStyle.Bold);

    public frmMain(
        FormFactory formFactory,
        IAuthService authService,
        IGenreService genreService,
        CurrentUserSession session)
    {
        _formFactory = formFactory
            ?? throw new ArgumentNullException(nameof(formFactory));

        _authService = authService
            ?? throw new ArgumentNullException(nameof(authService));

        _genreService = genreService
            ?? throw new ArgumentNullException(nameof(genreService));

        _session = session
            ?? throw new ArgumentNullException(nameof(session));

        BuildUI();
        Disposed += (_, _) => _navigationToolTip.Dispose();

        if (_menuButtons.Count > 0)
            SetActiveMenuButton(_menuButtons[0]);

        OpenChildForm(
            _formFactory.Create<frmDashboard>(),
            "Tổng quan");
    }

    private void BuildUI()
    {
        ThemeHelper.ConfigureDpiScaling(this);
        Text =
            $"Quản lý rạp chiếu phim - Xin chào: {_session.User?.FullName ?? "Người dùng"}";

        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(1160, 700);
        BackColor = ThemeHelper.Background;
        Font = ThemeHelper.BodyFont;
        StartPosition = FormStartPosition.CenterScreen;

        _sidebar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 252,
            BackColor = Sidebar
        };

        _brandPanel = BuildBrandPanel();
        _profilePanel = BuildProfilePanel();

        _menuViewport = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Sidebar,
            AutoScroll = true,
            Padding = new Padding(10, 8, 10, 8)
        };

        var menuFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Sidebar,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        _menuViewport.Controls.Add(menuFlow);

        _menuViewport.Resize += (_, _) =>
        {
            menuFlow.Width =
                Math.Max(52, _menuViewport.ClientSize.Width - 4);

            foreach (Control control in menuFlow.Controls)
            {
                if (control is Button button)
                {
                    button.Width =
                        Math.Max(48, menuFlow.ClientSize.Width - 4);
                }
                else
                {
                    control.Width =
                        Math.Max(48, menuFlow.ClientSize.Width - 4);
                }
            }
        };

        AddMenuSection(menuFlow, "TỔNG QUAN");

        AddMenuButton(
            menuFlow,
            "Tổng quan",
            () => OpenChildForm(
                _formFactory.Create<frmDashboard>(),
                "Tổng quan"));
        AddMenuButton(
            menuFlow,
            "Khám phá phim",
            () => OpenChildForm(
                _formFactory.Create<frmMovieDiscovery>(),
                "Khám phá phim"));

        AddMenuSection(menuFlow, "QUẢN LÝ DANH MỤC");

        AddMenuButton(
            menuFlow,
            "Quản lý phim",
            () => OpenChildForm(
                _formFactory.Create<frmMovieList>(),
                "Quản lý phim"));

        AddMenuSection(menuFlow, "BÁN HÀNG");

        AddMenuButton(
            menuFlow,
            "Bán vé",
            () => OpenChildForm(
                _formFactory.Create<frmSelectMovie>(),
                "Bán vé"));

        AddMenuButton(
            menuFlow,
            "Lịch sử hóa đơn",
            () => OpenChildForm(
                _formFactory.Create<frmInvoiceHistory>(),
                "Lịch sử hóa đơn"));

        AddMenuSection(menuFlow, "DANH MỤC KHÁC");

        AddMenuButton(
            menuFlow,
            "Quản lý thể loại",
            () => OpenChildForm(
                new frmGenreManage(_genreService, _session),
                "Quản lý thể loại"),
            adminOnly: true);

        AddMenuButton(
            menuFlow,
            "Quản lý phòng chiếu",
            () => OpenChildForm(
                _formFactory.Create<frmRoomManage>(),
                "Quản lý phòng chiếu"),
            adminOnly: true);

        AddMenuButton(
            menuFlow,
            "Quản lý suất chiếu",
            () => OpenChildForm(
                _formFactory.Create<frmShowtimeManage>(),
                "Quản lý suất chiếu"),
            adminOnly: true);

        AddMenuSection(menuFlow, "BÁO CÁO");

        AddMenuButton(
            menuFlow,
            "Báo cáo tổng hợp",
            () => OpenChildForm(
                _formFactory.Create<frmReportsHub>(),
                "Báo cáo tổng hợp"));


        AddMenuSection(menuFlow, "TIỆN ÍCH VIDEO");

        AddMenuButton(
            menuFlow,
            "Trình phát video",
            () => OpenChildForm(
                _formFactory.Create<frmVideoPlayer>(),
                "Trình phát video"));

        AddMenuSection(menuFlow, "QUẢN TRỊ HỆ THỐNG");

        AddMenuButton(
            menuFlow,
            "Quản lý khách hàng",
            () => OpenChildForm(
                _formFactory.Create<frmCustomerManage>(),
                "Quản lý khách hàng"),
            adminOnly: true);

        AddMenuButton(
            menuFlow,
            "Quản lý sản phẩm",
            () => OpenChildForm(
                _formFactory.Create<frmProductManage>(),
                "Quản lý sản phẩm"),
            adminOnly: true);

        AddMenuButton(
            menuFlow,
            "Khuyến mãi & giá vé",
            () => OpenChildForm(
                _formFactory.Create<frmSalesRulesHub>(),
                "Khuyến mãi & giá vé"),
            adminOnly: true);

        AddMenuButton(
            menuFlow,
            "Quản lý tài khoản",
            () => OpenChildForm(
                _formFactory.Create<frmUserManage>(),
                "Quản lý tài khoản"),
            adminOnly: true);

        AddMenuButton(
            menuFlow,
            "Cấu hình và sao lưu",
            () => OpenChildForm(
                _formFactory.Create<frmSettings>(),
                "Cấu hình và sao lưu"),
            adminOnly: true);

        AddMenuSection(menuFlow, "TÀI KHOẢN");

        AddMenuButton(
            menuFlow,
            "Đổi mật khẩu",
            () => OpenChildForm(
                _formFactory.Create<frmChangePassword>(),
                "Bảo mật tài khoản"));

        AddMenuButton(
            menuFlow,
            "Đăng xuất",
            Logout,
            isLogout: true);

        _sidebar.Controls.Add(_menuViewport);
        _sidebar.Controls.Add(_profilePanel);
        _sidebar.Controls.Add(_brandPanel);

        _pnlContent = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeHelper.Background,
            Padding = new Padding(18, 16, 18, 18)
        };

        Controls.Add(_pnlContent);
        Controls.Add(_sidebar);
    }

    private Panel BuildBrandPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 92,
            BackColor = Sidebar,
            Padding = new Padding(20, 15, 14, 12)
        };

        _brandMark = new Label
        {
            Text = "C",
            Location = new Point(18, 18),
            Size = new Size(42, 42),
            BackColor = SidebarSelected,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 22f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };

        _brandTitle = new Label
        {
            Text = "CINEMA",
            Location = new Point(70, 18),
            Size = new Size(145, 23),
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 12f, FontStyle.Bold)
        };

        _brandSubtitle = new Label
        {
            Text = "MANAGEMENT SYSTEM",
            Location = new Point(71, 41),
            Size = new Size(150, 19),
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(148, 163, 184),
            Font = new Font("Segoe UI", 7.5f, FontStyle.Bold)
        };

        _sidebarToggle = new Button
        {
            Text = "‹",
            Location = new Point(211, 29),
            Size = new Size(28, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = Sidebar,
            ForeColor = Color.FromArgb(203, 213, 225),
            Font = new Font("Segoe UI", 15f, FontStyle.Regular),
            Cursor = Cursors.Hand,
            AccessibleName = "Thu gọn thanh điều hướng",
            AccessibleDescription = "Thu gọn hoặc mở rộng thanh điều hướng"
        };
        _sidebarToggle.FlatAppearance.BorderSize = 0;
        _sidebarToggle.FlatAppearance.MouseOverBackColor = SidebarHover;
        _sidebarToggle.Click += (_, _) => SetSidebarCollapsed(!_sidebarCollapsed);

        panel.Controls.AddRange(
            new Control[] { _brandMark, _brandTitle, _brandSubtitle, _sidebarToggle });

        return panel;
    }

    private Panel BuildProfilePanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 72,
            BackColor = Color.FromArgb(23, 33, 52),
            Padding = new Padding(16, 10, 12, 8)
        };

        _profileAvatar = new Label
        {
            Text = GetInitials(_session.User?.FullName),
            Location = new Point(15, 15),
            Size = new Size(38, 38),
            BackColor = Color.FromArgb(51, 65, 85),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };

        _profileName = new Label
        {
            Text = _session.User?.Username ?? "Người dùng",
            Location = new Point(63, 13),
            Size = new Size(175, 22),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            AutoEllipsis = true,
            AccessibleName = $"Tài khoản: {_session.User?.FullName ?? "Người dùng"}"
        };
        _navigationToolTip.SetToolTip(
            _profileName,
            _session.User?.FullName ?? "Người dùng");

        _profileRole = new Label
        {
            Text = _session.Role switch
            {
                UserRole.Admin => "Quản trị viên",
                UserRole.Manager => "Quản lý",
                _ => "Nhân viên"
            },
            Location = new Point(64, 36),
            Size = new Size(150, 18),
            ForeColor = Color.FromArgb(148, 163, 184),
            Font = new Font("Segoe UI", 8f)
        };

        panel.Controls.AddRange(
            new Control[] { _profileAvatar, _profileName, _profileRole });

        return panel;
    }

    private void AddMenuSection(
        FlowLayoutPanel menu,
        string caption)
    {
        var label = new Label
        {
            Text = caption,
            Width = menu.ClientSize.Width - 4,
            Height = 28,
            Margin = new Padding(7, 13, 4, 2),
            ForeColor = Color.FromArgb(100, 116, 139),
            Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };

        menu.Controls.Add(label);
        _menuSections.Add(label);
    }

    private void AddMenuButton(
        FlowLayoutPanel menu,
        string text,
        Action onClick,
        bool adminOnly = false,
        bool isLogout = false)
    {
        if (adminOnly && !_session.IsAdmin)
            return;

        var button = new Button
        {
            Text = GetMenuIcon(text) + "    " + text,
            Width = Math.Max(48, menu.ClientSize.Width - 4),
            Height = 40,
            Margin = new Padding(2, 2, 2, 2),
            FlatStyle = FlatStyle.Flat,
            BackColor = Sidebar,
            ForeColor = SidebarText,
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(13, 0, 8, 0),
            Cursor = Cursors.Hand,
            Tag = "sidebar",
            UseVisualStyleBackColor = false
        };

        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = SidebarHover;
        button.FlatAppearance.MouseDownBackColor = SidebarHover;
        button.AccessibleName = text;
        button.AccessibleRole = AccessibleRole.PushButton;
        _navigationToolTip.SetToolTip(button, text);

        if (isLogout)
            button.ForeColor = Color.FromArgb(252, 165, 165);

        button.Click += (_, _) =>
        {
            if (!isLogout)
                SetActiveMenuButton(button);

            onClick();
        };

        menu.Controls.Add(button);
        _menuButtons.Add(button);
        _menuIcons[button] = GetMenuIcon(text);
    }

    private static string GetMenuIcon(string caption) => caption switch
    {
        "Tổng quan" => "⌂",
        "Khám phá phim" or "Quản lý phim" => "▣",
        "Bán vé" => "▤",
        "Lịch sử hóa đơn" => "▧",
        "Quản lý thể loại" => "◈",
        "Quản lý phòng chiếu" => "▦",
        "Quản lý suất chiếu" => "◷",
        "Báo cáo tổng hợp" => "▥",
        "Trình phát video" => "▷",
        "Quản lý khách hàng" => "♙",
        "Quản lý sản phẩm" => "▱",
        "Khuyến mãi & giá vé" => "%",
        "Quản lý tài khoản" => "♙",
        "Cấu hình và sao lưu" => "⚙",
        "Đổi mật khẩu" => "♧",
        "Đăng xuất" => "⇥",
        _ => "•"
    };

    private void SetSidebarCollapsed(bool collapsed)
    {
        if (_sidebarCollapsed == collapsed)
            return;

        _sidebarCollapsed = collapsed;
        _sidebar.SuspendLayout();
        _sidebar.Width = collapsed ? 76 : 252;
        _brandPanel.Height = collapsed ? 76 : 92;
        _brandPanel.Padding = collapsed
            ? new Padding(0)
            : new Padding(20, 15, 14, 12);
        _brandMark.Location = collapsed ? new Point(6, 21) : new Point(18, 18);
        _brandMark.Size = collapsed ? new Size(36, 36) : new Size(42, 42);
        _brandMark.Font = collapsed ? BrandMarkCollapsedFont : BrandMarkFont;
        _brandTitle.Visible = !collapsed;
        _brandSubtitle.Visible = !collapsed;
        _sidebarToggle.Location = collapsed ? new Point(46, 23) : new Point(211, 29);
        _sidebarToggle.Size = collapsed ? new Size(26, 30) : new Size(28, 32);
        _sidebarToggle.Text = collapsed ? "›" : "‹";
        _sidebarToggle.AccessibleName = collapsed
            ? "Mở rộng thanh điều hướng"
            : "Thu gọn thanh điều hướng";
        _profilePanel.Height = collapsed ? 64 : 72;
        _profilePanel.Padding = collapsed
            ? new Padding(0)
            : new Padding(16, 10, 12, 8);
        _profileAvatar.Location = collapsed ? new Point(19, 13) : new Point(15, 15);
        _profileName.Visible = !collapsed;
        _profileRole.Visible = !collapsed;
        _menuViewport.Padding = collapsed
            ? new Padding(4, 8, 4, 8)
            : new Padding(10, 8, 10, 8);

        foreach (var section in _menuSections)
            section.Visible = !collapsed;

        foreach (var button in _menuButtons)
        {
            button.Text = collapsed ? _menuIcons[button] : _menuIcons[button] + "    " + button.AccessibleName;
            button.TextAlign = collapsed
                ? ContentAlignment.MiddleCenter
                : ContentAlignment.MiddleLeft;
            button.Padding = collapsed
                ? Padding.Empty
                : new Padding(13, 0, 8, 0);
            button.Margin = collapsed
                ? new Padding(6, 2, 6, 2)
                : new Padding(2, 2, 2, 2);
            button.Width = collapsed
                ? 48
                : Math.Max(180, (button.Parent?.ClientSize.Width ?? 184) - 4);
            button.AccessibleDescription = collapsed
                ? $"Đi tới {button.AccessibleName}"
                : string.Empty;
        }

        _sidebar.ResumeLayout(true);
        _navigationToolTip.Active = collapsed;
    }

    private void SetActiveMenuButton(Button selected)
    {
        if (_activeMenuButton == selected)
            return;

        if (_activeMenuButton != null)
        {
        _activeMenuButton.BackColor = Sidebar;
        _activeMenuButton.ForeColor = SidebarText;
        _activeMenuButton.Font = NavigationFont;

        _activeMenuButton.FlatAppearance.MouseOverBackColor =
            SidebarHover;
        }

        _activeMenuButton = selected;
        _activeMenuButton.BackColor = SidebarSelected;
        _activeMenuButton.ForeColor = ThemeHelper.Primary;
        _activeMenuButton.Font = NavigationActiveFont;

        _activeMenuButton.FlatAppearance.MouseOverBackColor =
            ThemeHelper.AccentHover;
    }


    private void OpenChildForm(Control child, string pageTitle)
    {
        ArgumentNullException.ThrowIfNull(child);

        while (_pnlContent.Controls.Count > 0)
        {
            var control = _pnlContent.Controls[0];
            _pnlContent.Controls.RemoveAt(0);
            control.Dispose();
        }

        child.AccessibleDescription = pageTitle;

        ThemeHelper.Apply(child);

        // Pages are hosted inside the shell workspace. Their standalone
        // dialog minimum sizes must not force the embedded surface past the
        // available viewport on restored or high-DPI windows.
        child.Dock = DockStyle.Fill;

        if (child is Form form)
        {
            form.MinimumSize = System.Drawing.Size.Empty;
            form.TopLevel = false;
            form.FormBorderStyle = FormBorderStyle.None;
        }

        _pnlContent.Controls.Add(child);
        child.Visible = true;
    }

    private static string GetInitials(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return "U";

        var parts = fullName.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 1)
            return parts[0][..1].ToUpperInvariant();

        return string.Concat(
            parts[0][..1],
            parts[^1][..1]).ToUpperInvariant();
    }

    private void Logout()
    {
        if (!MessageHelper.Confirm(
                "Bạn có chắc chắn muốn đăng xuất?"))
        {
            return;
        }

        _authService.Logout();
        Application.Restart();
    }
}
