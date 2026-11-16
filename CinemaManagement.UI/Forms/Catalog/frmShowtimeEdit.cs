
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.UI.Forms.Catalog
{
    public partial class frmShowtimeEdit : Form
    {
        private readonly IShowtimesService _showtimeService;
        private readonly IMovieService _movieService;
        private readonly IRoomService _roomService;
        private readonly Showtime _editingShowtime;

        private ComboBox cboMovie, cboRoom;
        private DateTimePicker dtpDate, dtpTime;
        private NumericUpDown numBasePrice;

        public frmShowtimeEdit(
            IShowtimesService showtimeService,
            IMovieService movieService,
            IRoomService roomService,
            Showtime showtime = null)
        {
            _showtimeService = showtimeService
                ?? throw new ArgumentNullException(nameof(showtimeService));

            _movieService = movieService
                ?? throw new ArgumentNullException(nameof(movieService));

            _roomService = roomService
                ?? throw new ArgumentNullException(nameof(roomService));

            _editingShowtime = showtime;

            InitializeComponent();
            BuildLayout();
            LoadSources();
        }

        private void BuildLayout()
        {
            Controls.Clear();

            Text = _editingShowtime == null
                ? "Thêm suất chiếu mới"
                : "Cập nhật suất chiếu";

            Size = new Size(460, 330);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            AddLabel("Phim:", 25, 28);
            cboMovie = new ComboBox
            {
                Location = new Point(130, 25),
                Width = 280,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            AddLabel("Phòng chiếu:", 25, 68);
            cboRoom = new ComboBox
            {
                Location = new Point(130, 65),
                Width = 280,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            AddLabel("Ngày chiếu:", 25, 108);
            dtpDate = new DateTimePicker
            {
                Location = new Point(130, 105),
                Width = 145,
                Format = DateTimePickerFormat.Short
            };

            AddLabel("Giờ chiếu:", 25, 148);
            dtpTime = new DateTimePicker
            {
                Location = new Point(130, 145),
                Width = 110,
                Format = DateTimePickerFormat.Time,
                ShowUpDown = true
            };

            AddLabel("Giá vé gốc:", 25, 188);
            numBasePrice = new NumericUpDown
            {
                Location = new Point(130, 185),
                Width = 145,
                Minimum = 0,
                Maximum = 1000000,
                Increment = 5000,
                ThousandsSeparator = true,
                Value = 75000
            };

            var btnSave = new Button
            {
                Text = "Lưu lịch",
                Location = new Point(200, 235),
                Size = new Size(100, 32)
            };

            var btnCancel = new Button
            {
                Text = "Hủy",
                Location = new Point(310, 235),
                Size = new Size(100, 32)
            };

            btnSave.Click += BtnSave_Click;
            btnCancel.Click += (s, e) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            Controls.AddRange(new Control[]
            {
                cboMovie, cboRoom, dtpDate, dtpTime,
                numBasePrice, btnSave, btnCancel
            });
        }

        private void AddLabel(string text, int x, int y)
        {
            Controls.Add(new Label
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true
            });
        }

        private void LoadSources()
        {
            var movies = _movieService.GetAllMovies().ToList();

            cboMovie.DataSource = movies;
            cboMovie.DisplayMember = nameof(Movie.Title);
            cboMovie.ValueMember = nameof(Movie.Id);

            var rooms = _roomService.GetAllRooms()
                .Where(r => r.IsActive)
                .ToList();

            cboRoom.DataSource = rooms;
            cboRoom.DisplayMember = nameof(Room.Name);
            cboRoom.ValueMember = nameof(Room.Id);

            if (_editingShowtime == null)
                return;

            cboMovie.SelectedValue = _editingShowtime.MovieId;
            cboRoom.SelectedValue = _editingShowtime.RoomId;

            dtpDate.Value = _editingShowtime.StartTime.Date;
            dtpTime.Value = _editingShowtime.StartTime;

            numBasePrice.Value = Math.Max(
                numBasePrice.Minimum,
                Math.Min(numBasePrice.Maximum, _editingShowtime.BasePrice));
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (cboMovie.SelectedValue == null)
                    throw new InvalidOperationException(
                        "Vui lòng chọn phim.");

                if (cboRoom.SelectedValue == null)
                    throw new InvalidOperationException(
                        "Vui lòng chọn phòng chiếu.");

                var showtime = _editingShowtime ?? new Showtime();

                showtime.MovieId = Convert.ToInt32(cboMovie.SelectedValue);
                showtime.RoomId = Convert.ToInt32(cboRoom.SelectedValue);
                showtime.StartTime = dtpDate.Value.Date.Add(
                    dtpTime.Value.TimeOfDay);
                showtime.BasePrice = numBasePrice.Value;

                if (_editingShowtime == null)
                    _showtimeService.AddShowtime(showtime);
                else
                    _showtimeService.UpdateShowtime(showtime);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Trùng lịch hoặc lỗi nghiệp vụ",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
    }
}
