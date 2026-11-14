// File: CinemaManagement.UI/Forms/Catalog/frmShowtimeManage.cs
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.UI.Forms.Catalog
{
    public partial class frmShowtimeManage : Form
    {
        private readonly IShowtimesService _showtimeService;
        private readonly IMovieService _movieService;
        private readonly IRoomService _roomService;

        private DateTimePicker dtpFilterDate;
        private DataGridView dgvShowtimes;

        public frmShowtimeManage(IShowtimesService showtimeService, IMovieService movieService, IRoomService roomService)
        {
            _showtimeService = showtimeService;
            _movieService = movieService;
            _roomService = roomService;

            InitializeComponent();
            SetupLayout();
            LoadShowtimes();
        }

        private void SetupLayout()
        {
            if (Controls.Count > 0) return;

            Text = "Quản Lý Lịch Chiếu";
            Size = new Size(960, 560);
            StartPosition = FormStartPosition.CenterParent;

            var pnlTop = new Panel { Dock = DockStyle.Top, Height = 55 };
            pnlTop.Controls.Add(new Label { Text = "Ngày Chiếu:", Location = new Point(15, 19), AutoSize = true });
            dtpFilterDate = new DateTimePicker { Location = new Point(95, 15), Width = 140, Format = DateTimePickerFormat.Short };
            dtpFilterDate.ValueChanged += (s, e) => LoadShowtimesByDate();

            var btnShowAll = new Button { Text = "Tất Cả Lịch", Location = new Point(245, 14), Size = new Size(100, 30) };
            var btnAdd = new Button { Text = "Thêm Suất Chiếu", Location = new Point(360, 14), Size = new Size(125, 30) };
            var btnEdit = new Button { Text = "Sửa Suất", Location = new Point(495, 14), Size = new Size(95, 30) };
            var btnDelete = new Button { Text = "Xóa Suất", Location = new Point(600, 14), Size = new Size(95, 30) };

            btnShowAll.Click += (s, e) => LoadShowtimes();
            btnAdd.Click += BtnAdd_Click;
            btnEdit.Click += BtnEdit_Click;
            btnDelete.Click += BtnDelete_Click;

            pnlTop.Controls.AddRange(new Control[] { dtpFilterDate, btnShowAll, btnAdd, btnEdit, btnDelete });

            dgvShowtimes = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            Controls.Add(dgvShowtimes);
            Controls.Add(pnlTop);
        }

        private void LoadShowtimes() => dgvShowtimes.DataSource = _showtimeService.GetAllShowtimes().ToList();
        private void LoadShowtimesByDate() => dgvShowtimes.DataSource = _showtimeService.GetShowtimesByDate(dtpFilterDate.Value.Date).ToList();

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            using (var frm = new frmShowtimeEdit(_showtimeService, _movieService, _roomService, null))
            {
                if (frm.ShowDialog() == DialogResult.OK) LoadShowtimes();
            }
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (dgvShowtimes.CurrentRow?.DataBoundItem is Showtime st)
            {
                using (var frm = new frmShowtimeEdit(_showtimeService, _movieService, _roomService, st))
                {
                    if (frm.ShowDialog() == DialogResult.OK) LoadShowtimes();
                }
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (dgvShowtimes.CurrentRow?.DataBoundItem is Showtime st)
            {
                if (MessageBox.Show("Xác nhận xóa suất chiếu này?", "Xác nhận", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    _showtimeService.DeleteShowtime(st.Id);
                    LoadShowtimes();
                }
            }
        }
    }
}