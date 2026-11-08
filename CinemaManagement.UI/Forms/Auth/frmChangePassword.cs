using System;
using System.Windows.Forms;

namespace CinemaManagement.UI.Forms.Auth;

public partial class frmChangePassword : Form
{
    // Mật khẩu giả để kiểm thử UI
    private const string CurrentPassword = "123456";

    public frmChangePassword()
    {
        InitializeComponent();
    }

    private void btnSave_Click(object sender, EventArgs e)
    {
        string oldPassword = txtOldPassword.Text;
        string newPassword = txtNewPassword.Text;
        string confirmPassword = txtConfirmPassword.Text;

        if (string.IsNullOrWhiteSpace(oldPassword) ||
            string.IsNullOrWhiteSpace(newPassword) ||
            string.IsNullOrWhiteSpace(confirmPassword))
        {
            MessageBox.Show("Vui lòng nhập đầy đủ thông tin.");
            return;
        }

        if (oldPassword != CurrentPassword)
        {
            MessageBox.Show("Mật khẩu hiện tại không chính xác.");
            return;
        }

        if (newPassword.Length < 6)
        {
            MessageBox.Show("Mật khẩu mới phải có ít nhất 6 ký tự.");
            return;
        }

        if (newPassword != confirmPassword)
        {
            MessageBox.Show("Mật khẩu xác nhận không khớp.");
            return;
        }

        MessageBox.Show(
            "Đổi mật khẩu thành công!",
            "Thông báo",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);

        Close();
    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
        Close();
    }

    private void frmChangePassword_Load(object sender, EventArgs e)
    {
        throw new System.NotImplementedException();
    }
}