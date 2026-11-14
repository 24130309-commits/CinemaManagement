using System.Drawing;
using System.Windows.Forms;
using CinemaManagement.Core.Constants;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.UI.Helpers;

namespace CinemaManagement.UI.Forms.Reports
{
    public partial class frmSettings : Form
    {
        public frmSettings(ISettingService settingService, IBackupService backupService)
        {
            Padding = new Padding(25);
            Controls.Add(new Label { Text = "Tên rạp chiếu phim:", Location = new Point(25, 25), AutoSize = true });
            var txtCinemaName = new TextBox { Location = new Point(25, 48), Width = 300, Text = settingService.GetValue(AppConstants.SettingCinemaName, "Galaxy Cinema") };

            var btnSave = new Button { Text = "Lưu cấu hình", Location = new Point(25, 85), Width = 140, Height = 32 };
            btnSave.Click += (s, e) =>
            {
                settingService.SaveSetting(AppConstants.SettingCinemaName, txtCinemaName.Text);
                MessageHelper.ShowInfo("Đã lưu cấu hình!");
            };

            var btnBackup = new Button { Text = "Sao lưu Database (.db)", Location = new Point(25, 145), Width = 180, Height = 35 };
            btnBackup.Click += (s, e) =>
            {
                using var sfd = new SaveFileDialog { Filter = "SQLite DB|*.db", FileName = "CinemaBackup.db" };
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    backupService.BackupDatabase(sfd.FileName);
                    MessageHelper.ShowInfo("Sao lưu cơ sở dữ liệu thành công!");
                }
            };

            var btnRestore = new Button { Text = "Phục hồi Database (.db)", Location = new Point(220, 145), Width = 180, Height = 35 };
            btnRestore.Click += (s, e) =>
            {
                using var ofd = new OpenFileDialog { Filter = "SQLite DB|*.db" };
                if (ofd.ShowDialog() == DialogResult.OK && MessageHelper.Confirm("Phục hồi sẽ ghi đè dữ liệu hiện tại. Tiếp tục?"))
                {
                    backupService.RestoreDatabase(ofd.FileName);
                    MessageHelper.ShowInfo("Phục hồi thành công! Ứng dụng sẽ khởi động lại.");
                    Application.Restart();
                }
            };

            Controls.AddRange(new Control[] { txtCinemaName, btnSave, btnBackup, btnRestore });
        }
    }
}