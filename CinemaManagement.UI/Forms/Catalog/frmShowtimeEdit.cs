// File: CinemaManagement.UI/Forms/Catalog/frmShowtimeEdit.cs
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

        public frmShowtimeEdit(IShowtimesService showtimeService, IMovieService movieService, IRoomService roomService, Showtime showtime = null)
        {
            _showtimeService = showtimeService;
            _movieService = movieService;
            _roomService = roomService;
            _editingShowtime = showtime;

            InitializeComponent();
            SetupLayout();
            LoadSources();
        }

        private void SetupLayout()
        {
            if (Controls.Count > 0) return;

            Text = _editingShowtime == null ? "Thêm Suất Chiếu Mới" : "Cập Nhật Suất Chiếu";
            Size = new Size(460, 310);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            Controls.Add(new Label { Text = "Phim:", Location = new Point(25, 28), AutoSize = true });
            cboMovie = new ComboBox { Location = new Point(130, 25), Width = 280, DropDownStyle = ComboBoxStyle.DropDownList };

            Controls.Add(new Label { Text = "Phòng Chiếu:", Location = new Point(25, 68), AutoSize = true });
            cboRoom = new ComboBox { Location = new Point(130, 65), Width = 280, DropDownStyle = ComboBoxStyle.DropDownList };

            Controls.Add(new Label { Text = "Ngày Chiếu:", Location = new Point(25, 108), AutoSize = true });
            dtpDate = new DateTimePicker { Location = new Point(130, 105), Width = 145, Format = DateTimePickerFormat.Short };

            Controls.Add(new Label { Text = "Giờ Chiếu:", Location = new Point(25, 148), AutoSize = true });
            dtpTime = new DateTimePicker { Location = new Point(130, 145), Width = 110, Format = DateTimePickerFormat.Time, ShowUpDown = true };

            Controls.Add(new Label { Text = "Giá Vé Gốc:", Location = new Point(25, 188), AutoSize = true });
            numBasePrice = new NumericUpDown { Location = new Point(130, 185), Width = 145, Maximum = 1000000, Increment = 5000, Value = 75000 };

            var btnSave = new Button { Text = "Lưu Lịch", Location = new Point(200, 225), Size = new Size(100, 32) };
            var btnCancel = new Button { Text = "Hủy", Location = new Point(310, 225), Size = new Size(100, 32) };

            btnSave.Click += BtnSave_Click;
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.AddRange(new Control[] { cboMovie, cboRoom, dtpDate, dtpTime, numBasePrice, btnSave, btnCancel });
        }

        private void LoadSources()
        {
            cboMovie.DataSource = _movieService.GetAllMovies().ToList();
            cboMovie.DisplayMember = "Title";
            cboMovie.ValueMember = "Id";

            cboRoom.DataSource = _roomService.GetAllRooms().Where(r => r.IsActive).ToList();
            cboRoom.DisplayMember = "Name";
            cboRoom.ValueMember = "Id";

            if (_editingShowtime != null)
            {
                cboMovie.SelectedValue = _editingShowtime.MovieId;
                cboRoom.SelectedValue = _editingShowtime.RoomId;
                dtpDate.Value = _editingShowtime.StartTime.Date;
                dtpTime.Value = _editingShowtime.StartTime;
                numBasePrice.Value = _editingShowtime.BasePrice;
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                var st = _editingShowtime ?? new Showtime();
                st.MovieId = (int)cboMovie.SelectedValue;
                st.RoomId = (int)cboRoom.SelectedValue;
                st.StartTime = dtpDate.Value.Date.Add(dtpTime.Value.TimeOfDay);
                st.BasePrice = numBasePrice.Value;

                if (_editingShowtime == null)
                    _showtimeService.AddShowtime(st);
                else
                    _showtimeService.UpdateShowtime(st);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Trùng Lịch / Lỗi Nghiệp Vụ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}