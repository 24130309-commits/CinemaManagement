using System;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.UI.Controls;

namespace CinemaManagement.UI.Forms.Main
{
    public partial class frmDashboard : Form
    {
        public frmDashboard(IReportService reportService)
        {
            Padding = new Padding(20);

            var revenues = reportService.GetRevenueByMovie(DateTime.Today.AddDays(-30), DateTime.Today).ToList();

            var pnlCards = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 120 };
            pnlCards.Controls.Add(new StatCard
                { Title = "DOANH THU 30 NGÀY", Value = $"{revenues.Sum(x => x.TotalRevenue):N0} đ" });
            pnlCards.Controls.Add(new StatCard
                { Title = "VÉ ĐÃ BÁN", Value = revenues.Sum(x => x.TotalTicketsSold).ToString("N0") });
            pnlCards.Controls.Add(new StatCard
                { Title = "TỔNG SUẤT CHIẾU", Value = revenues.Sum(x => x.TotalShowtimes).ToString("N0") });

            var dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                DataSource = revenues
            };

            Controls.Add(dgv);
            Controls.Add(pnlCards);
        }
    }
}
 