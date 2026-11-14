using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.UI.Forms.Catalog
{
    public partial class frmMovieEdit : Form
    {
        private readonly IMovieService _movieService;
        private readonly IGenreRepository _genreRepository;
        private readonly Movie _editingMovie;

        private TextBox txtTitle, txtDescription;
        private NumericUpDown numDuration;
        private DateTimePicker dtpReleaseDate;
        private ComboBox cboStatus;
        private CheckedListBox clbGenres;
        private Button btnSave, btnCancel;

        public frmMovieEdit(IMovieService movieService, IGenreRepository genreRepository = null, Movie movie = null)
        {
            _movieService = movieService;
            _genreRepository = genreRepository;
            _editingMovie = movie;

            InitializeComponent();
            SetupLayout();
            LoadGenres();
            BindMovieData();
        }

        private void SetupLayout()
        {
            if (Controls.Count > 0) return;

            Text = _editingMovie == null ? "Thêm Phim Mới" : "Cập Nhật Thông Tin Phim";
            Size = new Size(540, 420);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            Controls.Add(new Label { Text = "Tên Phim:", Location = new Point(25, 25), AutoSize = true });
            txtTitle = new TextBox { Location = new Point(145, 22), Width = 350 };

            Controls.Add(new Label { Text = "Thời Lượng (phút):", Location = new Point(25, 62), AutoSize = true });
            numDuration = new NumericUpDown { Location = new Point(145, 59), Width = 100, Minimum = 1, Maximum = 500, Value = 120 };

            Controls.Add(new Label { Text = "Ngày Khởi Chiếu:", Location = new Point(260, 62), AutoSize = true });
            dtpReleaseDate = new DateTimePicker { Location = new Point(365, 59), Width = 130, Format = DateTimePickerFormat.Short };

            Controls.Add(new Label { Text = "Trạng Thái:", Location = new Point(25, 99), AutoSize = true });
            cboStatus = new ComboBox { Location = new Point(145, 96), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
            cboStatus.DataSource = Enum.GetValues(typeof(MovieStatus));

            Controls.Add(new Label { Text = "Thể Loại:", Location = new Point(25, 136), AutoSize = true });
            clbGenres = new CheckedListBox { Location = new Point(145, 133), Size = new Size(350, 85), CheckOnClick = true };

            Controls.Add(new Label { Text = "Mô Tả:", Location = new Point(25, 232), AutoSize = true });
            txtDescription = new TextBox { Location = new Point(145, 229), Size = new Size(350, 80), Multiline = true };

            btnSave = new Button { Text = "Lưu Thông Tin", Location = new Point(280, 325), Size = new Size(105, 32) };
            btnCancel = new Button { Text = "Hủy", Location = new Point(395, 325), Size = new Size(100, 32) };

            btnSave.Click += BtnSave_Click;
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.AddRange(new Control[] { txtTitle, numDuration, dtpReleaseDate, cboStatus, clbGenres, txtDescription, btnSave, btnCancel });
        }

        private void LoadGenres()
        {
            if (_genreRepository == null) return;
            var genres = _genreRepository.GetAll().ToList();
            clbGenres.DataSource = genres;
            clbGenres.DisplayMember = "Name";
            clbGenres.ValueMember = "Id";
        }

        private void BindMovieData()
        {
            if (_editingMovie == null) return;
            txtTitle.Text = _editingMovie.Title;
            numDuration.Value = _editingMovie.Duration > 0 ? _editingMovie.Duration : 120;
            dtpReleaseDate.Value = _editingMovie.ReleaseDate == default ? DateTime.Now : _editingMovie.ReleaseDate;
            cboStatus.SelectedItem = _editingMovie.Status;
            txtDescription.Text = _editingMovie.Description;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                var movie = _editingMovie ?? new Movie();
                movie.Title = txtTitle.Text.Trim();
                movie.Duration = (int)numDuration.Value;
                movie.ReleaseDate = dtpReleaseDate.Value;
                movie.Status = (MovieStatus)cboStatus.SelectedItem;
                movie.Description = txtDescription.Text.Trim();

                if (_editingMovie == null)
                    _movieService.AddMovie(movie);
                else
                    _movieService.UpdateMovie(movie);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}