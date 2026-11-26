
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using CinemaManagement.Core.Storage;
using CinemaManagement.UI.Helpers;
using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;
using VlcMedia = LibVLCSharp.Shared.Media;

namespace CinemaManagement.UI.Forms.Media;

public sealed class frmVideoPlayer : Form
{
    private LibVLC? _libVlc;
    private MediaPlayer? _mediaPlayer;
    private VlcMedia? _currentMedia;

    private readonly VideoView _videoView;
    private readonly Button _btnPlayPause;
    private readonly Button _btnStop;
    private readonly Button _btnOpen;
    private readonly Button _btnBackward;
    private readonly Button _btnForward;
    private readonly Button _btnMute;
    private readonly Button _btnFullscreen;
    private readonly TrackBar _volume;
    private readonly TrackBar _timeline;
    private readonly Label _lblTime;
    private readonly Label _lblStatus;
    private readonly System.Windows.Forms.Timer _timer;

    private bool _isSeeking;
    private bool _isReleased;
    private bool _hasMedia;
    private bool _isMuted;
    private bool _isFullscreen;
    private int _previousVolume = 80;
    private FormBorderStyle _previousBorderStyle;
    private FormWindowState _previousWindowState;
    private Rectangle _previousBounds;

    public frmVideoPlayer() : this(null)
    {
    }

    public frmVideoPlayer(string? mediaPathOrUrl)
    {
        ThemeHelper.ConfigureDpiScaling(this);
        Text = "Cinema Management - Cinema Player";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(950, 600);
        Size = new Size(1200, 760);
        BackColor = Color.Black;
        ForeColor = Color.White;
        KeyPreview = true;

        var top = new Panel
        {
            Dock = DockStyle.Top,
            Height = 56,
            BackColor = Color.FromArgb(20, 24, 32),
            Padding = new Padding(18, 0, 18, 0)
        };

        var titleLabel = new Label
        {
            Text = "CINEMA PLAYER",
            Dock = DockStyle.Left,
            Width = 220,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            ForeColor = Color.White
        };

        _lblStatus = new Label
        {
            Text = "Chưa mở video",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(170, 184, 202),
            AutoEllipsis = true
        };

        top.Controls.Add(_lblStatus);
        top.Controls.Add(titleLabel);

        _videoView = new VideoView
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Black
        };

        var bottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 126,
            BackColor = Color.FromArgb(20, 24, 32),
            Padding = new Padding(14, 5, 14, 8)
        };

        _timeline = new TrackBar
        {
            Minimum = 0,
            Maximum = 1000,
            Value = 0,
            TickStyle = TickStyle.None,
            Dock = DockStyle.Top,
            Height = 32,
            BackColor = Color.FromArgb(20, 24, 32)
        };

        _timeline.MouseDown += (_, _) =>
        {
            _isSeeking = true;
        };

        _timeline.MouseUp += (_, _) =>
        {
            SeekToTimeline();
            _isSeeking = false;
        };

        var controls = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(0, 12, 0, 0),
            BackColor = Color.FromArgb(20, 24, 32)
        };

        _btnOpen = CreateButton("Mở video", 88);
        _btnPlayPause = CreateButton("Tạm dừng", 105);
        _btnStop = CreateButton("Dừng", 65);
        _btnBackward = CreateButton("−10 giây", 75);
        _btnForward = CreateButton("+10 giây", 75);
        _btnMute = CreateButton("Tắt tiếng", 85);
        _btnFullscreen = CreateButton("Toàn màn hình", 115);

        _btnPlayPause.Enabled = false;
        _btnStop.Enabled = false;
        _btnBackward.Enabled = false;
        _btnForward.Enabled = false;
        _btnMute.Enabled = false;
        _btnFullscreen.Enabled = false;

        _lblTime = new Label
        {
            Text = "00:00 / 00:00",
            Width = 112,
            Height = 36,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = Color.White,
            Margin = new Padding(5, 0, 5, 0)
        };

        var volumeLabel = new Label
        {
            Text = "Âm lượng",
            Width = 58,
            Height = 36,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 8),
            ForeColor = Color.FromArgb(190, 200, 214),
            Margin = new Padding(4, 0, 0, 0)
        };

        _volume = new TrackBar
        {
            Minimum = 0,
            Maximum = 100,
            Value = 80,
            TickStyle = TickStyle.None,
            Width = 115,
            Height = 36,
            BackColor = Color.FromArgb(20, 24, 32),
            Margin = new Padding(0, 0, 4, 0)
        };

        controls.Controls.Add(_btnOpen);
        controls.Controls.Add(_btnPlayPause);
        controls.Controls.Add(_btnStop);
        controls.Controls.Add(_btnBackward);
        controls.Controls.Add(_btnForward);
        controls.Controls.Add(_lblTime);
        controls.Controls.Add(_btnMute);
        controls.Controls.Add(volumeLabel);
        controls.Controls.Add(_volume);
        controls.Controls.Add(_btnFullscreen);

        bottom.Controls.Add(controls);
        bottom.Controls.Add(_timeline);

        _btnOpen.Click += (_, _) => OpenLocalVideo();
        _btnPlayPause.Click += (_, _) => TogglePlayback();
        _btnStop.Click += (_, _) => StopPlayback();
        _btnBackward.Click += (_, _) => SeekBy(-10000);
        _btnForward.Click += (_, _) => SeekBy(10000);
        _btnMute.Click += (_, _) => ToggleMute();
        _btnFullscreen.Click += (_, _) => ToggleFullscreen();

        _volume.Scroll += (_, _) =>
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Volume = _volume.Value;

                if (_volume.Value > 0)
                {
                    _isMuted = false;
                    _btnMute.Text = "Tắt tiếng";
                }
            }
        };

        Controls.Add(_videoView);
        Controls.Add(bottom);
        Controls.Add(top);

        _timer = new System.Windows.Forms.Timer
        {
            Interval = 500
        };

        _timer.Tick += (_, _) => RefreshTimeline();

        try
        {
            LibVLCSharp.Shared.Core.Initialize();

            _libVlc = new LibVLC();

            _mediaPlayer = new MediaPlayer(_libVlc)
            {
                Volume = _volume.Value
            };

            _videoView.MediaPlayer = _mediaPlayer;

            _lblStatus.Text = "Sẵn sàng phát video";

            _timer.Start();
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "Không khởi tạo được VLC";

            _btnOpen.Enabled = false;

            MessageHelper.Handle(ex, "Không thể khởi tạo trình phát video.");
        }

        KeyDown += HandleKeyboard;

        FormClosed += (_, _) => ReleasePlayer();

        if (!string.IsNullOrWhiteSpace(mediaPathOrUrl))
        {
            LoadMedia(mediaPathOrUrl);
        }
    }

    private static Button CreateButton(string text, int width)
    {
        return new Button
        {
            Text = text,
            Width = width,
            Height = 36,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(43, 53, 69),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 8, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Margin = new Padding(3, 0, 3, 0),
            FlatAppearance =
            {
                BorderColor = Color.FromArgb(65, 79, 100)
            }
        };
    }

    private void OpenLocalVideo()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Chọn video",
            Filter =
                "Video (*.mp4;*.mkv;*.avi;*.mov;*.wmv;*.m4v)|" +
                "*.mp4;*.mkv;*.avi;*.mov;*.wmv;*.m4v|" +
                "All files (*.*)|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            LoadMedia(dialog.FileName);
        }
    }

    public void LoadMedia(string pathOrUrl)
    {
        if (_isReleased || _libVlc == null || _mediaPlayer == null)
        {
            MessageBox.Show(
                this,
                "VLC chưa được khởi tạo.",
                "Cinema Player",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            return;
        }

        if (string.IsNullOrWhiteSpace(pathOrUrl))
            return;

        VlcMedia? newMedia = null;

        try
        {
            _mediaPlayer.Stop();

            _currentMedia?.Dispose();
            _currentMedia = null;

            _hasMedia = false;

            _timeline.Value = 0;
            _lblTime.Text = "00:00 / 00:00";

            if (Uri.TryCreate(pathOrUrl, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp ||
                 uri.Scheme == Uri.UriSchemeHttps))
            {
                newMedia = new VlcMedia(_libVlc, uri);
            }
            else
            {
                var fullPath = StoragePathResolver.ResolvePath(pathOrUrl);

                if (!File.Exists(fullPath))
                {
                    throw new FileNotFoundException(
                        "Không tìm thấy file video trong kho dữ liệu.",
                        fullPath);
                }

                newMedia = new VlcMedia(
                    _libVlc,
                    fullPath,
                    FromType.FromPath);
            }

            if (!_mediaPlayer.Play(newMedia))
            {
                throw new InvalidOperationException(
                    "VLC không thể bắt đầu phát video này.");
            }

            _currentMedia = newMedia;
            newMedia = null;

            _hasMedia = true;

            _lblStatus.Text = Path.GetFileName(pathOrUrl);

            _btnPlayPause.Enabled = true;
            _btnStop.Enabled = true;
            _btnBackward.Enabled = true;
            _btnForward.Enabled = true;
            _btnMute.Enabled = true;
            _btnFullscreen.Enabled = true;

            _btnPlayPause.Text = "Tạm dừng";
        }
        catch (Exception ex)
        {
            newMedia?.Dispose();

            _hasMedia = false;

            _btnPlayPause.Enabled = false;
            _btnStop.Enabled = false;
            _btnBackward.Enabled = false;
            _btnForward.Enabled = false;
            _btnMute.Enabled = false;
            _btnFullscreen.Enabled = false;

            _lblStatus.Text = "Không mở được video";

            MessageHelper.Handle(ex, "Không thể mở video đã chọn.");
        }
    }

    private void TogglePlayback()
    {
        if (_mediaPlayer == null || !_hasMedia)
            return;

        if (_mediaPlayer.IsPlaying)
        {
            _mediaPlayer.Pause();
            _btnPlayPause.Text = "Tiếp tục";
        }
        else
        {
            _mediaPlayer.Play();
            _btnPlayPause.Text = "Tạm dừng";
        }
    }

    private void StopPlayback()
    {
        if (_mediaPlayer == null || !_hasMedia)
            return;

        _mediaPlayer.Stop();

        _btnPlayPause.Text = "Phát lại";
        _timeline.Value = 0;
        _lblTime.Text = "00:00 / 00:00";
    }

    private void SeekToTimeline()
    {
        if (_mediaPlayer == null ||
            !_hasMedia ||
            _mediaPlayer.Length <= 0)
            return;

        long targetTime = (long)(
            _timeline.Value / 1000d * _mediaPlayer.Length);

        _mediaPlayer.Time = Math.Clamp(
            targetTime,
            0L,
            _mediaPlayer.Length);
    }

    private void SeekBy(long milliseconds)
    {
        if (_mediaPlayer == null ||
            !_hasMedia ||
            _mediaPlayer.Length <= 0)
            return;

        long target = _mediaPlayer.Time + milliseconds;

        _mediaPlayer.Time = Math.Clamp(
            target,
            0L,
            _mediaPlayer.Length);
    }

    private void ToggleMute()
    {
        if (_mediaPlayer == null)
            return;

        _isMuted = !_isMuted;

        if (_isMuted)
        {
            _previousVolume = Math.Max(1, _volume.Value);
            _mediaPlayer.Volume = 0;
            _btnMute.Text = "Bật tiếng";
        }
        else
        {
            if (_volume.Value == 0)
                _volume.Value = _previousVolume;

            _mediaPlayer.Volume = _volume.Value;
            _btnMute.Text = "Tắt tiếng";
        }
    }

    private void ToggleFullscreen()
    {
        if (!_isFullscreen)
        {
            _previousBorderStyle = FormBorderStyle;
            _previousWindowState = WindowState;
            _previousBounds = Bounds;

            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;

            _isFullscreen = true;
            _btnFullscreen.Text = "Thoát toàn màn hình";
        }
        else
        {
            WindowState = FormWindowState.Normal;
            FormBorderStyle = _previousBorderStyle;
            Bounds = _previousBounds;
            WindowState = _previousWindowState;

            _isFullscreen = false;
            _btnFullscreen.Text = "Toàn màn hình";
        }
    }

    private void HandleKeyboard(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F11)
        {
            ToggleFullscreen();
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Escape && _isFullscreen)
        {
            ToggleFullscreen();
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Space)
        {
            TogglePlayback();
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Left)
        {
            SeekBy(-10000);
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Right)
        {
            SeekBy(10000);
            e.Handled = true;
        }
    }

    private void RefreshTimeline()
    {
        if (_mediaPlayer == null || !_hasMedia || _isSeeking)
            return;

        long current = Math.Max(0, _mediaPlayer.Time);
        long duration = Math.Max(0, _mediaPlayer.Length);

        if (duration <= 0)
            return;

        int value = (int)Math.Clamp(
            current / (double)duration * 1000d,
            0d,
            1000d);

        _timeline.Value = value;

        _lblTime.Text =
            $"{FormatTime(current)} / {FormatTime(duration)}";

        if (_mediaPlayer.State == VLCState.Ended)
        {
            _btnPlayPause.Text = "Phát lại";
        }
        else if (_mediaPlayer.IsPlaying)
        {
            _btnPlayPause.Text = "Tạm dừng";
        }
    }

    private static string FormatTime(long milliseconds)
    {
        var time = TimeSpan.FromMilliseconds(
            Math.Max(0, milliseconds));

        return time.TotalHours >= 1
            ? time.ToString(@"hh\:mm\:ss")
            : time.ToString(@"mm\:ss");
    }

    private void ReleasePlayer()
    {
        if (_isReleased)
            return;

        _isReleased = true;

        _timer.Stop();
        _timer.Dispose();

        if (_mediaPlayer != null)
        {
            _mediaPlayer.Stop();

            _videoView.MediaPlayer = null;

            _mediaPlayer.Dispose();
            _mediaPlayer = null;
        }

        _currentMedia?.Dispose();
        _currentMedia = null;

        _libVlc?.Dispose();
        _libVlc = null;

        _hasMedia = false;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ReleasePlayer();
        }

        base.Dispose(disposing);
    }
}
