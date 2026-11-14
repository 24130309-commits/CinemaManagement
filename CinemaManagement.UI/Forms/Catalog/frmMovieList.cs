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
        private readonly IGenreRepository _genreRepository;

        private TextBox txtSearch;
        private Button btnAdd, btnEdit, btnDelete, btnRefresh;
        private DataGridView dgvMovies;

        public frmMovieList(IMovieService movieService, IGenreRepository genreRepository = null)
        {
            _movieService = movieService;
            _genreRepository = genreRepository;
            InitializeComponent();
            SetupLayout();
            LoadMovies();
        }

        private void SetupLayout()
        {
            if (Controls.Count > 0) return;

            Text = "Danh Sách Phim";
            Size = new Size(960, 580);
            StartPosition = FormStartPosition.CenterParent;

            var pnlTop = new Panel { Dock = DockStyle.Top, Height = 55, Padding = new Padding(10) };

            txtSearch = new TextBox { Location = new Point(15, 16), Width = 260, PlaceholderText = "Tìm theo tên phim..." };
            txtSearch.TextChanged += (s, e) => FilterMovies(txtSearch.Text);

            btnAdd = new Button { Text = "Thêm Phim", Location = new Point(295, 14), Size = new Size(105, 30) };
            btnEdit = new Button { Text = "Sửa Phim", Location = new Point(410, 14), Size = new Size(100, 30) };
            btnDelete = new Button { Text = "Xóa Phim", Location = new Point(520, 14), Size = new Size(100, 30) };
            btnRefresh = new Button { Text = "Làm Mới", Location = new Point(630, 14), Size = new Size(95, 30) };

            btnAdd.Click += BtnAdd_Click;
            btnEdit.Click += BtnEdit_Click;
            btnDelete.Click += BtnDelete_Click;
            btnRefresh.Click += (s, e) => { txtSearch.Clear(); LoadMovies(); };

            pnlTop.Controls.AddRange(new Control[] { txtSearch, btnAdd, btnEdit, btnDelete, btnRefresh });

            dgvMovies = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgvMovies.CellDoubleClick += (s, e) => BtnEdit_Click(s, e);

            Controls.Add(dgvMovies);
            Controls.Add(pnlTop);
        }

        private void LoadMovies()
        {
            dgvMovies.DataSource = _movieService.GetAllMovies().ToList();
        }

        private void FilterMovies(string keyword)
        {
            var all = _movieService.GetAllMovies();
            if (string.IsNullOrWhiteSpace(keyword))
                dgvMovies.DataSource = all.ToList();
            else
                dgvMovies.DataSource = all.Where(m => m.Title != null && m.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            using (var frm = new frmMovieEdit(_movieService, _genreRepository, null))
            {
                if (frm.ShowDialog() == DialogResult.OK)
                    LoadMovies();
            }
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (dgvMovies.CurrentRow?.DataBoundItem is Movie selectedMovie)
            {
                using (var frm = new frmMovieEdit(_movieService, _genreRepository, selectedMovie))
                {
                    if (frm.ShowDialog() == DialogResult.OK)
                        LoadMovies();
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một bộ phim để sửa.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (dgvMovies.CurrentRow?.DataBoundItem is Movie selectedMovie)
            {
                if (MessageBox.Show($"Xác nhận xóa phim '{selectedMovie.Title}'?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    try
                    {
                        _movieService.DeleteMovie(selectedMovie.Id);
                        LoadMovies();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }
    }
}