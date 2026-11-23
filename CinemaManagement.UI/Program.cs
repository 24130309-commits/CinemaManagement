using System;
using System.Windows.Forms;

namespace CinemaManagement.UI;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        Application.Run(CreateLoginForm());
    }

    private static Form CreateLoginForm()
    {
        throw new NotImplementedException(
            "Hãy kết nối frmLogin với Dependency Injection tại đây.");
    }
}