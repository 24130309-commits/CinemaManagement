using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.UI.Helpers;

namespace CinemaManagement.UI.Forms.Auth;

public partial class frmLogin : Form
{
    private readonly IAuthService _auth;

    private readonly TextBox _username = new();
    private readonly TextBox _password = new();

    private readonly CheckBox _remember = new();
    private readonly CheckBox _showPassword = new();

    private Label _errorLabel = new();
    private Label _capsLabel = new();

    private Button _loginButton = null!;
    private bool _isLoggingIn;

    private static readonly Color Background =
        ThemeHelper.PrimaryDark;

    private static readonly Color Primary =
        ThemeHelper.Structural;

    private static readonly Color PrimaryHover =
        ThemeHelper.AccentHover;

    private static readonly Color TextPrimary =
        Color.FromArgb(241, 245, 249);

    private static readonly Color Border =
        ThemeHelper.StructuralSoft;

    public frmLogin(IAuthService auth)
    {
        _auth = auth
            ?? throw new ArgumentNullException(nameof(auth));

        BuildUi();
    }

    private void BuildUi()
    {
        Text = "Cinema Management | Đăng nhập";
        StartPosition = FormStartPosition.CenterScreen;

        Size = new Size(980, 680);
        MinimumSize = new Size(760, 560);
        ThemeHelper.ConfigureDpiScaling(this);
        AccessibleName = "Đăng nhập Cinema Management";
        AccessibleDescription = "Cổng đăng nhập dành cho nhân viên rạp chiếu phim";

        BackColor = Background;
        ForeColor = TextPrimary;

        Font = new Font("Segoe UI", 10F);

        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        MinimizeBox = true;

        var shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = Background
        };
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 39));
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 61));

        var brand = new LoginBrandPanel { Dock = DockStyle.Fill };
        BuildBrandContent(brand);

        var signInArea = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(243, 246, 250),
            Padding = new Padding(20)
        };

        var card = BuildLoginCard();
        signInArea.Controls.Add(card);

        void CenterCard()
        {
            card.Width = Math.Clamp(signInArea.ClientSize.Width - 24, 340, 430);
            card.Height = Math.Clamp(signInArea.ClientSize.Height - 24, 460, 480);
            card.Location = new Point(
                Math.Max(0, (signInArea.ClientSize.Width - card.Width) / 2),
                Math.Max(0, (signInArea.ClientSize.Height - card.Height) / 2));
        }

        signInArea.Resize += (_, _) => CenterCard();
        CenterCard();

        shell.Controls.Add(brand, 0, 0);
        shell.Controls.Add(signInArea, 1, 0);

        Controls.Add(shell);

        LoadRememberedUsername();

        AcceptButton = _loginButton;
        ActiveControl = _username;

        _username.KeyDown += Username_KeyDown;
        _password.KeyDown += Password_KeyDown;

        _password.KeyUp += (_, _) => UpdateCapsLock();
        _password.Enter += (_, _) => UpdateCapsLock();
        _password.Leave += (_, _) =>
        {
            _capsLabel.Visible = false;
        };
    }

    private static void BuildBrandContent(Control parent)
    {
        var brandTitle = CreateLabel(
            "CINEMA",
            16F,
            FontStyle.Bold,
            Color.White);
        brandTitle.TextAlign = ContentAlignment.MiddleLeft;

        var brandSubtitle = CreateLabel(
            "MANAGEMENT",
            13F,
            FontStyle.Bold,
            Color.FromArgb(203, 213, 225));
        brandSubtitle.TextAlign = ContentAlignment.MiddleLeft;

        var mark = new Label
        {
            Text = "C",
            Location = new Point(30, 36),
            Size = new Size(54, 54),
            BackColor = ThemeHelper.Accent,
            ForeColor = ThemeHelper.Primary,
            Font = new Font("Segoe UI", 23F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            AccessibleName = "Biểu trưng Cinema Management"
        };

        parent.Controls.AddRange([brandTitle, brandSubtitle, mark]);

        void LayoutBrand()
        {
            var width = Math.Max(220, parent.ClientSize.Width);
            var nameWidth = Math.Clamp(width - 104, 190, 260);
            var groupWidth = 72 + nameWidth;
            var left = Math.Max(12, (width - groupWidth) / 2);
            var top = Math.Max(32, (parent.ClientSize.Height - 54) / 2);
            mark.Location = new Point(left, top);
            brandTitle.SetBounds(left + 72, top - 4, nameWidth, 38);
            brandSubtitle.SetBounds(left + 72, top + 30, nameWidth, 34);
        }

        parent.Resize += (_, _) => LayoutBrand();
        LayoutBrand();
    }

    private Panel BuildLoginCard()
    {
        const int cardWidth = 430;
        const int cardHeight = 480;
        var card = new Panel
        {
            Size = new Size(cardWidth, cardHeight),
            BackColor = Color.White,
            Padding = new Padding(34),
            AccessibleName = "Đăng nhập Cinema Management"
        };
        card.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var border = new Pen(Border);
            e.Graphics.DrawRectangle(border, 0, 0, card.Width - 1, card.Height - 1);
            using var accent = new SolidBrush(Primary);
            e.Graphics.FillRectangle(accent, 0, 0, 5, card.Height);
        };

        var title = CreateLabel("Đăng nhập", 20F, FontStyle.Bold, ThemeHelper.Primary);
        title.TextAlign = ContentAlignment.MiddleLeft;
        title.SetBounds(34, 26, 360, 54);

        var usernameLabel = CreateLabel("TÊN ĐĂNG NHẬP", 9F, FontStyle.Bold, Color.FromArgb(71, 85, 105));
        usernameLabel.SetBounds(36, 103, 350, 20);

        StyleLoginTextBox(_username);
        _username.SetBounds(36, 126, 358, 42);
        _username.PlaceholderText = "Nhập tên đăng nhập";
        _username.AccessibleName = "Tên đăng nhập";
        _username.AccessibleDescription = "Nhập tên tài khoản nhân viên";

        var passwordLabel = CreateLabel("MẬT KHẨU", 9F, FontStyle.Bold, Color.FromArgb(71, 85, 105));
        passwordLabel.SetBounds(36, 189, 350, 20);

        StyleLoginTextBox(_password);
        _password.SetBounds(36, 212, 358, 42);
        _password.PlaceholderText = "Nhập mật khẩu";
        _password.UseSystemPasswordChar = true;
        _password.AccessibleName = "Mật khẩu";
        _password.AccessibleDescription = "Nhập mật khẩu tài khoản";

        StyleCheckBox(_showPassword);
        _showPassword.Text = "Hiện mật khẩu";
        _showPassword.SetBounds(36, 270, 145, 26);
        _showPassword.CheckedChanged += (_, _) => _password.UseSystemPasswordChar = !_showPassword.Checked;

        StyleCheckBox(_remember);
        _remember.Text = "Ghi nhớ tài khoản";
        _remember.SetBounds(218, 270, 176, 26);

        _capsLabel = CreateLabel("Caps Lock đang bật", 8F, FontStyle.Bold, Color.FromArgb(156, 93, 20));
        _capsLabel.SetBounds(36, 308, 358, 24);
        _capsLabel.Visible = false;

        _errorLabel = CreateLabel("", 8.5F, FontStyle.Bold, ThemeHelper.Danger);
        _errorLabel.SetBounds(36, 308, 358, 38);
        _errorLabel.Visible = false;
        _errorLabel.AutoEllipsis = true;
        _errorLabel.AccessibleRole = AccessibleRole.Alert;

        _loginButton = new Button
        {
            Text = "ĐĂNG NHẬP",
            Location = new Point(36, 365),
            Size = new Size(358, 48),
            BackColor = ThemeHelper.Accent,
            ForeColor = ThemeHelper.Primary,
            Font = new Font(
                "Segoe UI",
                10F,
                FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false,
            Cursor = Cursors.Hand
        };

        _loginButton.FlatAppearance.BorderSize = 0;
        _loginButton.AccessibleName = "Đăng nhập";
        _loginButton.AccessibleDescription = "Xác thực thông tin để mở hệ thống";

        _loginButton.FlatAppearance.MouseOverBackColor =
            PrimaryHover;

        _loginButton.FlatAppearance.MouseDownBackColor =
            ThemeHelper.AccentHover;

        _loginButton.Click += Login_Click;

        card.Controls.AddRange(
        [
            title,
            usernameLabel,
            _username,
            passwordLabel,
            _password,
            _showPassword,
            _remember,
            _capsLabel,
            _errorLabel,
            _loginButton
        ]);

        card.Resize += (_, _) =>
        {
            var width = Math.Max(300, card.ClientSize.Width - 72);
            foreach (var control in new Control[] { usernameLabel, _username, passwordLabel, _password, _capsLabel, _errorLabel, _loginButton })
                control.Width = width;
            _remember.Left = Math.Max(190, card.ClientSize.Width - 212);
        };
        return card;
    }

    private static Label CreateLabel(
        string text,
        float fontSize,
        FontStyle style,
        Color foreColor)
    {
        return new Label
        {
            Text = text,
            Font = new Font(
                "Segoe UI",
                fontSize,
                style),
            ForeColor = foreColor,
            BackColor = Color.Transparent,
            AutoSize = false,
            // Use WinForms' native text renderer to avoid GDI+ baseline clipping
            // and stray strokes on large Vietnamese glyphs at scaled DPI settings.
            UseCompatibleTextRendering = false
        };
    }

    private static void StyleCheckBox(CheckBox checkBox)
    {
        checkBox.AutoSize = false;
        checkBox.BackColor = Color.White;
        checkBox.ForeColor = Color.FromArgb(71, 85, 105);
        checkBox.Font = new Font("Segoe UI", 9.5F);
        checkBox.Cursor = Cursors.Hand;
        checkBox.FlatStyle = FlatStyle.Standard;
        checkBox.UseVisualStyleBackColor = false;
    }

    private static void StyleLoginTextBox(TextBox textBox)
    {
        textBox.Font = new Font("Segoe UI", 11F);
        textBox.ForeColor = ThemeHelper.Text;
        textBox.BackColor = Color.White;
        textBox.BorderStyle = BorderStyle.FixedSingle;
        textBox.Margin = Padding.Empty;
    }

    private void Login_Click(object? sender, EventArgs e)
    {
        if (_isLoggingIn)
            return;

        if (string.IsNullOrWhiteSpace(_username.Text) ||
            string.IsNullOrEmpty(_password.Text))
        {
            ShowError(
                "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.");

            return;
        }

        _isLoggingIn = true;

        _loginButton.Enabled = false;
        _loginButton.Text = "ĐANG XÁC THỰC...";

        UseWaitCursor = true;

        _errorLabel.Visible = false;
        _capsLabel.Visible = false;

        try
        {
            _auth.Login(
                _username.Text.Trim(),
                _password.Text);

            SaveRememberedUsername();

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (BusinessException ex)
        {
            ShowError(ex.Message);
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);
        }
        finally
        {
            _isLoggingIn = false;

            if (!IsDisposed)
            {
                UseWaitCursor = false;

                _loginButton.Enabled = true;
                _loginButton.Text = "ĐĂNG NHẬP";
            }
        }
    }

    private void ShowError(string message)
    {
        _errorLabel.Text = "⚠  " + message;
        _errorLabel.Visible = true;
        _capsLabel.Visible = false;

        _password.Focus();
        _password.SelectAll();
    }

    private void Username_KeyDown(
        object? sender,
        KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true;
            _password.Focus();
        }
    }

    private void Password_KeyDown(
        object? sender,
        KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true;
            Login_Click(sender, EventArgs.Empty);
        }
    }

    private void UpdateCapsLock()
    {
        _capsLabel.Visible =
            Control.IsKeyLocked(Keys.CapsLock) &&
            _password.Focused;

        if (_capsLabel.Visible)
            _errorLabel.Visible = false;
    }

    private static string RememberedUsernameFile =>
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "CinemaManagement",
            "remembered-user.txt");

    private void LoadRememberedUsername()
    {
        try
        {
            if (!File.Exists(RememberedUsernameFile))
                return;

            var username = File.ReadAllText(
                RememberedUsernameFile).Trim();

            if (!string.IsNullOrWhiteSpace(username))
            {
                _username.Text = username;
                _remember.Checked = true;
            }
        }
        catch
        {
            _remember.Checked = false;
        }
    }

    private void SaveRememberedUsername()
    {
        try
        {
            var path = RememberedUsernameFile;

            if (_remember.Checked)
            {
                Directory.CreateDirectory(
                    Path.GetDirectoryName(path)!);

                File.WriteAllText(
                    path,
                    _username.Text.Trim());
            }
            else if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(
                ex,
                "Không thể lưu tùy chọn ghi nhớ tài khoản.");
        }
    }

    private sealed class LoginBrandPanel : Panel
    {
        public LoginBrandPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = Background;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;

            g.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = ClientRectangle;

            if (rect.Width <= 0 || rect.Height <= 0)
                return;

            using var backgroundBrush =
                new LinearGradientBrush(
                    rect,
                    ThemeHelper.PrimaryDark,
                    ThemeHelper.Structural,
                    LinearGradientMode.ForwardDiagonal);

            g.FillRectangle(
                backgroundBrush,
                rect);

            using var watermarkFont = new Font(
                "Segoe UI",
                125F,
                FontStyle.Bold);

            using var watermarkBrush = new SolidBrush(
                Color.FromArgb(12, ThemeHelper.Accent));

            g.DrawString(
                "C",
                watermarkFont,
                watermarkBrush,
                new PointF(
                    rect.Width - 180,
                    rect.Height - 250));

            int centerX = rect.Width - 70;
            int centerY = 100;

            using var ringPen = new Pen(
                Color.FromArgb(16, ThemeHelper.Accent),
                2F);

            for (int i = 0; i < 4; i++)
            {
                int size = 120 + i * 55;

                g.DrawEllipse(
                    ringPen,
                    centerX - size / 2,
                    centerY - size / 2,
                    size,
                    size);
            }

            using var glowBrush = new SolidBrush(
                Color.FromArgb(18, ThemeHelper.Accent));

            g.FillEllipse(
                glowBrush,
                -120,
                rect.Height - 180,
                400,
                300);

            using var accentBrush =
                new LinearGradientBrush(
                    new Rectangle(
                        0,
                        0,
                        rect.Width,
                        3),
                    ThemeHelper.Accent,
                    ThemeHelper.AccentHover,
                    LinearGradientMode.Horizontal);

            g.FillRectangle(
                accentBrush,
                0,
                0,
                rect.Width,
                3);
        }
    }
}
