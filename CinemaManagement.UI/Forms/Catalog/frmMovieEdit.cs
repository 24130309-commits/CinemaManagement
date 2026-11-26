
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.Core.Storage;
using CinemaManagement.UI.Controls;
using CinemaManagement.UI.Helpers;

namespace CinemaManagement.UI.Forms.Catalog;

public partial class frmMovieEdit : Form
{
    private readonly IMovieService _movieService;
    private readonly IGenreService? _genreService;
    private readonly Movie? _editingMovie;

    private TextBox txtTitle = null!;
    private TextBox txtDescription = null!;
    private TextBox txtPosterPath = null!;
    private TextBox txtTrailerUrl = null!;
    private TextBox txtVideoPath = null!;

    private NumericUpDown numDuration = null!;
    private NumericUpDown numAgeLimit = null!;

    private DateTimePicker dtpReleaseDate = null!;
    private ComboBox cboStatus = null!;
    private CheckedListBox clbGenres = null!;

    private PictureBox picPoster = null!;

    private Button btnSave = null!;
    private Button btnCancel = null!;

    private Image? _previewImage;
    private readonly List<string> _managedAssetsCreated = new();
    private bool _assetsCommitted;

    public frmMovieEdit(
        IMovieService movieService,
        IGenreService? genreService = null,
        Movie? movie = null)
    {
        _movieService = movieService
            ?? throw new ArgumentNullException(nameof(movieService));

        _genreService = genreService;
        _editingMovie = movie;

        InitializeComponent();
        BuildLayout();

        FormClosed += (_, _) => DisposeUncommittedAssets();

        LoadGenres();
        BindMovieData();
        ThemeHelper.Apply(this);
    }

    private void BuildLayout()
    {
        Text = _editingMovie == null
            ? "Thêm phim mới"
            : "Cập nhật thông tin phim";

        Size = new Size(900, 760);
        MinimumSize = new Size(820, 540);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.FromArgb(245, 247, 250);
        Font = new Font("Segoe UI", 9F);

        Controls.Clear();

        var header = new PageHeader(
            _editingMovie == null ? "Thêm phim mới" : "Cập nhật phim",
            "Quản lý nội dung, thông tin phát hành và tài nguyên của phim.");

        var content = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(22, 8, 22, 8),
            AutoScroll = true
        };

        var leftPanel = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(535, 490),
            BackColor = Color.White,
            Padding = new Padding(15)
        };

        var rightPanel = new Panel
        {
            Location = new Point(555, 0),
            Size = new Size(260, 590),
            BackColor = Color.White,
            Padding = new Padding(15)
        };

        AddSectionTitle(leftPanel, "THÔNG TIN PHIM", 15, 12);

        AddLabel(leftPanel, "Tên phim *", 15, 52);

        txtTitle = new TextBox
        {
            Location = new Point(15, 76),
            Size = new Size(490, 30),
            Font = new Font("Segoe UI", 10F)
        };

        AddLabel(leftPanel, "Thời lượng (phút) *", 15, 120);

        numDuration = new NumericUpDown
        {
            Location = new Point(15, 144),
            Size = new Size(145, 30),
            Minimum = 1,
            Maximum = 500,
            Value = 120,
            Font = new Font("Segoe UI", 10F)
        };

        AddLabel(leftPanel, "Giới hạn độ tuổi", 185, 120);

        numAgeLimit = new NumericUpDown
        {
            Location = new Point(185, 144),
            Size = new Size(145, 30),
            Minimum = 0,
            Maximum = 21,
            Value = 0,
            Font = new Font("Segoe UI", 10F)
        };

        AddLabel(leftPanel, "Ngày phát hành", 15, 190);

        dtpReleaseDate = new DateTimePicker
        {
            Location = new Point(15, 214),
            Size = new Size(220, 30),
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd/MM/yyyy",
            Font = new Font("Segoe UI", 10F)
        };

        AddLabel(leftPanel, "Trạng thái phim", 260, 190);

        cboStatus = new ComboBox
        {
            Location = new Point(260, 214),
            Size = new Size(245, 30),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 10F)
        };

        foreach (MovieStatus status in Enum.GetValues(typeof(MovieStatus)))
        {
            cboStatus.Items.Add(status);
        }

        cboStatus.Format += (_, e) =>
        {
            if (e.ListItem is MovieStatus status)
                e.Value = GetStatusText(status);
        };

        AddLabel(leftPanel, "Thể loại", 15, 260);

        clbGenres = new CheckedListBox
        {
            Location = new Point(15, 284),
            Size = new Size(490, 72),
            CheckOnClick = true,
            Font = new Font("Segoe UI", 9F),
            BorderStyle = BorderStyle.FixedSingle
        };

        AddLabel(leftPanel, "Mô tả nội dung", 15, 370);

        txtDescription = new TextBox
        {
            Location = new Point(15, 394),
            Size = new Size(490, 78),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("Segoe UI", 9F)
        };

        leftPanel.Controls.AddRange(new Control[]
        {
            txtTitle,
            numDuration,
            numAgeLimit,
            dtpReleaseDate,
            cboStatus,
            clbGenres,
            txtDescription
        });

        AddSectionTitle(rightPanel, "HÌNH ẢNH & VIDEO", 15, 12);

        AddLabel(rightPanel, "Poster phim", 15, 52);

        picPoster = new PictureBox
        {
            Location = new Point(45, 80),
            Size = new Size(150, 205),
            BorderStyle = BorderStyle.FixedSingle,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.FromArgb(241, 245, 249),
            AccessibleName = "Xem trước poster phim"
        };

        var btnChoosePoster = new Button
        {
            Text = "Chọn ảnh poster",
            Location = new Point(35, 298),
            Size = new Size(170, 34),
            BackColor = ThemeHelper.Accent,
            ForeColor = ThemeHelper.Primary,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        ThemeHelper.StyleActionButton(btnChoosePoster, ThemeHelper.ActionButtonRole.Secondary);

        btnChoosePoster.Click += BtnChoosePoster_Click;

        AddLabel(rightPanel, "Đường dẫn poster", 15, 345);

        txtPosterPath = new TextBox
        {
            Location = new Point(15, 369),
            Size = new Size(215, 28),
            ReadOnly = true,
            Font = new Font("Segoe UI", 8F)
        };

        AddLabel(rightPanel, "Đường dẫn trailer", 15, 410);

        txtTrailerUrl = new TextBox
        {
            Location = new Point(15, 434),
            Size = new Size(215, 28),
            Font = new Font("Segoe UI", 8F)
        };

        AddLabel(rightPanel, "Video phim trong kho", 15, 475);

        txtVideoPath = new TextBox
        {
            Location = new Point(15, 499),
            Size = new Size(215, 28),
            ReadOnly = true,
            Font = new Font("Segoe UI", 8F)
        };

        var btnChooseVideo = new Button
        {
            Text = "Chọn file video",
            Location = new Point(35, 535),
            Size = new Size(170, 34),
            BackColor = ThemeHelper.Accent,
            ForeColor = ThemeHelper.Primary,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        ThemeHelper.StyleActionButton(btnChooseVideo, ThemeHelper.ActionButtonRole.Secondary);

        btnChooseVideo.Click += BtnChooseVideo_Click;

        rightPanel.Controls.AddRange(new Control[]
        {
            picPoster,
            btnChoosePoster,
            txtPosterPath,
            txtTrailerUrl,
            txtVideoPath,
            btnChooseVideo
        });

        content.Controls.Add(leftPanel);
        content.Controls.Add(rightPanel);

        void LayoutEditorPanels()
        {
            const int panelGap = 20;
            const int sideBySideWidth = 535 + panelGap + 260;
            var availableWidth = content.ClientSize.Width - content.Padding.Horizontal;
            var stackPanels = availableWidth < sideBySideWidth;

            leftPanel.Location = Point.Empty;
            rightPanel.Location = stackPanels
                ? new Point(0, leftPanel.Bottom + 16)
                : new Point(leftPanel.Width + panelGap, 0);

            content.AutoScrollMinSize = stackPanels
                ? new Size(Math.Max(leftPanel.Width, rightPanel.Width), rightPanel.Bottom + 8)
                : new Size(sideBySideWidth, Math.Max(leftPanel.Height, rightPanel.Height));
        }

        content.Resize += (_, _) => LayoutEditorPanels();
        LayoutEditorPanels();

        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 65,
            Padding = new Padding(22, 12, 22, 12),
            BackColor = Color.White
        };

        btnSave = new Button
        {
            Text = "Lưu thông tin",
            Size = new Size(125, 36),
            Location = new Point(600, 12),
            BackColor = ThemeHelper.Accent,
            ForeColor = ThemeHelper.Primary,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Cursor = Cursors.Hand
        };

        btnCancel = new Button
        {
            Text = "Hủy",
            Size = new Size(100, 36),
            Location = new Point(735, 12),
            BackColor = ThemeHelper.SurfaceAlt,
            ForeColor = ThemeHelper.Primary,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };

        ThemeHelper.StyleActionButton(btnSave, ThemeHelper.ActionButtonRole.Primary);
        ThemeHelper.StyleActionButton(btnCancel, ThemeHelper.ActionButtonRole.Secondary);

        btnSave.Click += BtnSave_Click;

        btnCancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        footer.Controls.AddRange(new Control[]
        {
            btnSave,
            btnCancel
        });

        Controls.Add(content);
        Controls.Add(footer);
        Controls.Add(header);

        AcceptButton = btnSave;
        CancelButton = btnCancel;
        ThemeHelper.FitToWorkingArea(this, new Size(900, 760), new Size(820, 540));

        AcceptButton = btnSave;
        CancelButton = btnCancel;
    }

    private static void AddSectionTitle(
        Control parent,
        string text,
        int x,
        int y)
    {
        parent.Controls.Add(new Label
        {
            Text = text,
            Location = new Point(x, y),
            AutoSize = true,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = ThemeHelper.Primary
        });
    }

    private static void AddLabel(
        Control parent,
        string text,
        int x,
        int y)
    {
        parent.Controls.Add(new Label
        {
            Text = text,
            Location = new Point(x, y),
            AutoSize = true,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(51, 65, 85)
        });
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

    private void LoadGenres()
    {
        if (_genreService == null)
        {
            clbGenres.Enabled = false;
            clbGenres.Items.Add("Chưa kết nối dữ liệu thể loại");
            return;
        }

        try
        {
            var genres = _genreService.GetAll().ToList();

            clbGenres.DataSource = genres;
            clbGenres.DisplayMember = nameof(Genre.Name);
            clbGenres.ValueMember = nameof(Genre.Id);
        }
        catch
        {
            clbGenres.Enabled = false;
            clbGenres.Items.Add("Không thể tải danh sách thể loại");
        }
    }

    private void BindMovieData()
    {
        if (_editingMovie == null)
        {
            cboStatus.SelectedItem = Enum.GetValues(typeof(MovieStatus))
                .Cast<MovieStatus>()
                .FirstOrDefault();

            return;
        }

        txtTitle.Text = _editingMovie.Title ?? string.Empty;

        numDuration.Value = Math.Clamp(
            _editingMovie.DurationMinutes,
            (int)numDuration.Minimum,
            (int)numDuration.Maximum);

        numAgeLimit.Value = Math.Clamp(
            _editingMovie.AgeLimit,
            (int)numAgeLimit.Minimum,
            (int)numAgeLimit.Maximum);

        var releaseDate = _editingMovie.ReleaseDate == default
            ? DateTime.Today
            : _editingMovie.ReleaseDate;

        if (releaseDate < dtpReleaseDate.MinDate)
            releaseDate = dtpReleaseDate.MinDate;
        else if (releaseDate > dtpReleaseDate.MaxDate)
            releaseDate = dtpReleaseDate.MaxDate;

        dtpReleaseDate.Value = releaseDate;
        cboStatus.SelectedItem = _editingMovie.Status;

        txtDescription.Text =
            _editingMovie.Description ?? string.Empty;

        txtPosterPath.Text =
            _editingMovie.PosterPath ?? string.Empty;

        txtTrailerUrl.Text =
            _editingMovie.TrailerUrl ?? string.Empty;

        txtVideoPath.Text =
            _editingMovie.VideoPath ?? string.Empty;

        LoadPosterPreview(_editingMovie.PosterPath);

        if (_genreService != null && clbGenres.Enabled)
        {
            var selectedGenreIds = _movieService
                .GetGenreIds(_editingMovie.Id)
                .ToHashSet();

            for (var index = 0; index < clbGenres.Items.Count; index++)
            {
                if (clbGenres.Items[index] is Genre genre &&
                    selectedGenreIds.Contains(genre.Id))
                {
                    clbGenres.SetItemChecked(index, true);
                }
            }
        }
    }

    private void BtnChoosePoster_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Chọn poster phim",
            Filter = "Ảnh poster|*.jpg;*.jpeg;*.png",
            Multiselect = false,
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        txtPosterPath.Text = dialog.FileName;
        LoadPosterPreview(dialog.FileName);
    }

    private void BtnChooseVideo_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Chọn video phim",
            Filter = "Video|*.mp4;*.mkv;*.avi;*.mov;*.wmv;*.m4v",
            Multiselect = false,
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        txtVideoPath.Text = dialog.FileName;
    }

    private void LoadPosterPreview(string? path)
    {
        picPoster.Image = null;

        _previewImage?.Dispose();
        _previewImage = null;

        if (string.IsNullOrWhiteSpace(path))
            return;

        try
        {
            var fullPath = StoragePathResolver.ResolvePath(path);

            if (!File.Exists(fullPath))
                return;

            using var source = Image.FromFile(fullPath);

            _previewImage = new Bitmap(source);
            picPoster.Image = _previewImage;
        }
        catch
        {
            MessageBox.Show(
                "Không thể đọc ảnh poster đã chọn.",
                "Ảnh không hợp lệ",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private string PreparePosterPathForSaving(string? storedPath)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
            return string.Empty;

        var fullPath = StoragePathResolver.ResolvePath(storedPath);

        if (!File.Exists(fullPath))
            return storedPath;

        var dataRoot = Path.GetFullPath(StoragePathResolver.DataRoot);

        var relativeToData = Path.GetRelativePath(
            dataRoot,
            Path.GetFullPath(fullPath));

        var isInsideData =
            !Path.IsPathRooted(relativeToData) &&
            relativeToData != ".." &&
            !relativeToData.StartsWith(
                ".." + Path.DirectorySeparatorChar,
                StringComparison.Ordinal);

        if (isInsideData)
            return StoragePathResolver.ToRelativePath(fullPath);

        var savedPath = ImageHelper.SavePoster(fullPath);
        TrackManagedAsset(savedPath);
        return savedPath;
    }

    private string PrepareVideoPathForSaving(string? storedPath)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
            return string.Empty;

        var fullPath = StoragePathResolver.ResolvePath(storedPath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                "Không tìm thấy file video đã chọn.",
                fullPath);
        }

        var dataRoot = Path.GetFullPath(StoragePathResolver.DataRoot);

        var relativeToData = Path.GetRelativePath(
            dataRoot,
            Path.GetFullPath(fullPath));

        var isInsideData =
            !Path.IsPathRooted(relativeToData) &&
            relativeToData != ".." &&
            !relativeToData.StartsWith(
                ".." + Path.DirectorySeparatorChar,
                StringComparison.Ordinal);

        if (isInsideData)
            return StoragePathResolver.ToRelativePath(fullPath);

        Directory.CreateDirectory(StoragePathResolver.MoviesDirectory);

        var extension = Path.GetExtension(fullPath);
        var fileName = $"{Guid.NewGuid():N}{extension}";

        var destination = Path.Combine(
            StoragePathResolver.MoviesDirectory,
            fileName);

        File.Copy(fullPath, destination, false);

        var relativePath = StoragePathResolver.ToRelativePath(destination);
        TrackManagedAsset(relativePath);
        return relativePath;
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(txtTitle.Text))
            {
                MessageBox.Show(
                    "Vui lòng nhập tên phim.",
                    "Thiếu thông tin",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                txtTitle.Focus();
                return;
            }

            if (cboStatus.SelectedItem is not MovieStatus selectedStatus)
            {
                MessageBox.Show(
                    "Vui lòng chọn trạng thái phim.",
                    "Thiếu thông tin",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            var trailer = txtTrailerUrl.Text.Trim();

            if (!string.IsNullOrWhiteSpace(trailer) &&
                (!Uri.TryCreate(trailer, UriKind.Absolute, out var uri) ||
                 (uri.Scheme != Uri.UriSchemeHttp &&
                  uri.Scheme != Uri.UriSchemeHttps)))
            {
                MessageBox.Show(
                    "Đường dẫn trailer phải là URL hợp lệ bắt đầu bằng http:// hoặc https://.",
                    "Đường dẫn không hợp lệ",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                txtTrailerUrl.Focus();
                return;
            }

            List<int>? selectedGenreIds = null;

            if (_genreService != null && clbGenres.Enabled)
            {
                selectedGenreIds = clbGenres.CheckedItems
                    .Cast<Genre>()
                    .Select(genre => genre.Id)
                    .Distinct()
                    .ToList();
            }

            var movie = _editingMovie is null
                ? new Movie()
                : CloneMovie(_editingMovie);

            movie.Title = txtTitle.Text.Trim();
            movie.DurationMinutes = (int)numDuration.Value;
            movie.AgeLimit = (int)numAgeLimit.Value;
            movie.ReleaseDate = dtpReleaseDate.Value.Date;
            movie.Status = selectedStatus;
            movie.Description = txtDescription.Text.Trim();

            movie.PosterPath = PreparePosterPathForSaving(
                txtPosterPath.Text.Trim());

            movie.TrailerUrl = trailer;

            movie.VideoPath = PrepareVideoPathForSaving(
                txtVideoPath.Text.Trim());

            _movieService.SaveMovie(movie, selectedGenreIds);
            _assetsCommitted = true;

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex, "Không thể lưu phim.");
        }
    }

    private static Movie CloneMovie(Movie movie)
    {
        return new Movie
        {
            Id = movie.Id,
            Title = movie.Title,
            Description = movie.Description,
            DurationMinutes = movie.DurationMinutes,
            PosterPath = movie.PosterPath,
            TrailerUrl = movie.TrailerUrl,
            VideoPath = movie.VideoPath,
            AgeLimit = movie.AgeLimit,
            ReleaseDate = movie.ReleaseDate,
            Status = movie.Status
        };
    }

    private void TrackManagedAsset(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return;

        var fullPath = StoragePathResolver.ResolvePath(relativePath);
        if (!_managedAssetsCreated.Contains(fullPath, StringComparer.OrdinalIgnoreCase))
            _managedAssetsCreated.Add(fullPath);
    }

    private void DisposeUncommittedAssets()
    {
        if (picPoster is not null && !picPoster.IsDisposed)
            picPoster.Image = null;

        _previewImage?.Dispose();
        _previewImage = null;

        if (_assetsCommitted)
            return;

        foreach (var path in _managedAssetsCreated)
        {
            DeleteManagedAsset(path);
        }

        _managedAssetsCreated.Clear();
    }

    private static void DeleteManagedAsset(string path)
    {
        try
        {
            var fullPath = StoragePathResolver.ResolvePath(path);
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }
        catch (Exception ex)
        {
            Trace.TraceError("Could not remove uncommitted movie asset {0}: {1}", path, ex);
        }
    }
}
