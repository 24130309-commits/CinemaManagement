using System.Drawing;
using System.Windows.Forms;

namespace CinemaManagement.UI.Helpers
{
    public static class ThemeHelper
    {
        public static readonly Color PrimaryColor = Color.FromArgb(41, 128, 185);
        public static readonly Color DarkSidebarColor = Color.FromArgb(30, 39, 46);
        public static readonly Color LightBackgroundColor = Color.FromArgb(245, 246, 250);

        public static void ApplyDefaultGridStyle(DataGridView dgv)
        {
            dgv.BackgroundColor = Color.White;
            dgv.BorderStyle = BorderStyle.None;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.ReadOnly = true;
            dgv.AllowUserToAddRows = false;
        }
    }
}