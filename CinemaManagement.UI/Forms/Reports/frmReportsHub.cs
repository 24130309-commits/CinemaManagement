using System;
using System.Drawing;
using System.Windows.Forms;
using CinemaManagement.UI.Controls;
using CinemaManagement.UI.Helpers;

namespace CinemaManagement.UI.Forms.Reports;

/// <summary>
/// Gộp báo cáo doanh thu và tỷ lệ lấp đầy.
/// </summary>
public sealed class frmReportsHub : Form
{
    private readonly FormFactory _formFactory;

    public frmReportsHub(FormFactory formFactory)
    {
        ThemeHelper.ConfigureDpiScaling(this);
        _formFactory = formFactory
                       ?? throw new ArgumentNullException(nameof(formFactory));

        BuildLayout();
    }

    private void BuildLayout()
    {
        Text = "Báo cáo tổng hợp";
        BackColor = Color.White;
        MinimumSize = new Size(900, 600);
        Dock = DockStyle.Fill;

        var header = new PageHeader(
            "Báo cáo & phân tích",
            "Theo dõi hiệu quả doanh thu và công suất sử dụng ghế.");

        var tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Padding = new Point(12, 6)
        };

        tabs.TabPages.Add(
            CreatePage(
                "Doanh thu",
                _formFactory.Create<frmRevenueReport>()));

        tabs.TabPages.Add(
            CreatePage(
                "Tỷ lệ lấp đầy",
                _formFactory.Create<frmOccupancyReport>()));

        Controls.Add(tabs);
        Controls.Add(header);
        ThemeHelper.Apply(this);
    }

    private static TabPage CreatePage(string title, Control child)
    {
        var page = new TabPage(title)
        {
            BackColor = Color.White
        };

        child.Dock = DockStyle.Fill;

        page.Controls.Add(child);

        return page;
    }
}
