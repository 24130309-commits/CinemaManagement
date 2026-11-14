using System;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.UI.Helpers;

namespace CinemaManagement.UI.Forms.Management
{
    public partial class frmUserManage : Form
    {
        private readonly IUserService _userService;
        private readonly DataGridView _dgv;

        public frmUserManage(IUserService userService)
        {
            _userService = userService;
            Padding = new Padding(15);

            var pnlTop = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 45 };
            var txtUser = new TextBox { PlaceholderText = "Username", Width = 120 };
            var txtPass = new TextBox { PlaceholderText = "Mật khẩu", Width = 120, PasswordChar = '*' };
            var txtName = new TextBox { PlaceholderText = "Họ tên", Width = 150 };
            var cboRole = new ComboBox { DataSource = Enum.GetValues(typeof(UserRole)), Width = 100 };
            var btnAdd = new Button { Text = "Thêm User", Width = 90 };
            var btnToggle = new Button { Text = "Khóa / Mở khóa", Width = 110 };

            _dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            btnAdd.Click += (s, e) =>
            {
                try
                {
                    _userService.CreateUser(new User
                    {
                        Username = txtUser.Text,
                        FullName = txtName.Text,
                        Role = (UserRole)cboRole.SelectedItem
                    }, txtPass.Text);

                    MessageHelper.ShowInfo("Thêm tài khoản thành công!");
                    txtUser.Clear();
                    txtPass.Clear();
                    txtName.Clear();
                    LoadData();
                }
                catch (BusinessException ex)
                {
                    MessageHelper.ShowWarning(ex.Message);
                }
            };

            btnToggle.Click += (s, e) =>
            {
                if (_dgv.CurrentRow?.DataBoundItem is User selected)
                {
                    try
                    {
                        _userService.ToggleUserStatus(selected.Id);
                        LoadData();
                    }
                    catch (BusinessException ex)
                    {
                        MessageHelper.ShowWarning(ex.Message);
                    }
                }
            };

            pnlTop.Controls.AddRange(new Control[] { txtUser, txtPass, txtName, cboRole, btnAdd, btnToggle });
            Controls.Add(_dgv);
            Controls.Add(pnlTop);
            LoadData();
        }

        private void LoadData() => _dgv.DataSource = _userService.GetAllUsers().ToList();
    }
}