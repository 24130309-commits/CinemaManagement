
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
using CinemaManagement.UI.Forms.Media;
using CinemaManagement.UI.Helpers;

namespace CinemaManagement.UI.Forms.Catalog;

public sealed class frmMovieDiscovery : UserControl
{
    private readonly IMovieService _movieService;
    private readonly IGenreService _genreService;

    private readonly List<Image> _images = new();
    private readonly Dictionary<int, HashSet<int>> _movieGenres = new();

    private List<Movie> _movies = new();
    private List<Genre> _genres = new();

    private TextBox _txtSearch = null!;
    private ComboBox _cboGenre = null!;
    private ComboBox _cboStatus = null!;
    private FlowLayoutPanel _movieFlow = null!;
    private Panel _heroPanel = null!;
    private Label _lblResult = null!;
    private FlowLayoutPanel _toolbar = null!;
    private Label _featuredTitle = null!;
    private Label _featuredDescription = null!;
    private FlowLayoutPanel _featuredActions = null!;

    private static readonly Color BackgroundColor =
        ThemeHelper.PrimaryDark;

    private static readonly Color CardColor =
        Color.FromArgb(30, 41, 59);

    public frmMovieDiscovery(
        IMovieService movieService,
        IGenreService genreService)
    {
        _movieService = movieService
            ?? throw new ArgumentNullException(nameof(movieService));

        _genreService = genreService
            ?? throw new ArgumentNullException(nameof(genreService));

        BuildUI();
        LoadCatalog();

        Disposed += (_, _) => DisposeImages();
    }

    private void BuildUI()
    {
        ThemeHelper.ConfigureDpiScaling(this);
        Text = "Khám phá phim";
        BackColor = BackgroundColor;
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 9F);
        MinimumSize = Size.Empty;
        Padding = new Padding(18, 12, 18, 14);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            BackColor = BackgroundColor,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 184));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var title = new Label
        {
            Text = "Thư viện phim",
            Dock = DockStyle.Top,
            Padding = new Padding(0, 4, 0, 3),
            Font = new Font("Segoe UI Semibold", 19F, FontStyle.Bold),
            ForeColor = Color.White,
            AutoEllipsis = false,
            TextAlign = ContentAlignment.MiddleLeft
        };
        title.Height = TextRenderer.MeasureText(title.Text, title.Font).Height +
            title.Padding.Vertical + 6;

        var subtitle = new Label
        {
            Text = "Tìm kiếm và xem nhanh nội dung phim trong danh mục của rạp.",
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(168, 181, 201),
            Font = new Font("Segoe UI", 9.5F),
            AutoEllipsis = false,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var heading = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = BackgroundColor,
            Padding = new Padding(2, 0, 2, 3)
        };
        heading.Controls.Add(subtitle);
        heading.Controls.Add(title);
        void FitHeading()
        {
            var titleHeight = TextRenderer.MeasureText(title.Text, title.Font).Height +
                title.Padding.Vertical + 6;
            title.Height = titleHeight;
            root.RowStyles[0].Height = Math.Max(78, titleHeight + 34);
        }
        heading.Resize += (_, _) => FitHeading();

        _toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            Height = 54,
            Padding = new Padding(0, 7, 0, 5),
            BackColor = BackgroundColor,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoScroll = true,
            Margin = Padding.Empty
        };

        _txtSearch = new TextBox
        {
            Width = 250,
            Height = 34,
            Margin = new Padding(0, 2, 10, 3),
            Font = new Font("Segoe UI", 9.5F),
            PlaceholderText = "Tìm theo tên phim...",
            AccessibleName = "Tìm theo tên phim"
        };

        _cboGenre = new ComboBox
        {
            Width = 185,
            Height = 34,
            Margin = new Padding(0, 2, 10, 3),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9.5F),
            AccessibleName = "Lọc theo thể loại"
        };

        _cboStatus = new ComboBox
        {
            Width = 165,
            Height = 34,
            Margin = new Padding(0, 2, 10, 3),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9.5F),
            AccessibleName = "Lọc theo trạng thái phim"
        };

        _cboStatus.Items.Add("Tất cả phim");
        _cboStatus.Items.Add("Đang chiếu");
        _cboStatus.Items.Add("Sắp chiếu");
        _cboStatus.Items.Add("Đã kết thúc");
        _cboStatus.SelectedIndex = 0;

        _txtSearch.TextChanged += (_, _) => ApplyFilters();
        _cboGenre.SelectedIndexChanged += (_, _) => ApplyFilters();
        _cboStatus.SelectedIndexChanged += (_, _) => ApplyFilters();

        _toolbar.Controls.AddRange(new Control[] { _txtSearch, _cboGenre, _cboStatus });

        _heroPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 2, 0, 8),
            Padding = new Padding(14),
            BackColor = CardColor,
            AccessibleName = "Phim nổi bật"
        };

        _lblResult = new Label
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(2, 5, 0, 0),
            Text = "TẤT CẢ PHIM",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = BackgroundColor
        };

        _movieFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(20, 10, 20, 20),
            BackColor = BackgroundColor
        };

        root.Controls.Add(heading, 0, 0);
        root.Controls.Add(_toolbar, 0, 1);
        root.Controls.Add(_heroPanel, 0, 2);
        root.Controls.Add(_lblResult, 0, 3);
        root.Controls.Add(_movieFlow, 0, 4);
        Controls.Add(root);

        _heroPanel.Resize += (_, _) => LayoutFeaturedContent();
        _movieFlow.Resize += (_, _) => ResizeMovieCards();
    }

    private void LoadCatalog()
    {
        try
        {
            _movies = _movieService.GetAllMovies().ToList();
            _genres = _genreService.GetAll().ToList();

            _movieGenres.Clear();

            foreach (var movie in _movies)
            {
                _movieGenres[movie.Id] = _movieService
                    .GetGenreIds(movie.Id)
                    .ToHashSet();
            }

            _cboGenre.Items.Clear();
            _cboGenre.Items.Add("Tất cả thể loại");

            foreach (var genre in _genres.OrderBy(g => g.Name))
                _cboGenre.Items.Add(genre);

            _cboGenre.DisplayMember = "Name";
            _cboGenre.SelectedIndex = 0;

            ApplyFilters();
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex, "Không thể tải danh mục phim.");
        }
    }

    private void BuildHero()
    {
        DisposeChildren(_heroPanel);

        var featured = _movies
            .OrderByDescending(m => m.Status == MovieStatus.NowShowing)
            .ThenByDescending(m => m.ReleaseDate)
            .FirstOrDefault();

        if (featured == null)
        {
            _heroPanel.Controls.Add(new Label
            {
                Text = "Chưa có phim trong danh mục",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(181, 194, 211),
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold)
            });

            return;
        }

        var poster = new PictureBox
        {
            Dock = DockStyle.Left,
            Width = 112,
            Margin = new Padding(0, 0, 16, 0),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = ThemeHelper.PrimaryDark,
            AccessibleName = $"Poster phim {featured.Title}"
        };

        poster.Image = LoadImage(featured.PosterPath);
        AddPosterFallback(poster);

        var content = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = CardColor,
            Padding = new Padding(3, 2, 2, 1)
        };

        var kicker = new Label
        {
            Text = "ĐƯỢC ƯU TIÊN HIỂN THỊ",
            Dock = DockStyle.Top,
            Height = 21,
            ForeColor = Color.FromArgb(236, 157, 132),
            Font = new Font("Segoe UI", 8F, FontStyle.Bold)
        };

        _featuredTitle = new Label
        {
            Text = featured.Title,
            Dock = DockStyle.Top,
            Height = 44,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold),
            AutoEllipsis = false,
            TextAlign = ContentAlignment.MiddleLeft,
            AccessibleName = $"Phim nổi bật: {featured.Title}"
        };

        _featuredDescription = new Label
        {
            Text = string.IsNullOrWhiteSpace(featured.Description)
                ? "Chưa có mô tả cho bộ phim này."
                : featured.Description,
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(198, 210, 226),
            Font = new Font("Segoe UI", 9F),
            AutoEllipsis = false,
            AccessibleName = "Mô tả phim nổi bật"
        };

        _featuredActions = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 38,
            WrapContents = false,
            AutoScroll = true,
            BackColor = CardColor,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        var btnDetails = CreateButton(
            "Xem chi tiết",
            ThemeHelper.ActionButtonRole.Secondary,
            124);
        var btnPlay = CreateButton(
            "Phát phim",
            ThemeHelper.ActionButtonRole.Primary,
            112);

        btnDetails.Click += (_, _) => OpenDetails(featured);
        btnPlay.Click += (_, _) => PlayMovie(featured);

        _featuredActions.Controls.AddRange(new Control[] { btnDetails, btnPlay });
        content.Controls.Add(_featuredDescription);
        content.Controls.Add(_featuredActions);
        content.Controls.Add(_featuredTitle);
        content.Controls.Add(kicker);
        _heroPanel.Controls.Add(content);
        _heroPanel.Controls.Add(poster);
        LayoutFeaturedContent();
    }

    private void ApplyFilters()
    {
        if (_movieFlow == null)
            return;

        DisposeChildren(_heroPanel);
        DisposeChildren(_movieFlow);
        DisposeImages();

        BuildHero();

        IEnumerable<Movie> result = _movies;

        var keyword = _txtSearch.Text.Trim();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            result = result.Where(m =>
                m.Title.Contains(
                    keyword,
                    StringComparison.OrdinalIgnoreCase));
        }

        if (_cboGenre.SelectedItem is Genre selectedGenre)
        {
            result = result.Where(m =>
                _movieGenres.TryGetValue(m.Id, out var ids) &&
                ids.Contains(selectedGenre.Id));
        }

        result = _cboStatus.SelectedIndex switch
        {
            1 => result.Where(m => m.Status == MovieStatus.NowShowing),
            2 => result.Where(m => m.Status == MovieStatus.ComingSoon),
            3 => result.Where(m => m.Status == MovieStatus.Ended),
            _ => result
        };

        var filtered = result
            .OrderByDescending(m => m.Status == MovieStatus.NowShowing)
            .ThenByDescending(m => m.ReleaseDate)
            .ToList();

        _lblResult.Text = $"DANH MỤC PHIM  ·  {filtered.Count} KẾT QUẢ";

        foreach (var movie in filtered)
            _movieFlow.Controls.Add(CreateMovieCard(movie));
        ResizeMovieCards();

        if (filtered.Count == 0)
        {
            _movieFlow.Controls.Add(new Label
            {
                Text = "Không tìm thấy phim phù hợp. Hãy thử đổi từ khóa hoặc bộ lọc.",
                AutoSize = false,
                Width = Math.Max(300, _movieFlow.ClientSize.Width - 48),
                Height = 72,
                Margin = new Padding(8, 12, 8, 8),
                Padding = new Padding(16),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = CardColor,
                ForeColor = Color.FromArgb(198, 210, 226),
                Font = new Font("Segoe UI", 9.5F)
            });
        }
    }

    private Control CreateMovieCard(Movie movie)
    {
        var card = new Panel
        {
            Width = 216,
            Height = 410,
            Margin = new Padding(7),
            BackColor = CardColor,
            Cursor = Cursors.Hand,
            AccessibleName = $"Phim {movie.Title}, {GetStatusText(movie.Status)}"
        };

        var poster = new PictureBox
        {
            Location = new Point(8, 8),
            Size = new Size(200, 246),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = ThemeHelper.PrimaryDark,
            Cursor = Cursors.Hand
        };

        poster.Image = LoadImage(movie.PosterPath);
        AddPosterFallback(poster);

        var title = new Label
        {
            Text = movie.Title,
            Location = new Point(12, 263),
            Size = new Size(192, 50),
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
            AutoEllipsis = false,
            TextAlign = ContentAlignment.TopLeft,
            Padding = new Padding(0, 2, 0, 0),
            AccessibleName = movie.Title
        };
        title.Height = Math.Max(
            50,
            TextRenderer.MeasureText("Ag", title.Font).Height * 2 + title.Padding.Vertical + 4);

        var status = new Label
        {
            Text = GetStatusText(movie.Status),
            Location = new Point(12, 263 + title.Height + 2),
            Size = new Size(192, 24),
            ForeColor = GetStatusColor(movie.Status),
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var details = new Button
        {
            Text = "Chi tiết",
            Location = new Point(12, 263 + title.Height + 34),
            Size = new Size(88, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(56, 72, 95),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
            Cursor = Cursors.Hand,
            AccessibleName = $"Xem chi tiết phim {movie.Title}"
        };

        details.FlatAppearance.BorderSize = 0;
        details.Click += (_, _) => OpenDetails(movie);

        var play = new Button
        {
            Text = "Phát phim",
            Location = new Point(108, 263 + title.Height + 34),
            Size = new Size(96, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(54, 119, 102),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
            Cursor = Cursors.Hand,
            AccessibleName = $"Phát phim {movie.Title}"
        };

        play.FlatAppearance.BorderSize = 0;
        play.Click += (_, _) => PlayMovie(movie);

        card.Controls.Add(poster);
        card.Controls.Add(title);
        card.Controls.Add(status);
        card.Controls.Add(details);
        card.Controls.Add(play);

        card.Resize += (_, _) =>
        {
            poster.Width = Math.Max(120, card.ClientSize.Width - 16);
            title.Width = Math.Max(100, card.ClientSize.Width - 24);
            title.Height = Math.Clamp(
                TextRenderer.MeasureText(
                    title.Text,
                    title.Font,
                    new Size(title.Width, int.MaxValue),
                    TextFormatFlags.WordBreak | TextFormatFlags.NoPadding).Height + 8,
                50,
                88);
            status.Width = Math.Max(100, card.ClientSize.Width - 24);
            status.Top = title.Bottom + 2;
            details.Top = title.Bottom + 34;
            play.Top = details.Top;
            card.Height = Math.Max(410, details.Bottom + 8);
            var actionGap = 6;
            var actionWidth = Math.Max(60, (card.ClientSize.Width - 24 - actionGap) / 2);
            details.Width = actionWidth;
            play.Left = details.Right + actionGap;
            play.Width = Math.Max(60, card.ClientSize.Width - play.Left - 12);
        };

        card.Click += (_, _) => OpenDetails(movie);
        poster.Click += (_, _) => OpenDetails(movie);
        title.Click += (_, _) => OpenDetails(movie);

        return card;
    }

    private void PlayMovie(Movie movie)
    {
        if (string.IsNullOrWhiteSpace(movie.VideoPath))
        {
            MessageBox.Show(
                this,
                $"Phim \"{movie.Title}\" chưa được gắn video.\n\n" +
                "Hãy vào chức năng chỉnh sửa phim và chọn file video.",
                "Chưa có video",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        try
        {
            var videoPath = StoragePathResolver.ResolvePath(movie.VideoPath);

            if (!File.Exists(videoPath))
            {
                MessageBox.Show(
                    this,
                    $"Không tìm thấy file video:\n{videoPath}\n\n" +
                    "Hãy kiểm tra lại thư mục Data/Movies.",
                    "Không tìm thấy video",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            using var player = new frmVideoPlayer(movie.VideoPath);
            player.ShowDialog(this);
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex, "Không thể phát phim.");
        }
    }

    private void OpenDetails(Movie movie)
    {
        using var form = new frmMovieDetails(movie);
        form.ShowDialog(this);
    }

    private Image? LoadImage(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        try
        {
            var imagePath = StoragePathResolver.ResolvePath(path);

            if (!File.Exists(imagePath))
                return null;

            using var source = Image.FromFile(imagePath);

            var copy = new Bitmap(source);
            _images.Add(copy);

            return copy;
        }
        catch
        {
            return null;
        }
    }

    private void DisposeImages()
    {
        foreach (var image in _images)
            image.Dispose();

        _images.Clear();
    }

    private static void AddPosterFallback(PictureBox poster)
    {
        if (poster.Image != null)
            return;

        poster.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "Chưa có\nposter",
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.FromArgb(148, 163, 184),
            BackColor = Color.FromArgb(20, 31, 49),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            AccessibleName = "Chưa có ảnh poster"
        });
    }

    private static void DisposeChildren(Control parent)
    {
        while (parent.Controls.Count > 0)
        {
            var child = parent.Controls[0];
            parent.Controls.RemoveAt(0);
            child.Dispose();
        }
    }

    private static Button CreateButton(
        string text,
        ThemeHelper.ActionButtonRole role,
        int width)
    {
        var button = new Button
        {
            Text = text,
            Size = new Size(width, 34),
            Margin = new Padding(0, 2, 8, 2),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        ThemeHelper.StyleActionButton(button, role);

        return button;
    }

    private void LayoutFeaturedContent()
    {
        if (_featuredTitle == null || _featuredDescription == null || _featuredActions == null)
            return;

        var compact = _heroPanel.ClientSize.Width < 620;
        if (_heroPanel.Controls.Count > 1 && _heroPanel.Controls[1] is Panel content)
        {
            content.Padding = compact
                ? new Padding(0, 0, 0, 0)
                : new Padding(3, 2, 2, 1);
            if (_heroPanel.Controls[0] is PictureBox poster)
            {
                poster.Width = compact ? 82 : 112;
                poster.Margin = new Padding(0, 0, compact ? 10 : 16, 0);
            }
        }

        var titleHeight = TextRenderer.MeasureText(
            _featuredTitle.Text,
            _featuredTitle.Font,
            new Size(Math.Max(120, _heroPanel.ClientSize.Width - (compact ? 112 : 150)), int.MaxValue),
            TextFormatFlags.WordBreak | TextFormatFlags.NoPadding).Height;
        _featuredTitle.Height = Math.Max(44, titleHeight + 10);
    }

    private void ResizeMovieCards()
    {
        if (_movieFlow == null || _movieFlow.ClientSize.Width <= 0)
            return;

        const int minWidth = 190;
        const int maxWidth = 226;
        const int gap = 14;
        var available = Math.Max(minWidth, _movieFlow.ClientSize.Width - 44);
        var columns = Math.Max(1, available / (minWidth + gap));
        var cardWidth = Math.Clamp((available - (columns - 1) * gap) / columns, minWidth, maxWidth);

        foreach (Control control in _movieFlow.Controls)
        {
            if (control is not Panel card)
                continue;

            card.Width = cardWidth;
        }
    }

    private static string GetStatusText(MovieStatus status)
    {
        return status switch
        {
            MovieStatus.NowShowing => "Đang chiếu",
            MovieStatus.ComingSoon => "Sắp chiếu",
            MovieStatus.Ended => "Đã kết thúc",
            _ => status.ToString()
        };
    }

    private static Color GetStatusColor(MovieStatus status)
    {
        return status switch
        {
            MovieStatus.NowShowing => Color.FromArgb(74, 222, 128),
            MovieStatus.ComingSoon => Color.FromArgb(96, 165, 250),
            _ => Color.FromArgb(148, 163, 184)
        };
    }
}
