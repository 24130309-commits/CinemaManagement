using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.UI.Controls;
using CinemaManagement.UI.Helpers;

namespace CinemaManagement.UI.Forms.Catalog;

public partial class frmShowtimeManage : Form
{
    private readonly IShowtimesService _showtimeService;
    private readonly IMovieService _movieService;
    private readonly IRoomService _roomService;

    private List<Showtime> _allShowtimes = new();

    private TextBox txtSearch = null!;
    private DateTimePicker dtpFilterDate = null!;
    private CheckBox chkFilterDate = null!;
    private ComboBox cboStatusFilter = null!;
    private EmptyStateDataGridView dgvShowtimes = null!;
    private Label lblTotal = null!;

    public frmShowtimeManage(
        IShowtimesService showtimeService,
        IMovieService movieService,
        IRoomService roomService)
    {
        _showtimeService = showtimeService
            ?? throw new ArgumentNullException(nameof(showtimeService));

        _movieService = movieService
            ?? throw new ArgumentNullException(nameof(movieService));

        _roomService = roomService
            ?? throw new ArgumentNullException(nameof(roomService));

        InitializeComponent();
        BuildLayout();
        LoadShowtimes();

        ThemeHelper.Apply(this);
        ThemeHelper.ApplyDefaultGridStyle(dgvShowtimes);
    }

    private void BuildLayout()
    {
        Text = "Quản lý suất chiếu";
        Size = new Size(1150, 700);
        MinimumSize = new Size(950, 600);
        StartPosition = FormStartPosition.CenterParent;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(18),
            BackColor = ThemeHelper.Background
        };

        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var header = new PageHeader(
            "Lịch chiếu",
            "Theo dõi lịch, phòng chiếu và tình trạng từng suất.");

        var actionPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeHelper.Surface,
            Padding = new Padding(8, 10, 8, 6),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoScroll = true
        };

        var btnAdd = CreateButton(
            "Thêm suất chiếu",
            ThemeHelper.Accent,
            12,
            12,
            125);

        var btnEdit = CreateButton(
            "Chỉnh sửa",
            Color.FromArgb(71, 85, 105),
            147,
            12,
            105);

        var btnDelete = CreateButton(
            "Xóa suất",
            ThemeHelper.Danger,
            260,
            12,
            95);

        var btnRefresh = CreateButton(
            "Làm mới",
            Color.FromArgb(100, 116, 139),
            363,
            12,
            100);

        var btnShowAll = CreateButton(
            "Hiển thị tất cả",
            ThemeHelper.Primary,
            473,
            12,
            125);

        btnAdd.Click += (_, _) => AddShowtime();
        btnEdit.Click += (_, _) => EditShowtime();
        btnDelete.Click += (_, _) => DeleteShowtime();
        btnRefresh.Click += (_, _) => LoadShowtimes();
        btnShowAll.Click += (_, _) =>
        {
            chkFilterDate.Checked = false;
            cboStatusFilter.SelectedIndex = 0;
            txtSearch.Clear();
            ApplyFilter();
        };

        actionPanel.Controls.AddRange(new Control[]
        {
            btnAdd,
            btnEdit,
            btnDelete,
            btnRefresh,
            btnShowAll
        });

        var filterPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeHelper.Background,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoScroll = true,
            Padding = new Padding(2, 8, 2, 4)
        };

        txtSearch = new TextBox
        {
            Location = new Point(0, 10),
            Size = new Size(250, 30),
            PlaceholderText = "Tìm theo tên phim hoặc phòng..."
        };

        chkFilterDate = new CheckBox
        {
            Text = "Lọc ngày",
            Location = new Point(265, 15),
            AutoSize = true,
            Checked = true
        };

        dtpFilterDate = new DateTimePicker
        {
            Location = new Point(350, 10),
            Size = new Size(140, 30),
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd/MM/yyyy",
            Value = DateTime.Today
        };

        cboStatusFilter = new ComboBox
        {
            Location = new Point(505, 10),
            Size = new Size(165, 30),
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        cboStatusFilter.Items.AddRange(new object[]
        {
            "Tất cả trạng thái",
            "Đang hoạt động",
            "Ngừng hoạt động"
        });

        cboStatusFilter.SelectedIndex = 0;

        lblTotal = new Label
        {
            Location = new Point(685, 15),
            AutoSize = true,
            Font = ThemeHelper.SectionFont,
            ForeColor = ThemeHelper.Primary
        };

        txtSearch.TextChanged += (_, _) => ApplyFilter();

        chkFilterDate.CheckedChanged += (_, _) =>
        {
            dtpFilterDate.Enabled = chkFilterDate.Checked;
            ApplyFilter();
        };

        dtpFilterDate.ValueChanged += (_, _) => ApplyFilter();

        cboStatusFilter.SelectedIndexChanged += (_, _) => ApplyFilter();

        filterPanel.Controls.AddRange(new Control[]
        {
            txtSearch,
            chkFilterDate,
            dtpFilterDate,
            cboStatusFilter,
            lblTotal
        });

        dgvShowtimes = new EmptyStateDataGridView
        {
            EmptyStateText = "Không có suất chiếu phù hợp với bộ lọc hiện tại.",
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false
        };

        dgvShowtimes.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Id",
            HeaderText = "MÃ SUẤT",
            DataPropertyName = nameof(Showtime.Id),
            FillWeight = 12
        });

        dgvShowtimes.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "MovieTitle",
            HeaderText = "PHIM",
            DataPropertyName = nameof(Showtime.MovieTitle),
            FillWeight = 28
        });

        dgvShowtimes.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "RoomName",
            HeaderText = "PHÒNG",
            DataPropertyName = nameof(Showtime.RoomName),
            FillWeight = 17
        });

        dgvShowtimes.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "StartTime",
            HeaderText = "BẮT ĐẦU",
            DataPropertyName = nameof(Showtime.StartTime),
            FillWeight = 19,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Format = "dd/MM/yyyy HH:mm"
            }
        });

        dgvShowtimes.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "EndTime",
            HeaderText = "KẾT THÚC",
            DataPropertyName = nameof(Showtime.EndTime),
            FillWeight = 19,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Format = "dd/MM/yyyy HH:mm"
            }
        });

        dgvShowtimes.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "BasePrice",
            HeaderText = "GIÁ VÉ",
            DataPropertyName = nameof(Showtime.BasePrice),
            FillWeight = 17,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Format = "N0"
            }
        });

        dgvShowtimes.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "IsActive",
            HeaderText = "TRẠNG THÁI",
            DataPropertyName = nameof(Showtime.IsActive),
            FillWeight = 17
        });

        dgvShowtimes.CellFormatting += DgvShowtimes_CellFormatting;

        dgvShowtimes.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0)
                EditShowtime();
        };

        root.Controls.Add(header, 0, 0);
        root.Controls.Add(actionPanel, 0, 1);
        root.Controls.Add(filterPanel, 0, 2);
        root.Controls.Add(dgvShowtimes, 0, 3);

        Controls.Add(root);
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
            Size = new Size(width, 36),
            BackColor = color,
            ForeColor = color == ThemeHelper.Accent
                ? ThemeHelper.Primary
                : Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        ThemeHelper.StyleActionButton(button, color);
        return button;
    }

    private void DgvShowtimes_CellFormatting(
        object? sender,
        DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0)
            return;

        if (dgvShowtimes.Columns[e.ColumnIndex].Name == "IsActive" &&
            e.Value is bool isActive)
        {
            e.Value = isActive
                ? "Đang hoạt động"
                : "Ngừng hoạt động";

            e.FormattingApplied = true;
        }
    }

    private void LoadShowtimes()
    {
        try
        {
            _allShowtimes = _showtimeService
                .GetAllShowtimes()
                .OrderByDescending(showtime => showtime.StartTime)
                .ToList();

            ApplyFilter();
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);
        }
    }

    private void ApplyFilter()
    {
        IEnumerable<Showtime> result = _allShowtimes;

        if (chkFilterDate.Checked)
        {
            var selectedDate = dtpFilterDate.Value.Date;

            result = result.Where(showtime =>
                showtime.StartTime.Date == selectedDate);
        }

        if (cboStatusFilter.SelectedIndex == 1)
        {
            result = result.Where(showtime => showtime.IsActive);
        }
        else if (cboStatusFilter.SelectedIndex == 2)
        {
            result = result.Where(showtime => !showtime.IsActive);
        }

        var keyword = txtSearch.Text.Trim();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            result = result.Where(showtime =>
                (showtime.MovieTitle ?? string.Empty).Contains(
                    keyword,
                    StringComparison.OrdinalIgnoreCase)
                ||
                (showtime.RoomName ?? string.Empty).Contains(
                    keyword,
                    StringComparison.OrdinalIgnoreCase));
        }

        var rows = result
            .OrderBy(showtime => showtime.StartTime)
            .ToList();

        dgvShowtimes.DataSource = null;
        dgvShowtimes.DataSource = rows;

        lblTotal.Text = $"Số suất chiếu: {rows.Count}";
    }

    private Showtime? GetSelectedShowtime()
    {
        return dgvShowtimes.CurrentRow?.DataBoundItem as Showtime;
    }

    private void AddShowtime()
    {
        try
        {
            using var form = new frmShowtimeEdit(
                _showtimeService,
                _movieService,
                _roomService);

            if (form.ShowDialog(this) == DialogResult.OK)
                LoadShowtimes();
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);
        }
    }

    private void EditShowtime()
    {
        var showtime = GetSelectedShowtime();

        if (showtime == null)
        {
            MessageHelper.Warning(
                "Vui lòng chọn suất chiếu cần chỉnh sửa.");

            return;
        }

        if (showtime.StartTime <= DateTime.Now)
        {
            MessageHelper.Warning(
                "Không thể chỉnh sửa suất chiếu đã bắt đầu hoặc đã qua.");

            return;
        }

        try
        {
            using var form = new frmShowtimeEdit(
                _showtimeService,
                _movieService,
                _roomService,
                showtime);

            if (form.ShowDialog(this) == DialogResult.OK)
                LoadShowtimes();
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);
        }
    }

    private void DeleteShowtime()
    {
        var showtime = GetSelectedShowtime();

        if (showtime == null)
        {
            MessageHelper.Warning(
                "Vui lòng chọn suất chiếu cần xóa.");

            return;
        }

        var confirm = MessageHelper.Confirm(
            $"Bạn có chắc chắn muốn xóa suất chiếu của phim " +
            $"\"{showtime.MovieTitle}\" tại phòng \"{showtime.RoomName}\"?");

        if (!confirm)
            return;

        try
        {
            _showtimeService.DeleteShowtime(showtime.Id);

            MessageHelper.Info("Xóa suất chiếu thành công.");

            LoadShowtimes();
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);
        }
    }
}
