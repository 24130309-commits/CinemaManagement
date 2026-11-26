#nullable enable
using System.ComponentModel;

namespace CinemaManagement.UI.Forms.Main;

partial class frmDashboard
{
    private IContainer? components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new Container();

        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(800, 450);
        Text = "Dashboard";
        StartPosition = FormStartPosition.CenterScreen;
    }

    #endregion
}