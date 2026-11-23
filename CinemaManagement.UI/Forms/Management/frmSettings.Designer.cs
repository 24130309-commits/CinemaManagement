using System.ComponentModel;

namespace CinemaManagement.UI.Forms.Management;

partial class frmSettings
{
    private IContainer components = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null)
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new Container();

        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new System.Drawing.Size(1050, 720);
        Text = "Cài đặt hệ thống";
    }
}