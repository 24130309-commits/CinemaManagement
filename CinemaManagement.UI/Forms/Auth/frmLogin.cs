
using System;
using System.ComponentModel;
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
    private Label _errorLabel = new();
    private Label _capsLabel = new();
    private RoundedButton _loginButton = null!;
    private RoundedInput _usernameHost = null!;
    private RoundedInput _passwordHost = null!;
    private bool _isLoggingIn;

    private static readonly Color Background = Color.FromArgb(8, 14, 27);
    private static readonly Color BackgroundTop = Color.FromArgb(9, 16, 31);
    private static readonly Color BackgroundBottom = Color.FromArgb(19, 30, 48);
    private static readonly Color CardColor = Color.FromArgb(19, 30, 48);
    private static readonly Color Accent = Color.FromArgb(239, 153, 65);
    private static readonly Color AccentHover = Color.FromArgb(250, 174, 87);
    private static readonly Color TextDark = Color.FromArgb(235, 240, 248);
    private static readonly Color Muted = Color.FromArgb(148, 163, 184);
    private static readonly Color InputBackground = Color.FromArgb(12, 21, 36);
    private static readonly Color InputBorder = Color.FromArgb(48, 64, 85);

    public frmLogin(IAuthService auth)
    {
        _auth = auth ?? throw new ArgumentNullException(nameof(auth));
        BuildUi();
    }

    private void BuildUi()
    {
        Text = "Cinema Management | Đăng nhập";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(460, 720);
        MinimumSize = new Size(460, 720);
        MaximumSize = new Size(460, 720);
        FormBorderStyle = FormBorderStyle.None;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Background;
        ForeColor = TextDark;
        Font = new Font("Segoe UI", 9.5F);
        ThemeHelper.ConfigureDpiScaling(this);
        AccessibleName = "Đăng nhập Cinema Management";
        AccessibleDescription = "Cổng đăng nhập dành cho nhân viên rạp chiếu phim";

        var backdrop = new LoginBackdrop
        {
            Dock = DockStyle.Fill,
            BackColor = Background
        };

        Controls.Add(backdrop);

        var card = new LoginCard
        {
            Size = new Size(366, 570),
            BackColor = CardColor
        };

        backdrop.Controls.Add(card);

        void PositionCard()
        {
            card.Location = new Point(
                Math.Max(12, (backdrop.ClientSize.Width - card.Width) / 2),
                Math.Max(12, (backdrop.ClientSize.Height - card.Height) / 2));

            backdrop.SetShadowBounds(card.Bounds);
            card.ApplyRoundedRegion();
            backdrop.Invalidate();
        }

        backdrop.Resize += (_, _) => PositionCard();
        PositionCard();

        BuildCardContent(card);
        LoadRememberedUsername();

        AcceptButton = _loginButton;
        ActiveControl = _username;

        _username.KeyDown += Username_KeyDown;
        _password.KeyDown += Password_KeyDown;
        _password.KeyUp += (_, _) => UpdateCapsLock();
        _password.Enter += (_, _) => UpdateCapsLock();
        _password.Leave += (_, _) => _capsLabel.Visible = false;
    }

    private void BuildCardContent(Control card)
    {
        var brandMark = new Label
        {
            Text = "C",
            Size = new Size(48, 48),
            Location = new Point(159, 22),
            BackColor = Accent,
            ForeColor = Background,
            Font = new Font("Segoe UI", 23F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };

        brandMark.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using var path = CreateRoundPath(brandMark.ClientRectangle, 13);
            using var brush = new SolidBrush(Accent);

            e.Graphics.FillPath(brush, path);

            TextRenderer.DrawText(
                e.Graphics,
                "C",
                brandMark.Font,
                brandMark.ClientRectangle,
                Background,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        };

        var brand = CreateLabel(
            "CINEMA MANAGEMENT",
            9F,
            FontStyle.Bold,
            Accent);

        brand.TextAlign = ContentAlignment.MiddleCenter;
        brand.SetBounds(28, 77, 310, 22);

        var title = CreateLabel("LOGIN", 23F, FontStyle.Bold, TextDark);
        title.TextAlign = ContentAlignment.MiddleCenter;
        title.SetBounds(28, 106, 310, 42);

        var subtitle = CreateLabel(
            "Chào mừng trở lại!",
            10F,
            FontStyle.Regular,
            Muted);

        subtitle.TextAlign = ContentAlignment.MiddleCenter;
        subtitle.SetBounds(28, 148, 310, 25);

        var usernameLabel = CreateLabel(
            "TÊN ĐĂNG NHẬP",
            9F,
            FontStyle.Bold,
            Muted);

        usernameLabel.SetBounds(30, 196, 306, 20);

        _usernameHost = new RoundedInput
        {
            Size = new Size(306, 46),
            Location = new Point(30, 220),
            FillColor = InputBackground,
            BorderColor = InputBorder,
            Radius = 12
        };

        StyleLoginTextBox(_username);
        _username.PlaceholderText = "Nhập tên đăng nhập";
        _username.AccessibleName = "Tên đăng nhập";
        _username.AccessibleDescription = "Nhập tên tài khoản nhân viên";
        _username.SetBounds(12, 10, 280, 26);

        _usernameHost.Controls.Add(_username);

        var passwordLabel = CreateLabel(
            "MẬT KHẨU",
            9F,
            FontStyle.Bold,
            Muted);

        passwordLabel.SetBounds(30, 280, 306, 20);

        _passwordHost = new RoundedInput
        {
            Size = new Size(306, 46),
            Location = new Point(30, 304),
            FillColor = InputBackground,
            BorderColor = InputBorder,
            Radius = 12
        };

        StyleLoginTextBox(_password);
        _password.PlaceholderText = "Nhập mật khẩu";
        _password.UseSystemPasswordChar = true;
        _password.AccessibleName = "Mật khẩu";
        _password.AccessibleDescription = "Nhập mật khẩu tài khoản";
        _password.SetBounds(12, 10, 280, 26);

        _passwordHost.Controls.Add(_password);

        StyleCheckBox(_remember);
        _remember.Text = "Ghi nhớ tài khoản";
        _remember.SetBounds(30, 361, 180, 26);

        _capsLabel = CreateLabel(
            "Caps Lock đang bật",
            8.5F,
            FontStyle.Bold,
            Accent);

        _capsLabel.SetBounds(30, 392, 306, 22);
        _capsLabel.Visible = false;

        _errorLabel = CreateLabel(
            "",
            8.5F,
            FontStyle.Bold,
            ThemeHelper.Danger);

        _errorLabel.SetBounds(30, 392, 306, 38);
        _errorLabel.Visible = false;
        _errorLabel.AutoEllipsis = true;
        _errorLabel.AccessibleRole = AccessibleRole.Alert;

        _loginButton = new RoundedButton
        {
            Text = "LOGIN",
            Size = new Size(230, 44),
            Location = new Point(68, 448),
            BackColor = Accent,
            ForeColor = Background,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Radius = 12
        };

        _loginButton.HoverColor = AccentHover;
        _loginButton.PressedColor = Color.FromArgb(213, 125, 40);
        _loginButton.AccessibleName = "Login";
        _loginButton.AccessibleDescription = "Xác thực thông tin để mở hệ thống";
        _loginButton.Click += Login_Click;

        var footer = CreateLabel(
            "SECURE CINEMA SYSTEM",
            8F,
            FontStyle.Bold,
            Color.FromArgb(120, 140, 163));

        footer.TextAlign = ContentAlignment.MiddleCenter;
        footer.SetBounds(30, 518, 306, 22);

        card.Controls.AddRange(new Control[]
        {
            brandMark,
            brand,
            title,
            subtitle,
            usernameLabel,
            _usernameHost,
            passwordLabel,
            _passwordHost,
            _remember,
            _capsLabel,
            _errorLabel,
            _loginButton,
            footer
        });
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
            Font = new Font("Segoe UI", fontSize, style),
            ForeColor = foreColor,
            BackColor = Color.Transparent,
            AutoSize = false,
            UseCompatibleTextRendering = false
        };
    }

    private static void StyleCheckBox(CheckBox checkBox)
    {
        checkBox.AutoSize = false;
        checkBox.BackColor = CardColor;
        checkBox.ForeColor = Color.FromArgb(203, 213, 225);
        checkBox.Font = new Font("Segoe UI", 9F);
        checkBox.Cursor = Cursors.Hand;
        checkBox.FlatStyle = FlatStyle.Standard;
        checkBox.UseVisualStyleBackColor = false;
    }

    private static void StyleLoginTextBox(TextBox textBox)
    {
        textBox.Font = new Font("Segoe UI", 10.5F);
        textBox.ForeColor = TextDark;
        textBox.BackColor = InputBackground;
        textBox.BorderStyle = BorderStyle.None;
        textBox.Margin = Padding.Empty;
    }

    private void Login_Click(object? sender, EventArgs e)
    {
        if (_isLoggingIn)
            return;

        if (string.IsNullOrWhiteSpace(_username.Text) ||
            string.IsNullOrEmpty(_password.Text))
        {
            ShowError("Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.");
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
            _auth.Login(_username.Text.Trim(), _password.Text);
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
                _loginButton.Text = "LOGIN";
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

    private void Username_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true;
            _password.Focus();
        }
    }

    private void Password_KeyDown(object? sender, KeyEventArgs e)
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
            Control.IsKeyLocked(Keys.CapsLock) && _password.Focused;

        if (_capsLabel.Visible)
            _errorLabel.Visible = false;
    }

    private static string RememberedUsernameFile =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CinemaManagement",
            "remembered-user.txt");

    private void LoadRememberedUsername()
    {
        try
        {
            if (!File.Exists(RememberedUsernameFile))
                return;

            var username = File.ReadAllText(RememberedUsernameFile).Trim();

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
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, _username.Text.Trim());
            }
            else if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex, "Không thể lưu tùy chọn ghi nhớ tài khoản.");
        }
    }

    private sealed class LoginCard : Panel
    {
        public LoginCard()
        {
            DoubleBuffered = true;
            BackColor = CardColor;
            Padding = Padding.Empty;
        }

        public void ApplyRoundedRegion()
        {
            if (Width <= 0 || Height <= 0)
                return;

            using var path = CreateRoundPath(
                new Rectangle(0, 0, Width, Height),
                22);

            Region = new Region(path);
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            ApplyRoundedRegion();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using var path = CreateRoundPath(
                new Rectangle(1, 1, Width - 3, Height - 3),
                22);

            using var pen = new Pen(Color.FromArgb(44, 59, 81), 1);

            e.Graphics.DrawPath(pen, path);
        }
    }

    private sealed class RoundedInput : Panel
    {
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color FillColor { get; set; } = InputBackground;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color BorderColor { get; set; } = InputBorder;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Radius { get; set; } = 12;

        public RoundedInput()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = FillColor;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var bounds = new Rectangle(0, 0, Width - 1, Height - 1);

            using var path = CreateRoundPath(bounds, Radius);
            using var brush = new SolidBrush(FillColor);
            using var pen = new Pen(BorderColor, 1);

            e.Graphics.FillPath(brush, path);
            e.Graphics.DrawPath(pen, path);
        }
    }

    private sealed class RoundedButton : Button
    {
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Radius { get; set; } = 12;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color HoverColor { get; set; } = AccentHover;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color PressedColor { get; set; } = Color.FromArgb(213, 125, 40);

        private bool _hovered;
        private bool _pressed;

        public RoundedButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            UseVisualStyleBackColor = false;
            DoubleBuffered = true;
            ResizeRedraw = true;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _hovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hovered = false;
            _pressed = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button == MouseButtons.Left)
            {
                _pressed = true;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _pressed = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var bounds = new Rectangle(0, 0, Width - 1, Height - 1);

            using var path = CreateRoundPath(bounds, Radius);
            using var brush = new SolidBrush(
                _pressed ? PressedColor : _hovered ? HoverColor : BackColor);

            e.Graphics.FillPath(brush, path);

            TextRenderer.DrawText(
                e.Graphics,
                Text,
                Font,
                bounds,
                Enabled ? ForeColor : Color.Gray,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    private sealed class LoginBackdrop : Panel
    {
        private Rectangle _shadowBounds;

        public LoginBackdrop()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = Background;
        }

        public void SetShadowBounds(Rectangle bounds)
        {
            _shadowBounds = bounds;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = ClientRectangle;

            if (rect.Width <= 0 || rect.Height <= 0)
                return;

            using var gradient = new LinearGradientBrush(
                rect,
                BackgroundTop,
                BackgroundBottom,
                LinearGradientMode.Vertical);

            g.FillRectangle(gradient, rect);

            using var vignette = new SolidBrush(Color.FromArgb(18, 0, 0, 0));
            g.FillRectangle(vignette, rect);

            for (var layer = 10; layer >= 1; layer--)
            {
                var shadowRect = _shadowBounds;
                shadowRect.Inflate(layer * 2, layer * 2);
                shadowRect.Offset(0, layer);

                using var shadowPath = CreateRoundPath(shadowRect, 24 + layer);
                using var shadowBrush = new SolidBrush(
                    Color.FromArgb(5 + layer * 2, Color.Black));

                g.FillPath(shadowBrush, shadowPath);
            }

            using var ringPen = new Pen(Color.FromArgb(35, Accent), 1.4F);

            for (var i = 0; i < 6; i++)
            {
                var diameter = 165 + i * 35;

                g.DrawEllipse(
                    ringPen,
                    rect.Width - diameter / 2 - 18,
                    30 - diameter / 2,
                    diameter,
                    diameter);
            }

            using var glow = new SolidBrush(Color.FromArgb(23, Accent));

            g.FillEllipse(glow, -115, rect.Height - 245, 340, 310);
            g.FillEllipse(glow, rect.Width - 175, rect.Height - 115, 280, 230);

            using var watermarkFont = new Font("Segoe UI", 160F, FontStyle.Bold);
            using var watermarkBrush = new SolidBrush(Color.FromArgb(10, Color.White));

            g.DrawString(
                "C",
                watermarkFont,
                watermarkBrush,
                new PointF(rect.Width - 180, rect.Height - 190));

            using var linePen = new Pen(Color.FromArgb(38, Accent), 2F);

            for (var i = 0; i < 8; i++)
            {
                var x = 24 + i * 22;

                g.DrawLine(linePen, x, 22, x + 9, 36);
                g.DrawLine(linePen, x, rect.Height - 37, x + 9, rect.Height - 22);
            }
            
        }
    }

    private static GraphicsPath CreateRoundPath(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();

        if (bounds.Width <= 0 || bounds.Height <= 0)
            return path;

        var diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));

        var arc = new Rectangle(bounds.X, bounds.Y, diameter, diameter);

        path.AddArc(arc, 180, 90);

        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);

        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);

        arc.X = bounds.X;
        path.AddArc(arc, 90, 90);

        path.CloseFigure();

        return path;
    }
}
