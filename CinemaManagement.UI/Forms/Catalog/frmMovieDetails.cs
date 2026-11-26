
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;
using CinemaManagement.Core.Storage;
using CinemaManagement.UI.Controls;
using CinemaManagement.UI.Forms.Media;
using CinemaManagement.UI.Helpers;

namespace CinemaManagement.UI.Forms.Catalog;

public partial class frmMovieDetails : Form
{
    private readonly Movie _movie;
    private PictureBox picPoster = null!;
    private Image? _posterImage;
    private Button btnTrailer = null!;
    private Button btnPlay = null!;

    public frmMovieDetails(Movie movie)
    {
        ThemeHelper.ConfigureDpiScaling(this);
        _movie = movie ?? throw new ArgumentNullException(nameof(movie));

        BuildLayout();

        FormClosed += (_, _) =>
        {
            picPoster.Image = null;
            _posterImage?.Dispose();
            _posterImage = null;
        };

        LoadPoster();
        ThemeHelper.Apply(this);
        ThemeHelper.FitToWorkingArea(this, new Size(850, 650), new Size(800, 520));
    }

    private void BuildLayout()
    {
        Text = "Chi tiết phim";
        Size = new Size(850, 650);
        MinimumSize = new Size(800, 520);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        BackColor = Color.FromArgb(245, 247, 250);
        Font = new Font("Segoe UI", 9F);

        Controls.Clear();

        var header = new PageHeader(
            "Chi tiết phim",
            "Thông tin phát hành, nội dung và tài nguyên đa phương tiện.");

        var content = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(22),
            AutoScroll = true
        };

        var posterPanel = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(260, 490),
            BackColor = Color.White,
            Padding = new Padding(15)
        };

        posterPanel.Controls.Add(new Label
        {
            Text = "POSTER PHIM",
            Location = new Point(15, 15),
            AutoSize = true,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = ThemeHelper.Primary
        });

        picPoster = new PictureBox
        {
            Location = new Point(30, 55),
            Size = new Size(200, 285),
            BorderStyle = BorderStyle.FixedSingle,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.FromArgb(241, 245, 249)
        };

        posterPanel.Controls.Add(picPoster);

        btnTrailer = CreateActionButton(
            "XEM TRAILER",
            Color.FromArgb(220, 38, 38),
            30,
            355,
            200);

        btnTrailer.Click += BtnTrailer_Click;
        posterPanel.Controls.Add(btnTrailer);

        btnPlay = CreateActionButton(
            "▶ PHÁT PHIM",
            Color.FromArgb(22, 163, 74),
            30,
            402,
            200);

        var videoAvailable = GetVideoStatus() == "Đã có video";
        btnPlay.Enabled = videoAvailable;
        btnPlay.AccessibleName = videoAvailable
            ? "Phát phim"
            : "Phát phim (chưa khả dụng)";
        btnPlay.AccessibleDescription = videoAvailable
            ? "Phát video đã gắn với phim này."
            : "Chưa tìm thấy file video của phim này.";

        btnPlay.Click += BtnPlay_Click;
        posterPanel.Controls.Add(btnPlay);

        var infoPanel = new Panel
        {
            Location = new Point(275, 0),
            Size = new Size(510, 490),
            BackColor = Color.White,
            Padding = new Padding(18)
        };

        infoPanel.Controls.Add(new Label
        {
            Text = _movie.Title,
            Location = new Point(18, 18),
            Size = new Size(465, 55),
            Font = new Font("Segoe UI", 16F, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 41, 59)
        });

        AddInfo(infoPanel, "Mã phim", _movie.Id.ToString(), 90);

        AddInfo(
            infoPanel,
            "Thời lượng",
            $"{_movie.DurationMinutes} phút",
            130);

        AddInfo(
            infoPanel,
            "Giới hạn độ tuổi",
            _movie.AgeLimit > 0 ? $"{_movie.AgeLimit}+" : "Chưa xác định",
            170);

        AddInfo(
            infoPanel,
            "Ngày phát hành",
            _movie.ReleaseDate == default
                ? "Chưa cập nhật"
                : _movie.ReleaseDate.ToString("dd/MM/yyyy"),
            210);

        AddInfo(
            infoPanel,
            "Trạng thái",
            GetStatusText(_movie.Status),
            250);

        AddInfo(
            infoPanel,
            "Video",
            GetVideoStatus(),
            290);

        infoPanel.Controls.Add(new Label
        {
            Text = "MÔ TẢ NỘI DUNG",
            Location = new Point(18, 330),
            AutoSize = true,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = ThemeHelper.Primary
        });

        var txtDescription = new TextBox
        {
            Location = new Point(18, 355),
            Size = new Size(465, 110),
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Text = string.IsNullOrWhiteSpace(_movie.Description)
                ? "Chưa có mô tả nội dung."
                : _movie.Description,
            Font = new Font("Segoe UI", 9F),
            BackColor = Color.White
        };

        infoPanel.Controls.Add(txtDescription);

        content.Controls.Add(posterPanel);
        content.Controls.Add(infoPanel);

        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            BackColor = Color.White
        };

        var btnClose = new Button
        {
            Text = "Đóng",
            Location = new Point(700, 12),
            Size = new Size(100, 35),
            BackColor = Color.FromArgb(226, 232, 240),
            ForeColor = Color.FromArgb(51, 65, 85),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };

        btnClose.FlatAppearance.BorderSize = 0;
        btnClose.Click += (_, _) => Close();

        footer.Controls.Add(btnClose);

        Controls.Add(content);
        Controls.Add(footer);
        Controls.Add(header);

        AcceptButton = btnClose;

        btnTrailer.Enabled =
            Uri.TryCreate(
                _movie.TrailerUrl,
                UriKind.Absolute,
                out var trailerUri) &&
            (trailerUri.Scheme == Uri.UriSchemeHttp ||
             trailerUri.Scheme == Uri.UriSchemeHttps);
    }

    private static Button CreateActionButton(
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
            Size = new Size(width, 38),
            BackColor = color,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        var role = text.Contains("PHÁT", StringComparison.OrdinalIgnoreCase)
            ? ThemeHelper.ActionButtonRole.Primary
            : ThemeHelper.ActionButtonRole.Secondary;
        ThemeHelper.StyleActionButton(button, role);

        return button;
    }

    private static void AddInfo(
        Control parent,
        string title,
        string value,
        int y)
    {
        parent.Controls.Add(new Label
        {
            Text = title + ":",
            Location = new Point(18, y),
            Size = new Size(145, 25),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(71, 85, 105)
        });

        parent.Controls.Add(new Label
        {
            Text = value,
            Location = new Point(165, y),
            Size = new Size(315, 25),
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.FromArgb(30, 41, 59)
        });
    }

    private string GetVideoStatus()
    {
        if (string.IsNullOrWhiteSpace(_movie.VideoPath))
            return "Chưa gắn video";

        try
        {
            var path = StoragePathResolver.ResolvePath(_movie.VideoPath);

            return File.Exists(path)
                ? "Đã có video"
                : "Không tìm thấy file";
        }
        catch
        {
            return "Đường dẫn không hợp lệ";
        }
    }

    private void LoadPoster()
    {
        var path = _movie.PosterPath;

        if (string.IsNullOrWhiteSpace(path))
            return;

        try
        {
            var resolvedPath = StoragePathResolver.ResolvePath(path);

            if (!File.Exists(resolvedPath))
                return;

            using var source = Image.FromFile(resolvedPath);

            _posterImage = new Bitmap(source);
            picPoster.Image = _posterImage;
        }
        catch
        {
        }
    }

    private void BtnPlay_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_movie.VideoPath))
        {
            MessageHelper.Info(
                "Phim này chưa được gắn video.\n\n" +
                "Hãy chỉnh sửa phim và chọn file video trong thư mục lưu trữ.",
                "Chưa có video");

            return;
        }

        try
        {
            var videoPath =
                StoragePathResolver.ResolvePath(_movie.VideoPath);

            if (!File.Exists(videoPath))
            {
                MessageHelper.Warning(
                    "Không tìm thấy file video:\n" + videoPath,
                    "Không tìm thấy video");

                return;
            }

            using var player = new frmVideoPlayer(_movie.VideoPath);
            player.ShowDialog(this);
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex, "Không thể phát phim.");
        }
    }

    private void BtnTrailer_Click(object? sender, EventArgs e)
    {
        var trailerUrl = _movie.TrailerUrl?.Trim();

        if (!Uri.TryCreate(
                trailerUrl,
                UriKind.Absolute,
                out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp &&
             uri.Scheme != Uri.UriSchemeHttps))
        {
            MessageHelper.Info(
                "Phim này chưa có đường dẫn trailer hợp lệ.",
                "Trailer");

            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = uri.AbsoluteUri,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex, "Không thể mở trailer.");
        }
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
}
