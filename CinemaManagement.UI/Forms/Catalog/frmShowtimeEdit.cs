using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.UI.Controls;
using CinemaManagement.UI.Helpers;

namespace CinemaManagement.UI.Forms.Catalog;

public partial class frmShowtimeEdit : Form
{
    private readonly IShowtimesService _showtimeService;
    private readonly IMovieService _movieService;
    private readonly IRoomService _roomService;
    private readonly Showtime? _editingShowtime;

    private ComboBox cboMovie = null!;
    private ComboBox cboRoom = null!;

    private DateTimePicker dtpDate = null!;
    private DateTimePicker dtpTime = null!;

    private NumericUpDown numBasePrice = null!;

    private Label lblMovieInfo = null!;
    private Label lblRoomInfo = null!;
    private Label lblEndTime = null!;
    private readonly ISettingService? _settings;

    private Button btnSave = null!;

    public frmShowtimeEdit(
        IShowtimesService showtimeService,
        IMovieService movieService,
        IRoomService roomService,
        Showtime? showtime = null,
        ISettingService? settings = null)
    {
        _showtimeService = showtimeService
            ?? throw new ArgumentNullException(nameof(showtimeService));

        _movieService = movieService
            ?? throw new ArgumentNullException(nameof(movieService));

        _roomService = roomService
            ?? throw new ArgumentNullException(nameof(roomService));

        _editingShowtime = showtime;
        _settings = settings;

        InitializeComponent();
        BuildLayout();
        LoadSources();

        ThemeHelper.Apply(this);
    }

    private void BuildLayout()
    {
        Text = _editingShowtime == null
            ? "Thêm suất chiếu mới"
            : "Cập nhật suất chiếu";

        ClientSize = new Size(760, 560);
        MinimumSize = new Size(680, 570);
        AutoScaleMode = AutoScaleMode.Dpi;

        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = false;

        BackColor = ThemeHelper.Background;

        var header = new PageHeader(
            _editingShowtime == null ? "Tạo suất chiếu" : "Cập nhật suất chiếu",
            "Chọn phim, phòng và thời gian để xem trước thời lượng cùng khoảng vệ sinh.");

        var content = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24, 15, 24, 10),
            BackColor = ThemeHelper.Background
        };

        AddLabel(content, "Phim *", 0, 8);

        cboMovie = new ComboBox
        {
            Location = new Point(0, 33),
            Size = new Size(620, 32),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        lblMovieInfo = new Label
        {
            Location = new Point(2, 68),
            Size = new Size(620, 22),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            ForeColor = ThemeHelper.Muted,
            Font = ThemeHelper.SmallFont
        };

        AddLabel(content, "Phòng chiếu *", 0, 100);

        cboRoom = new ComboBox
        {
            Location = new Point(0, 125),
            Size = new Size(300, 32),
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        lblRoomInfo = new Label
        {
            Location = new Point(315, 130),
            Size = new Size(305, 25),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            ForeColor = ThemeHelper.Muted,
            Font = ThemeHelper.SmallFont
        };

        AddLabel(content, "Ngày chiếu *", 0, 175);

        dtpDate = new DateTimePicker
        {
            Location = new Point(0, 200),
            Size = new Size(190, 32),
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd/MM/yyyy",
            MinDate = DateTime.Today,
            Value = DateTime.Today
        };

        AddLabel(content, "Giờ bắt đầu *", 230, 175);

        dtpTime = new DateTimePicker
        {
            Location = new Point(230, 200),
            Size = new Size(140, 32),
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "HH:mm",
            ShowUpDown = true,
            Value = DateTime.Today.AddHours(
                Math.Min(DateTime.Now.Hour + 1, 23))
        };

        AddLabel(content, "Giá vé cơ bản (VNĐ) *", 410, 175);

        numBasePrice = new NumericUpDown
        {
            Location = new Point(410, 200),
            Size = new Size(210, 32),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Minimum = 0,
            Maximum = 1000000,
            Increment = 5000,
            ThousandsSeparator = true,
            Value = 75000
        };

        var previewPanel = new Panel
        {
            Location = new Point(0, 260),
            Size = new Size(620, 85),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = ThemeHelper.AccentSoft,
            Padding = new Padding(12)
        };

        previewPanel.Controls.Add(new Label
        {
            Text = "THÔNG TIN THỜI GIAN DỰ KIẾN",
            Location = new Point(10, 8),
            AutoSize = true,
            Font = ThemeHelper.SectionFont,
            ForeColor = ThemeHelper.Primary
        });

        lblEndTime = new Label
        {
            Location = new Point(10, 38),
            Size = new Size(585, 30),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Font = ThemeHelper.BodyFont,
            ForeColor = ThemeHelper.Text
        };

        previewPanel.Controls.Add(lblEndTime);

        var note = new Label
        {
            Text = $"Hệ thống tự động cộng thêm {_settings?.GetCleaningMinutes() ?? CinemaManagement.Core.Constants.AppConstants.DefaultCleaningMinutes} phút vệ sinh phòng sau mỗi suất chiếu.",
            Location = new Point(2, 360),
            Size = new Size(620, 25),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Font = ThemeHelper.SmallFont,
            ForeColor = ThemeHelper.Muted
        };

        var footer = new Panel
        {
            Location = new Point(0, ClientSize.Height - 65),
            Size = new Size(ClientSize.Width, 65),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            Height = 65,
            Padding = new Padding(24, 12, 24, 12),
            BackColor = ThemeHelper.Surface,
            BorderStyle = BorderStyle.FixedSingle
        };

        var saveButtonWidth = _editingShowtime == null ? 135 : 155;
        btnSave = new Button
        {
            Text = _editingShowtime == null
                ? "Xác nhận thêm"
                : "Xác nhận cập nhật",
            Location = new Point(515 - saveButtonWidth, 12),
            Size = new Size(saveButtonWidth, 36),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            BackColor = ThemeHelper.Accent,
            ForeColor = ThemeHelper.Primary,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            AccessibleName = _editingShowtime == null
                ? "Xác nhận thêm suất chiếu"
                : "Xác nhận cập nhật suất chiếu",
            AccessibleDescription = "Kiểm tra dữ liệu và lưu suất chiếu."
        };

        var btnCancel = new Button
        {
            Text = "Hủy",
            Location = new Point(525, 12),
            Size = new Size(95, 36),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            BackColor = Color.FromArgb(226, 232, 240),
            ForeColor = ThemeHelper.Text,
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

        cboMovie.SelectedIndexChanged += (_, _) => UpdatePreview();
        cboRoom.SelectedIndexChanged += (_, _) => UpdatePreview();
        dtpDate.ValueChanged += (_, _) => UpdatePreview();
        dtpTime.ValueChanged += (_, _) => UpdatePreview();

        content.Controls.AddRange(new Control[]
        {
            cboMovie,
            lblMovieInfo,
            cboRoom,
            lblRoomInfo,
            dtpDate,
            dtpTime,
            numBasePrice,
            previewPanel,
            note
            
            
        });

        footer.Controls.AddRange(new Control[]
        {
            btnSave,
            btnCancel
        });

        var formLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = ThemeHelper.Background,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        formLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        formLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        header.Dock = DockStyle.Fill;
        content.Dock = DockStyle.Fill;
        formLayout.Controls.Add(header, 0, 0);
        formLayout.Controls.Add(content, 0, 1);
        Controls.Add(formLayout);
        Controls.Add(footer);
        footer.BringToFront();

        AcceptButton = btnSave;
        CancelButton = btnCancel;
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
            Font = ThemeHelper.SectionFont,
            ForeColor = ThemeHelper.Text
        });
    }

    private void LoadSources()
    {
        try
        {
            var movies = _movieService
                .GetAllMovies()
                .Where(movie => movie.Status != MovieStatus.Ended)
                .OrderBy(movie => movie.Title)
                .ToList();

            cboMovie.DataSource = movies;
            cboMovie.DisplayMember = nameof(Movie.Title);
            cboMovie.ValueMember = nameof(Movie.Id);

            var rooms = _roomService
                .GetAllRooms()
                .Where(room => room.IsActive)
                .OrderBy(room => room.Name)
                .ToList();

            cboRoom.DataSource = rooms;
            cboRoom.DisplayMember = nameof(Room.Name);
            cboRoom.ValueMember = nameof(Room.Id);

            if (_editingShowtime != null)
            {
                cboMovie.SelectedValue = _editingShowtime.MovieId;
                cboRoom.SelectedValue = _editingShowtime.RoomId;

                var startTime = _editingShowtime.StartTime;

                if (startTime.Date >= dtpDate.MinDate.Date &&
                    startTime.Date <= dtpDate.MaxDate.Date)
                {
                    dtpDate.Value = startTime.Date;
                    dtpTime.Value = DateTime.Today.Add(
                        startTime.TimeOfDay);
                }

                numBasePrice.Value = Math.Max(
                    numBasePrice.Minimum,
                    Math.Min(
                        numBasePrice.Maximum,
                        _editingShowtime.BasePrice));
            }

            btnSave.Enabled = movies.Count > 0 && rooms.Count > 0;

            if (movies.Count == 0)
            {
                MessageHelper.Warning(
                    "Chưa có phim để tạo suất chiếu.");
            }
            else if (rooms.Count == 0)
            {
                MessageHelper.Warning(
                    "Không có phòng chiếu nào đang hoạt động.");
            }

            UpdatePreview();
        }
        catch (Exception ex)
        {
            btnSave.Enabled = false;
            MessageHelper.Handle(ex);
        }
    }

    private void UpdatePreview()
    {
        if (cboMovie.SelectedItem is not Movie movie)
        {
            lblMovieInfo.Text = "Chưa chọn phim.";
            lblEndTime.Text = "Chưa có thông tin thời gian.";
            return;
        }

        lblMovieInfo.Text =
            $"Thời lượng: {movie.DurationMinutes} phút";

        if (cboRoom.SelectedItem is Room room)
        {
            lblRoomInfo.Text =
                $"{room.RoomType} · {room.TotalRows * room.TotalCols} ghế";
        }
        else
        {
            lblRoomInfo.Text = "Chưa chọn phòng.";
        }

        if (movie.DurationMinutes <= 0)
        {
            lblEndTime.Text = "Thời lượng phim không hợp lệ.";
            return;
        }

        var startTime = dtpDate.Value.Date.Add(
            dtpTime.Value.TimeOfDay);

        var endTime = startTime.AddMinutes(
            movie.DurationMinutes + (_settings?.GetCleaningMinutes() ?? CinemaManagement.Core.Constants.AppConstants.DefaultCleaningMinutes));

        lblEndTime.Text =
            $"Dự kiến: {startTime:dd/MM/yyyy HH:mm} – " +
            $"{endTime:dd/MM/yyyy HH:mm}";
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        try
        {
            if (cboMovie.SelectedItem is not Movie movie)
            {
                MessageHelper.Warning("Vui lòng chọn phim.");
                cboMovie.Focus();
                return;
            }

            if (cboRoom.SelectedItem is not Room room)
            {
                MessageHelper.Warning("Vui lòng chọn phòng chiếu.");
                cboRoom.Focus();
                return;
            }

            if (!room.IsActive)
            {
                MessageHelper.Warning(
                    "Phòng chiếu hiện không hoạt động.");

                return;
            }

            if (movie.DurationMinutes <= 0)
            {
                MessageHelper.Warning(
                    "Thời lượng phim không hợp lệ.");

                return;
            }

            var startTime = dtpDate.Value.Date.Add(
                dtpTime.Value.TimeOfDay);

            if (startTime <= DateTime.Now)
            {
                MessageHelper.Warning(
                    "Thời gian bắt đầu suất chiếu phải ở tương lai.");

                return;
            }

            var showtime = _editingShowtime ?? new Showtime();

            showtime.MovieId = movie.Id;
            showtime.RoomId = room.Id;
            showtime.StartTime = startTime;
            showtime.BasePrice = numBasePrice.Value;

            if (_editingShowtime == null)
            {
                showtime.IsActive = true;
                _showtimeService.AddShowtime(showtime);
            }
            else
            {
                _showtimeService.UpdateShowtime(showtime);
            }

            MessageHelper.Info(
                _editingShowtime == null
                    ? "Thêm suất chiếu thành công."
                    : "Cập nhật suất chiếu thành công.");

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);
        }
    }
}
