
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.UI.Controls;
using CinemaManagement.UI.Helpers;

namespace CinemaManagement.UI.Forms.Management;

public partial class frmUserManage : Form
{
    private readonly IUserService _users;
    private readonly IAuditService _audit;

    private readonly EmptyStateDataGridView _grid = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AutoGenerateColumns = false,
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

        var title = new PageHeader(
            "Nhân viên & tài khoản",
            "Quản lý quyền truy cập, thông tin nhân viên và trạng thái tài khoản.");

        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 88,
            Padding = new Padding(14, 10, 10, 8),
            WrapContents = true,
            AutoScroll = true,
            BackColor = ThemeHelper.Surface
        };

        AddButton(bar, "Thêm nhân viên", AddUser);
        AddButton(bar, "Sửa thông tin", EditUser);
        AddButton(bar, "Đặt lại mật khẩu", ResetPassword);
        AddButton(bar, "Khóa / Mở khóa", ToggleActive);
        AddButton(bar, "Làm mới", (_, _) => LoadUsers());

        ConfigureGridColumns();
        Controls.Add(_grid);
        Controls.Add(bar);
        Controls.Add(title);
        _grid.EmptyStateText = "Chưa có tài khoản nhân viên để hiển thị.";

        _grid.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
            {
                return;
            }

            var columnName = _grid.Columns[e.ColumnIndex].Name;
            if (columnName == nameof(User.IsActive) && e.Value is bool active)
            {
                e.Value = active ? "Đang hoạt động" : "Đã khóa";
                e.CellStyle.ForeColor = active ? ThemeHelper.Success : ThemeHelper.Danger;
                e.CellStyle.BackColor = active ? ThemeHelper.SuccessSoft : ThemeHelper.DangerSoft;
                e.CellStyle.Font = ThemeHelper.SectionFont;
                e.FormattingApplied = true;
            }
            else if (columnName == nameof(User.Role) && e.Value is UserRole role)
            {
                e.Value = role switch
                {
                    UserRole.Admin => "Quản trị viên",
                    UserRole.Manager => "Quản lý",
                    _ => "Nhân viên"
                };

                e.FormattingApplied = true;
            }
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
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);
        }
    }

    private void ConfigureGridColumns()
    {
        AddTextColumn(nameof(User.Username), "Tên đăng nhập", 17);
        AddTextColumn(nameof(User.FullName), "Họ và tên", 23);
        AddTextColumn(nameof(User.Email), "Email", 22);
        AddTextColumn(nameof(User.Role), "Vai trò", 15);
        AddTextColumn(nameof(User.IsActive), "Trạng thái", 15);
        AddTextColumn(nameof(User.CreatedAt), "Ngày tạo", 16, "dd/MM/yyyy");
    }

    private void AddTextColumn(
        string propertyName,
        string header,
        float fillWeight,
        string? format = null)
    {
        var column = new DataGridViewTextBoxColumn
        {
            Name = propertyName,
            DataPropertyName = propertyName,
            HeaderText = header,
            FillWeight = fillWeight,
            MinimumWidth = 80
        };

        if (format is not null)
            column.DefaultCellStyle.Format = format;

        _grid.Columns.Add(column);
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
            this,
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

            Size = new Size(520, source is null ? 520 : 430);
            MinimumSize = new Size(480, source is null ? 460 : 400);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = ThemeHelper.Background;
            Font = ThemeHelper.BodyFont;
            AccessibleRole = AccessibleRole.Dialog;
            AccessibleName = Text;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(16),
                BackColor = ThemeHelper.Background
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));

            var heading = new PageHeader(
                source is null ? "Thêm nhân viên" : "Sửa thông tin nhân viên",
                "Thông tin tài khoản và quyền truy cập.");

            var fields = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 1,
                RowCount = source is null ? 4 : 3,
                Padding = new Padding(10, 14, 10, 8),
                BackColor = ThemeHelper.Surface
            };
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            fields.Controls.Add(CreateEditorField("Tên đăng nhập", _username), 0, 0);
            fields.Controls.Add(CreateEditorField("Họ và tên", _fullName), 0, 1);

            if (source is null)
            {
                _password.UseSystemPasswordChar = true;
                _password.AccessibleName = "Mật khẩu ban đầu";
                _password.AccessibleDescription = "Nhập mật khẩu ban đầu cho tài khoản mới.";
                fields.Controls.Add(CreateEditorField("Mật khẩu ban đầu", _password), 0, 2);
            }

            _role.DataSource = Enum.GetValues<UserRole>();
            _role.SelectedItem = source?.Role ?? UserRole.Staff;
            _role.AccessibleName = "Vai trò tài khoản";
            fields.Controls.Add(
                CreateEditorField("Vai trò", _role),
                0,
                source is null ? 3 : 2);

            if (source is not null)
            {
                _username.Text = source.Username;
                _fullName.Text = source.FullName;
                _role.SelectedItem = source.Role;
            }
            var footer = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Padding = new Padding(0, 10, 0, 0),
                BackColor = ThemeHelper.Background
            };

            var ok = new Button
            {
                Text = "Lưu",
                Size = new Size(110, 38),
                Margin = new Padding(6, 0, 0, 0)
            };
            ThemeHelper.StyleActionButton(ok, ThemeHelper.ActionButtonRole.Primary);

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
                Size = new Size(110, 38),
                DialogResult = DialogResult.Cancel
            };
            ThemeHelper.StyleActionButton(cancel, ThemeHelper.ActionButtonRole.Secondary);

            footer.Controls.AddRange([ok, cancel]);
            root.Controls.Add(heading, 0, 0);
            root.Controls.Add(fields, 0, 1);
            root.Controls.Add(footer, 0, 2);
            Controls.Add(root);

            AcceptButton = ok;
            CancelButton = cancel;

            ThemeHelper.Apply(this);
        }

        private static Control CreateEditorField(
            string title,
            Control input)
        {
            var field = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 72,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(2, 2, 2, 6),
                BackColor = ThemeHelper.Surface
            };
            field.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            field.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));
            field.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var label = new Label
            {
                Text = title,
                Dock = DockStyle.Fill,
                ForeColor = ThemeHelper.Text,
                TextAlign = ContentAlignment.MiddleLeft
            };

            input.Dock = DockStyle.Fill;
            input.Margin = new Padding(0, 2, 0, 6);
            field.Controls.Add(label, 0, 0);
            field.Controls.Add(input, 0, 1);
            return field;
        }
    }

    private static class Prompt
    {
        public static string? Show(Form owner, string text, string title)
        {
            using var form = new Form
            {
                Text = title,
                Size = new Size(560, 300),
                MinimumSize = new Size(500, 270),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false,
                BackColor = ThemeHelper.Background,
                Font = ThemeHelper.BodyFont,
                AccessibleRole = AccessibleRole.Dialog,
                AccessibleName = title
            };

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(16),
                BackColor = ThemeHelper.Background
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));

            var header = new PageHeader(
                title,
                "Nhập thông tin xác thực mới cho tài khoản đã chọn.");

            var content = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(14, 12, 14, 8),
                BackColor = ThemeHelper.Surface
            };
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 45));

            var label = new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                ForeColor = ThemeHelper.Text,
                AutoEllipsis = true,
                AccessibleRole = AccessibleRole.StaticText
            };

            var input = new TextBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 4, 0, 8),
                UseSystemPasswordChar = true,
                AccessibleName = "Mật khẩu mới",
                AccessibleDescription = "Nhập mật khẩu mới cho tài khoản đã chọn."
            };

            content.Controls.Add(label, 0, 0);
            content.Controls.Add(input, 0, 1);

            var footer = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Padding = new Padding(0, 10, 0, 0),
                BackColor = ThemeHelper.Background
            };

            var ok = new Button
            {
                Text = "Đồng ý",
                DialogResult = DialogResult.OK,
                Size = new Size(110, 38),
                Margin = new Padding(6, 0, 0, 0)
            };
            ThemeHelper.StyleActionButton(ok, ThemeHelper.ActionButtonRole.Primary);

            var cancel = new Button
            {
                Text = "Hủy",
                DialogResult = DialogResult.Cancel,
                Size = new Size(90, 38)
            };
            ThemeHelper.StyleActionButton(cancel, ThemeHelper.ActionButtonRole.Secondary);

            footer.Controls.AddRange([ok, cancel]);
            root.Controls.Add(header, 0, 0);
            root.Controls.Add(content, 0, 1);
            root.Controls.Add(footer, 0, 2);
            form.Controls.Add(root);

            form.AcceptButton = ok;
            form.CancelButton = cancel;

            ThemeHelper.Apply(form);
            ThemeHelper.FitToWorkingArea(form, new Size(560, 300), new Size(500, 270));

            return form.ShowDialog(owner) == DialogResult.OK
                ? input.Text
                : null;
        }
    }
}
