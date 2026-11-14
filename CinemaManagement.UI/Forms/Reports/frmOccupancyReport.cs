using System;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.UI.Forms.Reports
{
    public partial class frmOccupancyReport : Form
    {
        public frmOccupancyReport(IReportService reportService)
        {
            Padding = new Padding(15);
            var pnlTop = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 45 };
            var dtpFrom = new DateTimePicker { Value = DateTime.Today.AddDays(-7), Format = DateTimePickerFormat.Short };
            var dtpTo = new DateTimePicker { Value = DateTime.Today.AddDays(7), Format = DateTimePickerFormat.Short };
            var btnFilter = new Button { Text = "Thống kê lấp đầy", Width = 120 };
            var dgv = new DataGridView { Dock = DockStyle.Fill, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, ReadOnly = true };

            btnFilter.Click += (s, e) => dgv.DataSource = reportService.GetOccupancyReport(dtpFrom.Value, dtpTo.Value).ToList();

            pnlTop.Controls.AddRange(new Control[] { new Label { Text = "Từ:", AutoSize = true }, dtpFrom, new Label { Text = "Đến:", AutoSize = true }, dtpTo, btnFilter });
            Controls.Add(dgv);
            Controls.Add(pnlTop);
            btnFilter.PerformClick();
        }
    }
}