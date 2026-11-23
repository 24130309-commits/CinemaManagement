
using CinemaManagement.BLL.Session;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.UI.Helpers;

namespace CinemaManagement.UI.Forms.Auth;

public partial class frmChangePassword : Form
{
    private readonly IAuthService _auth;
    private readonly CurrentUserSession _session;
    private readonly IAuditService _audit;

    private readonly TextBox _old = new();
    private readonly TextBox _next = new();
    private readonly TextBox _confirm = new();

    public frmChangePassword(
        IAuthService auth,
        CurrentUserSession session,
        IAuditService audit)
    {
        _auth = auth ?? throw new ArgumentNullException(nameof(auth));
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));

        InitializeComponent();

        Text = "Bảo mật tài khoản";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(560, 470);
        Size = new Size(560, 470);
        BackColor = ThemeHelper.Background;

        Controls.Add(new Label
        {
            Text = "ĐỔI MẬT KHẨU",
            Location = new Point(35, 22),
            Size = new Size(450, 32),
            Font = ThemeHelper.HeadingFont,
            ForeColor = ThemeHelper.Primary
        });

        Controls.Add(new Label
        {
            Text = "Sử dụng mật khẩu mạnh để bảo vệ tài khoản của bạn.",
            Location = new Point(37, 52),
            Size = new Size(450, 22),
            Font = ThemeHelper.SmallFont,
            ForeColor = ThemeHelper.Muted
        });

        BuildField("Mật khẩu hiện tại", _old, 95);
        BuildField("Mật khẩu mới", _next, 170);
        BuildField("Xác nhận mật khẩu mới", _confirm, 245);

        _old.UseSystemPasswordChar = true;
        _next.UseSystemPasswordChar = true;
        _confirm.UseSystemPasswordChar = true;

        var save = new Button
        {
            Text = "Lưu thay đổi",
            Location = new Point(300, 345),
            Size = new Size(130, 38)
        };

        save.Click += Save_Click;

        var cancel = new Button
        {
            Text = "Hủy",
            Location = new Point(440, 345),
            Size = new Size(75, 38)
        };

        cancel.Click += (_, _) => Close();

        Controls.AddRange([save, cancel]);

        AcceptButton = save;
        CancelButton = cancel;

        ThemeHelper.Apply(this);
    }

    private void BuildField(
        string caption,
        TextBox box,
        int top)
    {
        var label = new Label
        {
            Text = caption,
            Location = new Point(35, top),
            AutoSize = true
        };

        box.SetBounds(35, top + 25, 450, 32);

        Controls.AddRange([label, box]);
    }

    private void Save_Click(object? sender, EventArgs e)
    {
        try
        {
            if (_session.User is null)
            {
                MessageHelper.Warning(
                    "Phiên đăng nhập đã hết hạn.");

                Close();
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

            _audit.Log(
                "CHANGE_PASSWORD",
                "Người dùng đổi mật khẩu.");

            MessageHelper.Info(
                "Đổi mật khẩu thành công.");

            Close();
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
}
