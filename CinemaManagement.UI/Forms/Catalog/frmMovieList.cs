
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.Core.Storage;
using CinemaManagement.UI.Controls;
using CinemaManagement.UI.Helpers;

namespace CinemaManagement.UI.Forms.Catalog;

public partial class frmMovieList : Form
{
    private readonly IMovieService _movieService;
    private readonly IGenreService? _genreService;

    private TextBox txtSearch = null!;
    private ComboBox cboStatus = null!;
    private EmptyStateDataGridView dgvMovies = null!;
    private Label lblTotal = null!;
    private Label lblShowing = null!;
    private Label lblUpcoming = null!;

    private List<Movie> _allMovies = new();
    private readonly List<Image> _loadedImages = new();

    public frmMovieList(
        IMovieService movieService,
        IGenreService? genreService = null)
    {
        _movieService = movieService
            ?? throw new ArgumentNullException(nameof(movieService));

        _genreService = genreService;

        InitializeComponent();
        BuildLayout();
        LoadMovies();
        ThemeHelper.Apply(this);
    }

    private void BuildLayout()
    {
        Text = "Quản lý phim";
        Size = new Size(1280, 720);
        MinimumSize = new Size(1050, 600);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(245, 247, 250);
        Font = new Font("Segoe UI", 9F);

        Controls.Clear();

        var mainPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20),
            BackColor = BackColor
        };

        var header = new PageHeader(
            "Quản lý phim",
            "Quản lý danh mục, trạng thái phát hành và thông tin phim.");

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 96,
            Padding = new Padding(0, 8, 0, 8),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoScroll = true,
            BackColor = ThemeHelper.Background
        };

        txtSearch = new TextBox
        {
            Location = new Point(0, 11),
            Width = 250,
            Height = 30,
            Font = new Font("Segoe UI", 9F),
            PlaceholderText = "Tìm theo tên phim..."
        };

        cboStatus = new ComboBox
        {
            Location = new Point(265, 10),
            Width = 155,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9F)
        };

        cboStatus.Items.Add("Tất cả trạng thái");

        foreach (MovieStatus status in Enum.GetValues(typeof(MovieStatus)))
        {
            cboStatus.Items.Add(status);
        }

        cboStatus.SelectedIndex = 0;

        cboStatus.Format += (_, e) =>
        {
            if (e.ListItem is MovieStatus status)
                e.Value = GetStatusText(status);
        };

        var btnAdd = CreateButton(
            "Thêm phim", ThemeHelper.Accent, 430, 9, 100);

        var btnEdit = CreateButton(
            "Sửa phim", Color.FromArgb(71, 85, 105), 540, 9, 90);

        var btnDelete = CreateButton(
            "Xóa phim", Color.FromArgb(220, 38, 38), 640, 9, 90);

        var btnRefresh = CreateButton(
            "Làm mới", Color.FromArgb(100, 116, 139), 740, 9, 90);

        var btnDetails = CreateButton(
            "Chi tiết", Color.FromArgb(14, 165, 233), 840, 9, 100);

        txtSearch.TextChanged += (_, _) => ApplyFilters();
        cboStatus.SelectedIndexChanged += (_, _) => ApplyFilters();

        btnAdd.Click += BtnAdd_Click;
        btnEdit.Click += BtnEdit_Click;
        btnDelete.Click += BtnDelete_Click;
        btnDetails.Click += BtnDetails_Click;

        btnRefresh.Click += (_, _) =>
        {
            txtSearch.Clear();
            cboStatus.SelectedIndex = 0;
            LoadMovies();
        };

        toolbar.Controls.AddRange(new Control[]
        {
            txtSearch,
            cboStatus,
            btnAdd,
            btnEdit,
            btnDelete,
            btnRefresh,
            btnDetails
        });

        var summaryPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 54,
            Padding = new Padding(0, 12, 0, 0)
        };

        lblTotal = CreateSummaryLabel(0, 0);
        lblShowing = CreateSummaryLabel(220, 0);
        lblUpcoming = CreateSummaryLabel(440, 0);

        summaryPanel.Controls.AddRange(new Control[]
        {
            lblTotal,
            lblShowing,
            lblUpcoming
        });

        dgvMovies = new EmptyStateDataGridView
        {
            EmptyStateText = "Không tìm thấy phim phù hợp. Hãy điều chỉnh bộ lọc hoặc thêm phim mới.",
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            GridColor = Color.FromArgb(226, 232, 240),
            EnableHeadersVisualStyles = false,
            ColumnHeadersHeight = 42,
            RowTemplate = { Height = 78 },
            Font = new Font("Segoe UI", 9F)
        };

        dgvMovies.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0)
                BtnEdit_Click(this, EventArgs.Empty);
        };

        dgvMovies.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 ||
                dgvMovies.Columns[e.ColumnIndex].Name != "Status")
            {
                return;
            }

            var tone = Convert.ToString(e.Value) switch
            {
                "Đang chiếu" => (ThemeHelper.Success, ThemeHelper.SuccessSoft),
                "Sắp chiếu" => (ThemeHelper.Info, ThemeHelper.InfoSoft),
                "Ngừng chiếu" => (ThemeHelper.Danger, ThemeHelper.DangerSoft),
                "Đã kết thúc" or "Ngừng hoạt động" =>
                    (ThemeHelper.Muted, ThemeHelper.SurfaceAlt),
                _ => (ThemeHelper.Muted, ThemeHelper.SurfaceAlt)
            };

            e.CellStyle.ForeColor = tone.Item1;
            e.CellStyle.BackColor = tone.Item2;
            e.CellStyle.SelectionForeColor = tone.Item1;
            e.CellStyle.SelectionBackColor = tone.Item2;
            e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            e.CellStyle.Font = ThemeHelper.SectionFont;
        };

        dgvMovies.Columns.Add(new DataGridViewImageColumn
        {
            Name = "Poster",
            HeaderText = "POSTER",
            FillWeight = 13,
            ImageLayout = DataGridViewImageCellLayout.Zoom
        });

        dgvMovies.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Id",
            HeaderText = "MÃ PHIM",
            FillWeight = 8
        });

        dgvMovies.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Title",
            HeaderText = "TÊN PHIM",
            FillWeight = 25
        });

        dgvMovies.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Duration",
            HeaderText = "THỜI LƯỢNG",
            FillWeight = 11
        });

        dgvMovies.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "AgeLimit",
            HeaderText = "ĐỘ TUỔI",
            FillWeight = 9
        });

        dgvMovies.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "ReleaseDate",
            HeaderText = "NGÀY PHÁT HÀNH",
            FillWeight = 16
        });

        dgvMovies.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Status",
            HeaderText = "TRẠNG THÁI",
            FillWeight = 16
        });

        var gridPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 5, 0, 0),
            BackColor = Color.White
        };

        gridPanel.Controls.Add(dgvMovies);

        mainPanel.Controls.Add(gridPanel);
        mainPanel.Controls.Add(summaryPanel);
        mainPanel.Controls.Add(toolbar);
        mainPanel.Controls.Add(header);

        Controls.Add(mainPanel);
    }

    private static Button CreateButton(
        string text,
        Color color,
        int x,
        int y,
        int width)
    {
        var button = new Button
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(width, 32),
            BackColor = color,
            ForeColor = color == ThemeHelper.Accent
                ? ThemeHelper.Primary
                : Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        ThemeHelper.StyleActionButton(button, color);
        return button;
    }

    private static Label CreateSummaryLabel(int x, int y)
    {
        return new Label
        {
            Location = new Point(x, y),
            AutoSize = true,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(51, 65, 85)
        };
    }

    private void LoadMovies()
    {
        try
        {
            _allMovies = _movieService.GetAllMovies().ToList();
            ApplyFilters();
            UpdateSummary();
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex, "Không thể tải danh sách phim.");
        }
    }

    private void ApplyFilters()
    {
        if (dgvMovies == null)
            return;

        DisposeLoadedImages();
        dgvMovies.Rows.Clear();

        var keyword = txtSearch?.Text.Trim() ?? string.Empty;

        IEnumerable<Movie> filtered = _allMovies;

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            filtered = filtered.Where(movie =>
                movie.Title.Contains(
                    keyword,
                    StringComparison.OrdinalIgnoreCase));
        }

        if (cboStatus?.SelectedItem is MovieStatus selectedStatus)
        {
            filtered = filtered.Where(movie =>
                movie.Status == selectedStatus);
        }

        foreach (var movie in filtered.OrderBy(movie => movie.Title))
        {
            Image? poster = LoadPoster(movie.PosterPath);

            int rowIndex = dgvMovies.Rows.Add(
                new object[]
                {
                    poster!,
                    movie.Id,
                    movie.Title,
                    $"{movie.DurationMinutes} phút",
                    movie.AgeLimit > 0
                        ? $"{movie.AgeLimit}+"
                        : "Chưa xác định",
                    movie.ReleaseDate == default
                        ? "Chưa cập nhật"
                        : movie.ReleaseDate.ToString("dd/MM/yyyy"),
                    GetStatusText(movie.Status)
                });

            dgvMovies.Rows[rowIndex].Tag = movie;

            dgvMovies.Rows[rowIndex].Cells["Title"].ToolTipText =
                movie.Description ?? string.Empty;

        }
    }

    private Image? LoadPoster(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        try
        {
            var fullPath = StoragePathResolver.ResolvePath(path);

            if (!File.Exists(fullPath))
                return null;

            using var source = Image.FromFile(fullPath);
            var image = new Bitmap(source);
            _loadedImages.Add(image);
            return image;
        }
        catch
        {
            return null;
        }
    }

    private void DisposeLoadedImages()
    {
        foreach (var image in _loadedImages)
            image.Dispose();

        _loadedImages.Clear();
    }

    private void UpdateSummary()
    {
        lblTotal.Text = $"Tổng số phim: {_allMovies.Count}";

        lblShowing.Text =
            $"Đang chiếu: {_allMovies.Count(m => GetStatusText(m.Status) == "Đang chiếu")}";

        lblUpcoming.Text =
            $"Sắp chiếu: {_allMovies.Count(m => GetStatusText(m.Status) == "Sắp chiếu")}";
    }

    private static string GetStatusText(MovieStatus status)
    {
        return status.ToString() switch
        {
            "NowShowing" => "Đang chiếu",
            "ComingSoon" => "Sắp chiếu",
            "Stopped" => "Ngừng chiếu",
            "Ended" => "Đã kết thúc",
            "Inactive" => "Ngừng hoạt động",
            _ => status.ToString()
        };
    }

    private Movie? GetSelectedMovie()
    {
        return dgvMovies.CurrentRow?.Tag as Movie;
    }

    private void BtnAdd_Click(object? sender, EventArgs e)
    {
        using var form = new frmMovieEdit(
            _movieService,
            _genreService);

        if (form.ShowDialog(this) == DialogResult.OK)
            LoadMovies();
    }

    private void BtnEdit_Click(object? sender, EventArgs e)
    {
        var movie = GetSelectedMovie();

        if (movie == null)
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
            _genreService,
            movie);

        if (form.ShowDialog(this) == DialogResult.OK)
            LoadMovies();
    }

    private void BtnDetails_Click(object? sender, EventArgs e)
    {
        var movie = GetSelectedMovie();

        if (movie == null)
        {
            MessageBox.Show(
                "Vui lòng chọn phim cần xem chi tiết.",
                "Thông báo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        using var form = new frmMovieDetails(movie);
        form.ShowDialog(this);
    }

    private void BtnDelete_Click(object? sender, EventArgs e)
    {
        var movie = GetSelectedMovie();

        if (movie == null)
        {
            MessageBox.Show(
                "Vui lòng chọn phim cần xóa.",
                "Thông báo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        var confirm = MessageHelper.Confirm(
            $"Bạn có chắc chắn muốn xóa phim \"{movie.Title}\" không?",
            "Xác nhận xóa");

        if (!confirm)
            return;

        try
        {
            _movieService.DeleteMovie(movie.Id);
            LoadMovies();
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex, "Không thể xóa phim.");
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        DisposeLoadedImages();
        base.OnFormClosed(e);
    }
}
