
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

        public frmMovieEdit(
            IMovieService movieService,
            IGenreRepository genreRepository = null,
            Movie movie = null)
        {
            _movieService = movieService
                ?? throw new ArgumentNullException(nameof(movieService));

            _genreRepository = genreRepository;
            _editingMovie = movie;

            InitializeComponent();
            BuildLayout();
            LoadGenres();
            BindMovieData();
        }

        private void BuildLayout()
        {
            Text = _editingMovie == null
                ? "Thêm phim mới"
                : "Cập nhật thông tin phim";

            Size = new Size(540, 430);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            Controls.Clear();

            AddLabel("Tên phim:", 25, 25);
            txtTitle = new TextBox
            {
                Location = new Point(145, 22),
                Width = 350
            };

            AddLabel("Thời lượng (phút):", 25, 62);
            numDuration = new NumericUpDown
            {
                Location = new Point(145, 59),
                Width = 100,
                Minimum = 1,
                Maximum = 500,
                Value = 120
            };

            AddLabel("Ngày khởi chiếu:", 260, 62);
            dtpReleaseDate = new DateTimePicker
            {
                Location = new Point(365, 59),
                Width = 130,
                Format = DateTimePickerFormat.Short
            };

            AddLabel("Trạng thái:", 25, 99);
            cboStatus = new ComboBox
            {
                Location = new Point(145, 96),
                Width = 180,
                DropDownStyle = ComboBoxStyle.DropDownList,
                DataSource = Enum.GetValues(typeof(MovieStatus))
            };

            AddLabel("Thể loại:", 25, 136);
            clbGenres = new CheckedListBox
            {
                Location = new Point(145, 133),
                Size = new Size(350, 85),
                CheckOnClick = true
            };

            AddLabel("Mô tả:", 25, 232);
            txtDescription = new TextBox
            {
                Location = new Point(145, 229),
                Size = new Size(350, 80),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };

            var btnSave = new Button
            {
                Text = "Lưu thông tin",
                Location = new Point(280, 330),
                Size = new Size(105, 32)
            };

            var btnCancel = new Button
            {
                Text = "Hủy",
                Location = new Point(395, 330),
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
                txtTitle, numDuration, dtpReleaseDate,
                cboStatus, clbGenres, txtDescription,
                btnSave, btnCancel
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

        private void LoadGenres()
        {
            if (_genreRepository == null)
                return;

            var genres = _genreRepository.GetAll().ToList();

            clbGenres.DataSource = genres;
            clbGenres.DisplayMember = nameof(Genre.Name);
            clbGenres.ValueMember = nameof(Genre.Id);
        }

        private void BindMovieData()
        {
            if (_editingMovie == null)
                return;

            txtTitle.Text = _editingMovie.Title ?? string.Empty;

            numDuration.Value = Math.Max(
                numDuration.Minimum,
                Math.Min(numDuration.Maximum, _editingMovie.DurationMinutes));

            dtpReleaseDate.Value =
                _editingMovie.ReleaseDate == default
                    ? DateTime.Today
                    : _editingMovie.ReleaseDate;

            cboStatus.SelectedItem = _editingMovie.Status;
            txtDescription.Text = _editingMovie.Description ?? string.Empty;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtTitle.Text))
                    throw new InvalidOperationException("Vui lòng nhập tên phim.");

                var movie = _editingMovie ?? new Movie();

                movie.Title = txtTitle.Text.Trim();
                movie.DurationMinutes = (int)numDuration.Value;
                movie.ReleaseDate = dtpReleaseDate.Value.Date;
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
                MessageBox.Show(
                    ex.Message,
                    "Không thể lưu phim",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
    }
}
