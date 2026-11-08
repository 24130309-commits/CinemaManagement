using System.Drawing;
using System.Windows.Forms;

namespace CinemaManagement.UI.Forms.Auth;

public partial class frmLogin : Form
{
    private TextBox txtUsername = null!;
    private TextBox txtPassword = null!;
    private Button btnLogin = null!;
    private CheckBox chkShowPassword = null!;

    public frmLogin()
    {
        InitializeUI();
    }

    private void InitializeUI()
    {
        Text = "Cinema Management - Đăng nhập";
        Size = new Size(500, 450);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Color.FromArgb(245, 247, 250);

        var panel = new Panel
        {
            Size = new Size(380, 350),
            Location = new Point(52, 35),
            BackColor = Color.White
        };

        var lblTitle = new Label
        {
            Text = "CINEMA MANAGEMENT",
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 50, 90),
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(20, 25),
            Size = new Size(340, 40)
        };

        var lblSubtitle = new Label
        {
            Text = "Vui lòng đăng nhập để tiếp tục",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.Gray,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(20, 65),
            Size = new Size(340, 25)
        };

        var lblUsername = new Label
        {
            Text = "Tên đăng nhập",
            Location = new Point(35, 115),
            AutoSize = true
        };

        txtUsername = new TextBox
        {
            Location = new Point(35, 140),
            Size = new Size(310, 30),
            Font = new Font("Segoe UI", 11),
            Text = "admin"
        };

        var lblPassword = new Label
        {
            Text = "Mật khẩu",
            Location = new Point(35, 185),
            AutoSize = true
        };

        txtPassword = new TextBox
        {
            Location = new Point(35, 210),
            Size = new Size(310, 30),
            Font = new Font("Segoe UI", 11),
            UseSystemPasswordChar = true
        };

        chkShowPassword = new CheckBox
        {
            Text = "Hiển thị mật khẩu",
            Location = new Point(35, 245),
            AutoSize = true
        };

        chkShowPassword.CheckedChanged += (_, _) =>
        {
            txtPassword.UseSystemPasswordChar =
                !chkShowPassword.Checked;
        };

        btnLogin = new Button
        {
            Text = "ĐĂNG NHẬP",
            Location = new Point(35, 280),
            Size = new Size(310, 42),
            BackColor = Color.FromArgb(30, 50, 90),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            Cursor = Cursors.Hand
        };

        btnLogin.FlatAppearance.BorderSize = 0;
        btnLogin.Click += BtnLogin_Click;

        panel.Controls.AddRange(
        [
            lblTitle,
            lblSubtitle,
            lblUsername,
            txtUsername,
            lblPassword,
            txtPassword,
            chkShowPassword,
            btnLogin
        ]);

        Controls.Add(panel);

        AcceptButton = btnLogin;
    }

    private void BtnLogin_Click(object? sender, EventArgs e)
    {
        string username = txtUsername.Text.Trim();
        string password = txtPassword.Text;

        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password))
        {
            MessageBox.Show(
                "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.",
                "Thông báo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            return;
        }

        // Dữ liệu giả để kiểm thử UI
        if (username == "admin" && password == "123456")
        {
            MessageBox.Show(
                "Đăng nhập thành công!",
                "Thông báo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
            Close();
        }
        else
        {
            MessageBox.Show(
                "Tên đăng nhập hoặc mật khẩu không chính xác.",
                "Đăng nhập thất bại",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}