
using CinemaManagement.BLL.Session;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.UI.Controls;
using CinemaManagement.UI.Helpers;

namespace CinemaManagement.UI.Forms.Auth;

public partial class frmChangePassword : UserControl
{
    private readonly IAuthService _auth;
    private readonly CurrentUserSession _session;

    private readonly TextBox _old = new();
    private readonly TextBox _next = new();
    private readonly TextBox _confirm = new();

    public frmChangePassword(
        IAuthService auth,
        CurrentUserSession session)
    {
        _auth = auth ?? throw new ArgumentNullException(nameof(auth));
        _session = session ?? throw new ArgumentNullException(nameof(session));

        InitializeComponent();

        Text = "Bảo mật tài khoản";
        BackColor = ThemeHelper.Background;
        Font = ThemeHelper.BodyFont;

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

        var heading = new PageHeader(
            "Bảo mật tài khoản",
            "Đổi mật khẩu đăng nhập của tài khoản hiện tại.");

        _old.AccessibleName = "Mật khẩu hiện tại";
        _next.AccessibleName = "Mật khẩu mới";
        _confirm.AccessibleName = "Xác nhận mật khẩu mới";
        _old.AccessibleDescription = "Nhập mật khẩu đang sử dụng.";
        _next.AccessibleDescription = "Nhập mật khẩu mới.";
        _confirm.AccessibleDescription = "Nhập lại mật khẩu mới để xác nhận.";

        _old.UseSystemPasswordChar = true;
        _next.UseSystemPasswordChar = true;
        _confirm.UseSystemPasswordChar = true;

        var fields = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(12, 14, 12, 8),
            BackColor = ThemeHelper.Surface
        };
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        fields.Controls.Add(BuildField("Mật khẩu hiện tại", _old), 0, 0);
        fields.Controls.Add(BuildField("Mật khẩu mới", _next), 0, 1);
        fields.Controls.Add(BuildField("Xác nhận mật khẩu mới", _confirm), 0, 2);

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 10, 0, 0),
            BackColor = ThemeHelper.Background
        };

        var save = new Button
        {
            Text = "Lưu thay đổi",
            Size = new Size(130, 38),
            Margin = new Padding(6, 0, 0, 0)
        };
        ThemeHelper.StyleActionButton(save, ThemeHelper.ActionButtonRole.Primary);

        save.Click += Save_Click;

        var cancel = new Button
        {
            Text = "Xóa nội dung",
            Size = new Size(90, 38)
        };
        ThemeHelper.StyleActionButton(cancel, ThemeHelper.ActionButtonRole.Secondary);

        cancel.Click += (_, _) => ClearFields();

        footer.Controls.AddRange([save, cancel]);
        root.Controls.Add(heading, 0, 0);
        root.Controls.Add(fields, 0, 1);
        root.Controls.Add(footer, 0, 2);
        Controls.Add(root);

        ThemeHelper.Apply(this);
    }

    private static Control BuildField(
        string caption,
        TextBox box)
    {
        var field = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 76,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(2, 2, 2, 5),
            BackColor = ThemeHelper.Surface
        };
        field.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        field.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));
        field.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var label = new Label
        {
            Text = caption,
            Dock = DockStyle.Fill,
            ForeColor = ThemeHelper.Text,
            TextAlign = ContentAlignment.MiddleLeft
        };
        box.Dock = DockStyle.Fill;
        box.Margin = new Padding(0, 2, 0, 6);
        field.Controls.Add(label, 0, 0);
        field.Controls.Add(box, 0, 1);
        return field;
    }

    private void Save_Click(object? sender, EventArgs e)
    {
        try
        {
            if (_session.User is null)
            {
                MessageHelper.Warning(
                    "Phiên đăng nhập đã hết hạn.");

                ClearFields();
                return;
            }

            if (string.IsNullOrWhiteSpace(_old.Text) ||
                string.IsNullOrWhiteSpace(_next.Text) ||
                string.IsNullOrWhiteSpace(_confirm.Text))
            {
                MessageHelper.Warning(
                    "Vui lòng nhập đầy đủ thông tin.");

                return;
            }

            if (_next.Text != _confirm.Text)
            {
                MessageHelper.Warning(
                    "Mật khẩu xác nhận không khớp.");

                return;
            }

            if (_old.Text == _next.Text)
            {
                MessageHelper.Warning(
                    "Mật khẩu mới phải khác mật khẩu hiện tại.");

                return;
            }

            _auth.ChangePassword(
                _session.User.Id,
                _old.Text,
                _next.Text);

            MessageHelper.Info(
                "Đổi mật khẩu thành công.");

            ClearFields();
        }
        catch (BusinessException ex)
        {
            MessageHelper.Warning(ex.Message);
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);
        }
    }

    private void ClearFields()
    {
        _old.Clear();
        _next.Clear();
        _confirm.Clear();
        _old.Focus();
    }
}
