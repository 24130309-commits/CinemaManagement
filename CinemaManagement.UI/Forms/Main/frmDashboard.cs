using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.UI.Controls;

namespace CinemaManagement.UI.Forms
{
    public partial class frmDashboard : Form
    {
        public frmDashboard(IReportService reportService, IAuditService auditService)
        {
            Padding = new Padding(20);
            var revenues = reportService.GetRevenueByMovie(DateTime.Today.AddDays(-30), DateTime.Today).ToList();

            var flow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 120 };
            flow.Controls.Add(new StatCard { Title = "DOANH THU 30 NGÀY", Value = $"{revenues.Sum(x => x.TotalRevenue):N0} đ" });
            flow.Controls.Add(new StatCard { Title = "VÉ ĐÃ BÁN", Value = revenues.Sum(x => x.TotalTicketsSold).ToString("N0"), BackColor = Color.FromArgb(39, 174, 96) });
            flow.Controls.Add(new StatCard { Title = "TỔNG SUẤT CHIẾU", Value = revenues.Sum(x => x.TotalShowtimes).ToString("N0"), BackColor = Color.FromArgb(142, 68, 173) });

            var dgvLogs = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true,
                DataSource = auditService.GetRecentLogs(50).ToList()
            };

            Controls.Add(dgvLogs);
            Controls.Add(new Label { Text = "Nhật ký hoạt động gần đây", Dock = DockStyle.Top, Height = 30, Font = new Font("Segoe UI", 11, FontStyle.Bold) });
            Controls.Add(flow);
        }
    }
}