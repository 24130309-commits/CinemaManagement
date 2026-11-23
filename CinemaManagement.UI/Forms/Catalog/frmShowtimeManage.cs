
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

        public frmShowtimeManage(
            IShowtimesService showtimeService,
            IMovieService movieService,
            IRoomService roomService)
        {
            _showtimeService = showtimeService
                ?? throw new ArgumentNullException(nameof(showtimeService));

            _movieService = movieService
                ?? throw new ArgumentNullException(nameof(movieService));

            _roomService = roomService
                ?? throw new ArgumentNullException(nameof(roomService));

            InitializeComponent();
            BuildLayout();
            LoadShowtimes();
        }

        private void BuildLayout()
        {
            Controls.Clear();

            Text = "Quản lý lịch chiếu";
            Size = new Size(960, 560);
            StartPosition = FormStartPosition.CenterParent;

            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 55
            };

            pnlTop.Controls.Add(new Label
            {
                Text = "Ngày chiếu:",
                Location = new Point(15, 19),
                AutoSize = true
            });

            dtpFilterDate = new DateTimePicker
            {
                Location = new Point(95, 15),
                Width = 140,
                Format = DateTimePickerFormat.Short
            };

            dtpFilterDate.ValueChanged += (s, e) =>
                LoadShowtimesByDate();

            var btnShowAll = new Button
            {
                Text = "Tất cả lịch",
                Location = new Point(245, 14),
                Size = new Size(100, 30)
            };

            var btnAdd = new Button
            {
                Text = "Thêm suất chiếu",
                Location = new Point(360, 14),
                Size = new Size(125, 30)
            };

            var btnEdit = new Button
            {
                Text = "Sửa suất",
                Location = new Point(495, 14),
                Size = new Size(95, 30)
            };

            var btnDelete = new Button
            {
                Text = "Xóa suất",
                Location = new Point(600, 14),
                Size = new Size(95, 30)
            };

            btnShowAll.Click += (s, e) => LoadShowtimes();
            btnAdd.Click += BtnAdd_Click;
            btnEdit.Click += BtnEdit_Click;
            btnDelete.Click += BtnDelete_Click;

            pnlTop.Controls.AddRange(new Control[]
            {
                dtpFilterDate,
                btnShowAll,
                btnAdd,
                btnEdit,
                btnDelete
            });

            dgvShowtimes = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                MultiSelect = false
            };

            Controls.Add(dgvShowtimes);
            Controls.Add(pnlTop);
        }

        private void LoadShowtimes()
        {
            dgvShowtimes.DataSource =
                _showtimeService.GetAllShowtimes().ToList();
        }

        private void LoadShowtimesByDate()
        {
            dgvShowtimes.DataSource =
                _showtimeService
                    .GetShowtimesByDate(dtpFilterDate.Value.Date)
                    .ToList();
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            using var form = new frmShowtimeEdit(
                _showtimeService,
                _movieService,
                _roomService);

            if (form.ShowDialog(this) == DialogResult.OK)
                LoadShowtimes();
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (dgvShowtimes.CurrentRow?.DataBoundItem is not Showtime showtime)
            {
                MessageBox.Show(
                    "Vui lòng chọn suất chiếu cần sửa.",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            using var form = new frmShowtimeEdit(
                _showtimeService,
                _movieService,
                _roomService,
                showtime);

            if (form.ShowDialog(this) == DialogResult.OK)
                LoadShowtimes();
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (dgvShowtimes.CurrentRow?.DataBoundItem is not Showtime showtime)
            {
                MessageBox.Show(
                    "Vui lòng chọn suất chiếu cần xóa.",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            var confirm = MessageBox.Show(
                "Xác nhận xóa suất chiếu này?",
                "Xác nhận",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
                return;

            try
            {
                _showtimeService.DeleteShowtime(showtime.Id);
                LoadShowtimes();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Không thể xóa suất chiếu",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
    }
}
