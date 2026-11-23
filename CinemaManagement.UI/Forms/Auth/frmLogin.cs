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
        Color.FromArgb(7, 13, 26);

    private static readonly Color Surface =
        Color.FromArgb(15, 27, 47);

    private static readonly Color SurfaceLight =
        Color.FromArgb(23, 39, 64);

    private static readonly Color Primary =
        Color.FromArgb(37, 99, 235);

    private static readonly Color PrimaryHover =
        Color.FromArgb(59, 130, 246);

    private static readonly Color TextPrimary =
        Color.FromArgb(241, 245, 249);

    private static readonly Color TextSecondary =
        Color.FromArgb(148, 163, 184);

    private static readonly Color Border =
        Color.FromArgb(43, 62, 91);

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

        Size = new Size(680, 800);
        MinimumSize = new Size(600, 740);

        BackColor = Background;
        ForeColor = TextPrimary;

        Font = new Font("Segoe UI", 10F);

        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        MinimizeBox = true;

        var backgroundPanel = new LoginBackgroundPanel
        {
            Dock = DockStyle.Fill
        };

        Controls.Add(backgroundPanel);

        BuildLoginCard(backgroundPanel);

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

    private void BuildLoginCard(Control parent)
    {
        var card = new Panel
        {
            Size = new Size(430, 640),
            BackColor = Surface,
            Anchor = AnchorStyles.None
        };

        parent.Controls.Add(card);

        void CenterCard()
        {
            card.Location = new Point(
                Math.Max(0, (parent.ClientSize.Width - card.Width) / 2),
                Math.Max(0, (parent.ClientSize.Height - card.Height) / 2));
        }

        parent.Resize += (_, _) => CenterCard();

        CenterCard();

        card.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using var pen = new Pen(Border, 1);

            e.Graphics.DrawRectangle(
                pen,
                0,
                0,
                card.Width - 1,
                card.Height - 1);
        };

        var logoCircle = new Panel
        {
            Size = new Size(76, 76),
            Location = new Point(177, 30),
            BackColor = Color.FromArgb(21, 48, 91)
        };

        logoCircle.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using var brush = new LinearGradientBrush(
                logoCircle.ClientRectangle,
                Color.FromArgb(37, 99, 235),
                Color.FromArgb(30, 64, 175),
                LinearGradientMode.ForwardDiagonal);

            e.Graphics.FillEllipse(
                brush,
                1,
                1,
                73,
                73);

            using var font = new Font(
                "Segoe UI",
                31F,
                FontStyle.Bold);

            TextRenderer.DrawText(
                e.Graphics,
                "C",
                font,
                logoCircle.ClientRectangle,
                Color.White,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter);
        };

        var logoTitle = CreateLabel(
            "CINEMA",
            23F,
            FontStyle.Bold,
            Color.White);

        logoTitle.SetBounds(30, 115, 370, 40);
        logoTitle.TextAlign = ContentAlignment.MiddleCenter;

        var logoSubtitle = CreateLabel(
            "M A N A G E M E N T   S Y S T E M",
            8F,
            FontStyle.Bold,
            Color.FromArgb(96, 165, 250));

        logoSubtitle.SetBounds(30, 153, 370, 25);
        logoSubtitle.TextAlign = ContentAlignment.MiddleCenter;

        var accent = new Panel
        {
            Location = new Point(185, 190),
            Size = new Size(60, 3),
            BackColor = Primary
        };

        var title = CreateLabel(
            "Đăng nhập",
            22F,
            FontStyle.Bold,
            TextPrimary);

        title.SetBounds(35, 220, 360, 40);
        title.TextAlign = ContentAlignment.MiddleCenter;

        var subtitle = CreateLabel(
            "Vui lòng đăng nhập để tiếp tục",
            9F,
            FontStyle.Regular,
            TextSecondary);

        subtitle.SetBounds(35, 260, 360, 28);
        subtitle.TextAlign = ContentAlignment.MiddleCenter;

        var usernameLabel = CreateLabel(
            "TÊN ĐĂNG NHẬP",
            8.5F,
            FontStyle.Bold,
            Color.FromArgb(203, 213, 225));

        usernameLabel.SetBounds(38, 315, 350, 22);

        _username.SetBounds(38, 342, 354, 43);
        StyleTextBox(_username);
        _username.PlaceholderText = "Nhập tên đăng nhập";

        var passwordLabel = CreateLabel(
            "MẬT KHẨU",
            8.5F,
            FontStyle.Bold,
            Color.FromArgb(203, 213, 225));

        passwordLabel.SetBounds(38, 405, 350, 22);

        _password.SetBounds(38, 432, 354, 43);
        StyleTextBox(_password);

        _password.PlaceholderText = "Nhập mật khẩu";
        _password.UseSystemPasswordChar = true;

        StyleCheckBox(_showPassword);

        _showPassword.Text = "Hiện mật khẩu";
        _showPassword.SetBounds(38, 493, 150, 25);

        _showPassword.CheckedChanged += (_, _) =>
        {
            _password.UseSystemPasswordChar =
                !_showPassword.Checked;
        };

        StyleCheckBox(_remember);

        _remember.Text = "Ghi nhớ tài khoản";
        _remember.SetBounds(220, 493, 172, 25);

        _capsLabel = CreateLabel(
            "Caps Lock đang bật",
            8F,
            FontStyle.Bold,
            Color.FromArgb(251, 191, 36));

        _capsLabel.SetBounds(38, 525, 354, 24);
        _capsLabel.Visible = false;

        _errorLabel = CreateLabel(
            "",
            8F,
            FontStyle.Regular,
            Color.FromArgb(248, 113, 113));

        _errorLabel.SetBounds(38, 525, 354, 34);
        _errorLabel.Visible = false;

        _loginButton = new Button
        {
            Text = "ĐĂNG NHẬP",
            Location = new Point(38, 565),
            Size = new Size(354, 46),
            BackColor = Primary,
            ForeColor = Color.White,
            Font = new Font(
                "Segoe UI",
                10F,
                FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false,
            Cursor = Cursors.Hand
        };

        _loginButton.FlatAppearance.BorderSize = 0;

        _loginButton.FlatAppearance.MouseOverBackColor =
            PrimaryHover;

        _loginButton.FlatAppearance.MouseDownBackColor =
            Color.FromArgb(29, 78, 216);

        _loginButton.Click += Login_Click;

        var footer = CreateLabel(
            "●  HỆ THỐNG BẢO MẬT",
            8F,
            FontStyle.Bold,
            Color.FromArgb(96, 165, 250));

        footer.SetBounds(35, 615, 360, 20);
        footer.TextAlign = ContentAlignment.MiddleCenter;

        card.Controls.AddRange(
        [
            logoCircle,
            logoTitle,
            logoSubtitle,
            accent,
            title,
            subtitle,
            usernameLabel,
            _username,
            passwordLabel,
            _password,
            _showPassword,
            _remember,
            _capsLabel,
            _errorLabel,
            _loginButton,
            footer
        ]);
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
            UseCompatibleTextRendering = true
        };
    }

    private static void StyleTextBox(TextBox textBox)
    {
        textBox.Font = new Font("Segoe UI", 10F);
        textBox.ForeColor = TextPrimary;
        textBox.BackColor = SurfaceLight;
        textBox.BorderStyle = BorderStyle.FixedSingle;
        textBox.Margin = Padding.Empty;
    }

    private static void StyleCheckBox(CheckBox checkBox)
    {
        checkBox.AutoSize = false;
        checkBox.BackColor = Surface;
        checkBox.ForeColor = TextSecondary;
        checkBox.Font = new Font("Segoe UI", 8.5F);
        checkBox.Cursor = Cursors.Hand;
        checkBox.FlatStyle = FlatStyle.Standard;
        checkBox.UseVisualStyleBackColor = false;
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
            MessageHelper.Warning(
                "Không thể lưu tùy chọn ghi nhớ tài khoản: " +
                ex.Message);
        }
    }

    private sealed class LoginBackgroundPanel : Panel
    {
        public LoginBackgroundPanel()
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
                    Color.FromArgb(6, 12, 25),
                    Color.FromArgb(13, 28, 52),
                    LinearGradientMode.ForwardDiagonal);

            g.FillRectangle(
                backgroundBrush,
                rect);

            using var watermarkFont = new Font(
                "Segoe UI",
                125F,
                FontStyle.Bold);

            using var watermarkBrush = new SolidBrush(
                Color.FromArgb(12, 96, 165, 250));

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
                Color.FromArgb(16, 96, 165, 250),
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
                Color.FromArgb(14, 37, 99, 235));

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
                    Color.FromArgb(37, 99, 235),
                    Color.FromArgb(96, 165, 250),
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