
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.UI.Forms.Catalog
{
    public partial class frmMovieList : Form
    {
        private readonly IMovieService _movieService;
        private readonly IGenreRepository? _genreRepository;

        private TextBox txtSearch = null!;
        private DataGridView dgvMovies = null!;

        public frmMovieList(
            IMovieService movieService,
            IGenreRepository? genreRepository = null)
        {
            _movieService = movieService
                ?? throw new ArgumentNullException(nameof(movieService));

            _genreRepository = genreRepository;

            InitializeComponent();
            BuildLayout();
            LoadMovies();
        }

        private void BuildLayout()
        {
            Text = "Danh sách phim";
            Size = new Size(960, 580);
            StartPosition = FormStartPosition.CenterParent;

            Controls.Clear();

            var pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 55,
                Padding = new Padding(10)
            };

            txtSearch = new TextBox
            {
                Location = new Point(15, 16),
                Width = 260,
                PlaceholderText = "Tìm theo tên phim..."
            };

            txtSearch.TextChanged += (s, e) =>
                FilterMovies(txtSearch.Text);

            var btnAdd = new Button
            {
                Text = "Thêm phim",
                Location = new Point(295, 14),
                Size = new Size(105, 30)
            };

            var btnEdit = new Button
            {
                Text = "Sửa phim",
                Location = new Point(410, 14),
                Size = new Size(100, 30)
            };

            var btnDelete = new Button
            {
                Text = "Xóa phim",
                Location = new Point(520, 14),
                Size = new Size(100, 30)
            };

            var btnRefresh = new Button
            {
                Text = "Làm mới",
                Location = new Point(630, 14),
                Size = new Size(95, 30)
            };

            btnAdd.Click += BtnAdd_Click;
            btnEdit.Click += BtnEdit_Click;
            btnDelete.Click += BtnDelete_Click;

            btnRefresh.Click += (s, e) =>
            {
                txtSearch.Clear();
                LoadMovies();
            };

            pnlTop.Controls.AddRange(new Control[]
            {
                txtSearch,
                btnAdd,
                btnEdit,
                btnDelete,
                btnRefresh
            });

            dgvMovies = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                MultiSelect = false
            };

            dgvMovies.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    BtnEdit_Click(s, e);
                }
            };

            Controls.Add(dgvMovies);
            Controls.Add(pnlTop);
        }

        private void LoadMovies()
        {
            dgvMovies.DataSource =
                _movieService.GetAllMovies().ToList();
        }

        private void FilterMovies(string keyword)
        {
            var movies = _movieService.GetAllMovies();

            if (string.IsNullOrWhiteSpace(keyword))
            {
                dgvMovies.DataSource = movies.ToList();
                return;
            }

            dgvMovies.DataSource = movies
                .Where(m =>
                    !string.IsNullOrWhiteSpace(m.Title) &&
                    m.Title.Contains(
                        keyword.Trim(),
                        StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            using var form = new frmMovieEdit(
                _movieService,
                _genreRepository);

            if (form.ShowDialog(this) == DialogResult.OK)
            {
                LoadMovies();
            }
        }

        private void BtnEdit_Click(object? sender, EventArgs e)
        {
            if (dgvMovies.CurrentRow?.DataBoundItem is not Movie movie)
            {
                MessageBox.Show(
                    "Vui lòng chọn phim cần sửa.",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            using var form = new frmMovieEdit(
                _movieService,
                _genreRepository,
                movie);

            if (form.ShowDialog(this) == DialogResult.OK)
            {
                LoadMovies();
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (dgvMovies.CurrentRow?.DataBoundItem is not Movie movie)
            {
                MessageBox.Show(
                    "Vui lòng chọn phim cần xóa.",
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            var confirm = MessageBox.Show(
                $"Xác nhận xóa phim '{movie.Title}'?",
                "Xác nhận",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
            {
                return;
            }

            try
            {
                _movieService.DeleteMovie(movie.Id);
                LoadMovies();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Không thể xóa phim",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
    }
}
