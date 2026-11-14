using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Core.DTOs;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.UI.Helpers;

namespace CinemaManagement.UI.Forms.Reports
{
    public partial class frmRevenueReport : Form
    {
        private List<RevenueByMovieDto> _currentData = new();

        public frmRevenueReport(IReportService reportService, IExportService exportService)
        {
            Padding = new Padding(15);
            var pnlTop = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 45 };
            var dtpFrom = new DateTimePicker { Value = DateTime.Today.AddMonths(-1), Format = DateTimePickerFormat.Short };
            var dtpTo = new DateTimePicker { Value = DateTime.Today, Format = DateTimePickerFormat.Short };
            var btnFilter = new Button { Text = "Xem báo cáo", Width = 100 };
            var btnExport = new Button { Text = "Xuất CSV", Width = 90 };
            var dgv = new DataGridView { Dock = DockStyle.Fill, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, ReadOnly = true };

            btnFilter.Click += (s, e) =>
            {
                _currentData = reportService.GetRevenueByMovie(dtpFrom.Value, dtpTo.Value).ToList();
                dgv.DataSource = _currentData;
            };

            btnExport.Click += (s, e) =>
            {
                using var sfd = new SaveFileDialog { Filter = "CSV File|*.csv", FileName = "BaoCaoDoanhThu.csv" };
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    exportService.ExportToCsv(_currentData, sfd.FileName);
                    MessageHelper.ShowInfo("Xuất file CSV thành công!");
                }
            };

            pnlTop.Controls.AddRange(new Control[] { new Label { Text = "Từ:", AutoSize = true }, dtpFrom, new Label { Text = "Đến:", AutoSize = true }, dtpTo, btnFilter, btnExport });
            Controls.Add(dgv);
            Controls.Add(pnlTop);
            btnFilter.PerformClick();
        }
    }
}