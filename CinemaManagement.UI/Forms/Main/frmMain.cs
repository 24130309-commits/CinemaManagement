
using System.Drawing;
using CinemaManagement.BLL.Session;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.UI.Forms.Auth;
using CinemaManagement.UI.Forms.Management;
using CinemaManagement.UI.Forms.Reports;
using CinemaManagement.UI.Helpers;
using CinemaManagement.UI.Forms.Main;

namespace CinemaManagement.UI.Forms;

public partial class frmMain : Form
{
    private readonly FormFactory _formFactory;
    private readonly IAuthService _authService;
    private readonly CurrentUserSession _session;
    private Panel _pnlContent = null!;

    public frmMain(
        FormFactory formFactory,
        IAuthService authService,
        CurrentUserSession session)
    {
        _formFactory = formFactory
            ?? throw new ArgumentNullException(nameof(formFactory));

        _authService = authService
            ?? throw new ArgumentNullException(nameof(authService));

        _session = session
            ?? throw new ArgumentNullException(nameof(session));

        BuildUI();

        OpenChildForm(
            _formFactory.Create<frmDashboard>());
    }

    private void BuildUI()
    {
        Text =
            $"Quản lý rạp chiếu phim - Xin chào: {_session.User?.FullName ?? "Người dùng"}";

        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(1100, 650);
        BackColor = Color.WhiteSmoke;

        var pnlMenu = new Panel
        {
            Dock = DockStyle.Left,
            Width = 220,
            BackColor = Color.FromArgb(44, 62, 80)
        };

        _pnlContent = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.WhiteSmoke
        };

        var lblTitle = new Label
        {
            Text = "CINEMA MANAGEMENT",
            Dock = DockStyle.Top,
            Height = 65,
            ForeColor = Color.White,
            Font = new Font(
                "Segoe UI",
                12,
                FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };

        pnlMenu.Controls.Add(lblTitle);

        void AddMenuButton(
            string text,
            Action onClick,
            bool adminOnly = false)
        {
            if (adminOnly && !_session.IsAdmin)
                return;

            var btn = new Button
            {
                Text = text,
                Dock = DockStyle.Top,
                Height = 45,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(44, 62, 80),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(15, 0, 0, 0),
                Font = new Font(
                    "Segoe UI",
                    9,
                    FontStyle.Regular)
            };

            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (_, _) => onClick();

            pnlMenu.Controls.Add(btn);
            pnlMenu.Controls.SetChildIndex(btn, 0);
        }

        AddMenuButton(
            "Tổng quan",
            () => OpenChildForm(
                _formFactory.Create<frmDashboard>()));

        AddMenuButton(
            "Báo cáo doanh thu",
            () => OpenChildForm(
                _formFactory.Create<frmRevenueReport>()));

        AddMenuButton(
            "Tỷ lệ lấp đầy ghế",
            () => OpenChildForm(
                _formFactory.Create<frmOccupancyReport>()));

        AddMenuButton(
            "Quản lý tài khoản",
            () => OpenChildForm(
                _formFactory.Create<frmUserManage>()),
            adminOnly: true);

        AddMenuButton(
            "Cấu hình và sao lưu",
            () => OpenChildForm(
                _formFactory.Create<frmSettings>()),
            adminOnly: true);

        AddMenuButton(
            "Đổi mật khẩu",
            () => _formFactory
                .Create<frmChangePassword>()
                .ShowDialog(this));

        AddMenuButton(
            "Đăng xuất",
            Logout);

        Controls.Add(_pnlContent);
        Controls.Add(pnlMenu);
    }

    private void OpenChildForm(Form child)
    {
        foreach (Control control in _pnlContent.Controls)
        {
            control.Dispose();
        }

        _pnlContent.Controls.Clear();

        child.TopLevel = false;
        child.FormBorderStyle = FormBorderStyle.None;
        child.Dock = DockStyle.Fill;

        _pnlContent.Controls.Add(child);

        child.Show();
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
