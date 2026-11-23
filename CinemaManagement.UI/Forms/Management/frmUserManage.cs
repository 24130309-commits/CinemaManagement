
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.UI.Helpers;

namespace CinemaManagement.UI.Forms.Management;

public partial class frmUserManage : Form
{
    private readonly IUserService _users;
    private readonly IAuditService _audit;

    private readonly DataGridView _grid = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AutoGenerateColumns = true,
        AllowUserToAddRows = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
    };

    public frmUserManage(
        IUserService users,
        IAuditService audit)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));

        InitializeComponent();

        Text = "Quản lý người dùng";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(900, 550);
        Size = new Size(1050, 650);

        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 66,
            Padding = new Padding(14, 10, 10, 8),
            WrapContents = false,
            BackColor = ThemeHelper.Surface
        };

        bar.Controls.Add(new Label
        {
            Text = "QUẢN LÝ NHÂN VIÊN",
            AutoSize = true,
            Font = ThemeHelper.SectionFont,
            ForeColor = ThemeHelper.Primary,
            Padding = new Padding(0, 8, 14, 0)
        });

        AddButton(bar, "Thêm nhân viên", AddUser);
        AddButton(bar, "Sửa thông tin", EditUser);
        AddButton(bar, "Đặt lại mật khẩu", ResetPassword);
        AddButton(bar, "Khóa / Mở khóa", ToggleActive);
        AddButton(bar, "Làm mới", (_, _) => LoadUsers());

        Controls.Add(_grid);
        Controls.Add(bar);

        _grid.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 ||
                e.ColumnIndex < 0 ||
                _grid.Columns[e.ColumnIndex].Name != "IsActive" ||
                e.Value is not bool active)
            {
                return;
            }

            e.Value = active ? "Đang hoạt động" : "Đã khóa";
            e.CellStyle.ForeColor = active
                ? ThemeHelper.Success
                : ThemeHelper.Danger;

            e.CellStyle.Font = new Font(
                "Segoe UI",
                9f,
                FontStyle.Bold);

            e.FormattingApplied = true;
        };

        Load += (_, _) => LoadUsers();

        ThemeHelper.Apply(this);
    }

    private static void AddButton(
        Control parent,
        string text,
        EventHandler click)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            Height = 34,
            Margin = new Padding(4)
        };

        button.Click += click;
        parent.Controls.Add(button);
    }

    private User? Selected =>
        _grid.CurrentRow?.DataBoundItem as User;

    private void LoadUsers()
    {
        try
        {
            _grid.DataSource = _users.GetAllUsers().ToList();

            SetColumn("Id", "Mã nhân viên", false);
            SetColumn("Username", "Tên đăng nhập");
            SetColumn("FullName", "Họ và tên");
            SetColumn("Email", "Email");
            SetColumn("Role", "Vai trò");
            SetColumn("IsActive", "Trạng thái");
            SetColumn("PasswordHash", "Mật khẩu", false);
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);
        }
    }

    private void SetColumn(
        string name,
        string header,
        bool visible = true)
    {
        if (!_grid.Columns.Contains(name))
            return;

        _grid.Columns[name]!.HeaderText = header;
        _grid.Columns[name]!.Visible = visible;
    }

    private void AddUser(object? sender, EventArgs e)
    {
        using var dialog = new UserEditorForm();

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            _users.CreateUser(
                dialog.User,
                dialog.InitialPassword);

            _audit.Log(
                "USER_CREATE",
                $"Tạo tài khoản {dialog.User.Username}.");

            LoadUsers();

            MessageHelper.Info("Đã tạo tài khoản nhân viên.");
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);
        }
    }

    private void EditUser(object? sender, EventArgs e)
    {
        var user = Selected;

        if (user is null)
        {
            MessageHelper.Warning("Vui lòng chọn người dùng.");
            return;
        }

        using var dialog = new UserEditorForm(user);

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            var updated = dialog.User;

            updated.Id = user.Id;
            updated.IsActive = user.IsActive;
            updated.CreatedAt = user.CreatedAt;
            updated.PasswordHash = user.PasswordHash;

            _users.UpdateUser(updated);

            _audit.Log(
                "USER_UPDATE",
                $"Cập nhật tài khoản {updated.Username}.");

            LoadUsers();

            MessageHelper.Info("Cập nhật thông tin thành công.");
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);
        }
    }

    private void ResetPassword(object? sender, EventArgs e)
    {
        var user = Selected;

        if (user is null)
        {
            MessageHelper.Warning("Vui lòng chọn người dùng.");
            return;
        }

        var password = Prompt.Show(
            "Nhập mật khẩu mới (ít nhất 8 ký tự, có chữ hoa, chữ thường và số):",
            "Đặt lại mật khẩu");

        if (password is null)
            return;

        try
        {
            _users.ResetPassword(user.Id, password);

            _audit.Log(
                "USER_RESET_PASSWORD",
                $"Đặt lại mật khẩu tài khoản {user.Username}.");

            MessageHelper.Info("Đã đặt lại mật khẩu.");
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);
        }
    }

    private void ToggleActive(object? sender, EventArgs e)
    {
        var user = Selected;

        if (user is null)
        {
            MessageHelper.Warning("Vui lòng chọn người dùng.");
            return;
        }

        if (!MessageHelper.Confirm(
            $"Bạn muốn {(user.IsActive ? "khóa" : "mở khóa")} tài khoản {user.Username}?"))
        {
            return;
        }

        try
        {
            _users.ToggleUserStatus(user.Id);

            _audit.Log(
                "USER_STATUS",
                $"{(user.IsActive ? "Khóa" : "Mở khóa")} tài khoản {user.Username}.");

            LoadUsers();
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);
        }
    }

    private sealed class UserEditorForm : Form
    {
        private readonly TextBox _username = new();
        private readonly TextBox _fullName = new();
        private readonly TextBox _password = new();

        private readonly ComboBox _role = new()
        {
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        public User User { get; private set; } = new();

        public string InitialPassword => _password.Text;

        public UserEditorForm(User? source = null)
        {
            Text = source is null
                ? "Thêm nhân viên"
                : "Sửa nhân viên";

            Size = new Size(440, 390);
            MinimumSize = new Size(440, 390);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            AddField("Tên đăng nhập", _username, 25);
            AddField("Họ và tên", _fullName, 95);

            if (source is null)
            {
                AddField("Mật khẩu ban đầu", _password, 165);
                _password.UseSystemPasswordChar = true;
            }

            _role.SetBounds(
                25,
                source is null ? 245 : 175,
                370,
                32);

            _role.DataSource = Enum.GetValues<UserRole>();

            Controls.Add(_role);

            Controls.Add(new Label
            {
                Text = "Vai trò",
                Location = new Point(
                    25,
                    source is null ? 222 : 152),
                AutoSize = true
            });

            if (source is not null)
            {
                _username.Text = source.Username;
                _fullName.Text = source.FullName;
                _role.SelectedItem = source.Role;
            }
            else
            {
                _role.SelectedItem = UserRole.Staff;
            }

            var ok = new Button
            {
                Text = "Lưu",
                Location = new Point(235, 300),
                Size = new Size(75, 36)
            };

            ok.Click += (_, _) =>
            {
                if (string.IsNullOrWhiteSpace(_username.Text) ||
                    string.IsNullOrWhiteSpace(_fullName.Text))
                {
                    MessageHelper.Warning(
                        "Vui lòng nhập đầy đủ thông tin.");

                    return;
                }

                if (source is null &&
                    string.IsNullOrWhiteSpace(_password.Text))
                {
                    MessageHelper.Warning(
                        "Vui lòng nhập mật khẩu ban đầu.");

                    return;
                }

                if (_role.SelectedItem is not UserRole selectedRole)
                {
                    MessageHelper.Warning(
                        "Vui lòng chọn vai trò.");

                    return;
                }

                User = new User
                {
                    Username = _username.Text.Trim(),
                    FullName = _fullName.Text.Trim(),
                    Role = selectedRole
                };

                DialogResult = DialogResult.OK;
                Close();
            };

            var cancel = new Button
            {
                Text = "Hủy",
                Location = new Point(320, 300),
                Size = new Size(75, 36),
                DialogResult = DialogResult.Cancel
            };

            Controls.AddRange(new Control[]
            {
                ok,
                cancel
            });

            AcceptButton = ok;
            CancelButton = cancel;

            ThemeHelper.Apply(this);
        }

        private void AddField(
            string title,
            TextBox box,
            int y)
        {
            Controls.Add(new Label
            {
                Text = title,
                Location = new Point(25, y),
                AutoSize = true
            });

            box.SetBounds(25, y + 25, 370, 30);
            Controls.Add(box);
        }
    }

    private static class Prompt
    {
        public static string? Show(string text, string title)
        {
            using var form = new Form
            {
                Text = title,
                Size = new Size(430, 180),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var label = new Label
            {
                Text = text,
                Location = new Point(15, 15),
                Size = new Size(380, 40)
            };

            var input = new TextBox
            {
                Location = new Point(15, 60),
                Width = 380,
                UseSystemPasswordChar = true
            };

            var ok = new Button
            {
                Text = "Đồng ý",
                DialogResult = DialogResult.OK,
                Location = new Point(235, 100)
            };

            var cancel = new Button
            {
                Text = "Hủy",
                DialogResult = DialogResult.Cancel,
                Location = new Point(320, 100)
            };

            form.Controls.AddRange(new Control[]
            {
                label,
                input,
                ok,
                cancel
            });

            form.AcceptButton = ok;
            form.CancelButton = cancel;

            return form.ShowDialog() == DialogResult.OK
                ? input.Text
                : null;
        }
    }
}
