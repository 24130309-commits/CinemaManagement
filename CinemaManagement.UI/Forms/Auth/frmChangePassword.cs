using System;
using System.Drawing;
using System.Windows.Forms;
using CinemaManagement.BLL;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.UI.Helpers;

namespace CinemaManagement.UI.Forms.Auth
{
    public partial class frmChangePassword : Form
    {
        private readonly IAuthService _authService;
        private TextBox txtOld, txtNew, txtConfirm;

        public frmChangePassword(IAuthService authService)
        {
            _authService = authService;
            Text = "Đổi mật khẩu";
            Size = new Size(360, 280);
            StartPosition = FormStartPosition.CenterParent;

            Controls.Add(new Label { Text = "Mật khẩu cũ:", Location = new Point(30, 20), AutoSize = true });
            txtOld = new TextBox { Location = new Point(30, 42), Width = 280, PasswordChar = '*' };

            Controls.Add(new Label { Text = "Mật khẩu mới:", Location = new Point(30, 80), AutoSize = true });
            txtNew = new TextBox { Location = new Point(30, 102), Width = 280, PasswordChar = '*' };

            Controls.Add(new Label { Text = "Xác nhận mật khẩu:", Location = new Point(30, 140), AutoSize = true });
            txtConfirm = new TextBox { Location = new Point(30, 162), Width = 280, PasswordChar = '*' };

            var btnSave = new Button { Text = "Lưu thay đổi", Location = new Point(30, 200), Width = 280, Height = 32 };
            btnSave.Click += (s, e) =>
            {
                try
                {
                    if (txtNew.Text != txtConfirm.Text) throw new BusinessException("Mật khẩu xác nhận không khớp.");
                    _authService.ChangePassword(CurrentUserSession.CurrentUser.Id, txtOld.Text, txtNew.Text);
                    MessageHelper.ShowInfo("Đổi mật khẩu thành công!");
                    Close();
                }
                catch (BusinessException ex) { MessageHelper.ShowWarning(ex.Message); }
            };

            Controls.AddRange(new Control[] { txtOld, txtNew, txtConfirm, btnSave });
        }
    }
}