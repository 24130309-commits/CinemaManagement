using System;
using System.Drawing;
using System.Windows.Forms;
using CinemaManagement.UI.Controls;
using CinemaManagement.UI.Helpers;

namespace CinemaManagement.UI.Forms.Management;

/// <summary>
/// Gộp quản lý khuyến mãi và quy tắc giá vé.
/// </summary>
public sealed class frmSalesRulesHub : Form
{
    private readonly FormFactory _formFactory;

    public frmSalesRulesHub(FormFactory formFactory)
    {
        ThemeHelper.ConfigureDpiScaling(this);
        _formFactory = formFactory
                       ?? throw new ArgumentNullException(nameof(formFactory));

        BuildLayout();
    }

    private void BuildLayout()
    {
        Text = "Khuyến mãi và giá vé";
        BackColor = ThemeHelper.Surface;
        MinimumSize = new Size(900, 600);
        Dock = DockStyle.Fill;

        var header = new PageHeader(
            "Khuyến mãi & giá vé",
            "Quản lý chương trình ưu đãi và quy tắc giá đang được áp dụng.");

        var tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Padding = new Point(12, 6),
            AccessibleName = "Quản lý khuyến mãi và quy tắc giá",
            AccessibleRole = AccessibleRole.PageTabList
        };

        tabs.TabPages.Add(
            CreatePage(
                "Khuyến mãi",
                _formFactory.Create<frmPromotionManage>()));

        tabs.TabPages.Add(
            CreatePage(
                "Quy tắc giá vé",
                _formFactory.Create<frmPricingRule>()));

        Controls.Add(tabs);
        Controls.Add(header);
        ThemeHelper.Apply(this);
    }

    private static TabPage CreatePage(string title, Control child)
    {
        var page = new TabPage(title)
        {
            BackColor = ThemeHelper.Surface,
            AccessibleName = title
        };

        child.Dock = DockStyle.Fill;

        page.Controls.Add(child);

        return page;
    }
}
