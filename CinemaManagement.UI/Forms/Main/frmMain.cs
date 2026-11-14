using System.Drawing;
using System.Windows.Forms;
using CinemaManagement.BLL;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.UI.Forms.Auth;
using CinemaManagement.UI.Forms.Management;
using CinemaManagement.UI.Forms.Reports;
using CinemaManagement.UI.Helpers;
using frmSettings = CinemaManagement.UI.Forms.Management.frmSettings;

namespace CinemaManagement.UI.Forms
{
    public partial class frmMain : Form
    {
        private readonly FormFactory _formFactory;
        private readonly IAuthService _authService;
        private Panel pnlContent;

        public frmMain(FormFactory formFactory, IAuthService authService)
        {
            _formFactory = formFactory;
            _authService = authService;
            BuildUI();
            OpenChildForm(_formFactory.Create<frmDashboard>());
        }

        private void BuildUI()
        {
            Text = $"Quản Lý Rạp Chiếu Phim - Xin chào: {CurrentUserSession.CurrentUser?.FullName}";
            WindowState = FormWindowState.Maximized;

            var pnlMenu = new Panel { Dock = DockStyle.Left, Width = 210, BackColor = Color.FromArgb(44, 62, 80) };
            pnlContent = new Panel { Dock = DockStyle.Fill, BackColor = Color.WhiteSmoke };

            void AddMenuButton(string text, System.Action onClick, bool adminOnly = false)
            {
                if (adminOnly && !CurrentUserSession.IsAdmin) return;
                var btn = new Button
                {
                    Text = text,
                    Dock = DockStyle.Top,
                    Height = 45,
                    FlatStyle = FlatStyle.Flat,
                    ForeColor = Color.White,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(15, 0, 0, 0)
                };
                btn.Click += (s, e) => onClick();
                pnlMenu.Controls.Add(btn);
                pnlMenu.Controls.SetChildIndex(btn, 0);
            }

            AddMenuButton("Tổng quan (Dashboard)", () => OpenChildForm(_formFactory.Create<frmDashboard>()));
            AddMenuButton("Báo cáo Doanh thu", () => OpenChildForm(_formFactory.Create<frmRevenueReport>()));
            AddMenuButton("Tỷ lệ Lấp đầy Ghế", () => OpenChildForm(_formFactory.Create<frmOccupancyReport>()));
            AddMenuButton("Quản lý Tài khoản", () => OpenChildForm(_formFactory.Create<frmUserManage>()), adminOnly: true);
            AddMenuButton("Cấu hình & Sao lưu", () => OpenChildForm(_formFactory.Create<frmSettings>()), adminOnly: true);
            AddMenuButton("Đổi mật khẩu", () => _formFactory.Create<frmChangePassword>().ShowDialog());
            AddMenuButton("Đăng xuất", () =>
            {
                if (MessageHelper.Confirm("Bạn có chắc chắn muốn đăng xuất?"))
                {
                    _authService.Logout();
                    Application.Restart();
                }
            });

            Controls.Add(pnlContent);
            Controls.Add(pnlMenu);
        }

        private void OpenChildForm(Form child)
        {
            pnlContent.Controls.Clear();
            child.TopLevel = false;
            child.FormBorderStyle = FormBorderStyle.None;
            child.Dock = DockStyle.Fill;
            pnlContent.Controls.Add(child);
            child.Show();
        }
    }
}