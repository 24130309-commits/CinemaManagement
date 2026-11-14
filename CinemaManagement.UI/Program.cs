using System;
using System.Windows.Forms;
using CinemaManagement.UI.Forms.Auth;

namespace CinemaManagement.UI.Forms.Main
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new frmLogin());
        }
    }
}