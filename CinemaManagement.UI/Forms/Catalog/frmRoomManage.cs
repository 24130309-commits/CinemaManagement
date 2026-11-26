using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.UI.Controls;
using CinemaManagement.UI.Forms.Rooms;
using CinemaManagement.UI.Helpers;

namespace CinemaManagement.UI.Forms.Catalog;

public partial class frmRoomManage : Form
{
    private readonly IRoomService _roomService;
    private List<Room> _allRooms = new();

    private TextBox txtName = null!;
    private TextBox txtSearch = null!;
    private NumericUpDown numRows = null!;
    private NumericUpDown numCols = null!;
    private ComboBox cboScreenType = null!;
    private ComboBox cboStatusFilter = null!;
    private CheckBox chkActive = null!;
    private EmptyStateDataGridView dgvRooms = null!;
    private Label lblTotal = null!;
    private Label _roomSummaryTitle = null!;
    private Label _roomSummaryDetail = null!;
    private Label _roomSummaryCapacity = null!;
    private Label _roomSummaryStatus = null!;
    private Panel _layoutPreview = null!;

    private int _selectedId;
    private Room? _selectedRoom;
    private Button? _updateButton;
    private Button? _deleteButton;
    private Button? _seatMapButton;

    public frmRoomManage(IRoomService roomService)
    {
        _roomService = roomService
            ?? throw new ArgumentNullException(nameof(roomService));

        InitializeComponent();
        BuildLayout();
        LoadRooms();

        ThemeHelper.Apply(this);
        ThemeHelper.ApplyDefaultGridStyle(dgvRooms);
    }

    private void BuildLayout()
    {
        Text = "Quản lý phòng chiếu";
        Size = new Size(1100, 700);
        MinimumSize = new Size(900, 600);
        StartPosition = FormStartPosition.CenterParent;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(18),
            BackColor = ThemeHelper.Background
        };

        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var header = new PageHeader(
            "Phòng chiếu",
            "Quản lý màn hình, sức chứa và sơ đồ của từng phòng.");

        var editor = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeHelper.Surface,
            Padding = new Padding(14),
            AutoScroll = true
        };

        txtName = new TextBox
        {
            Dock = DockStyle.Bottom,
            Height = 32,
            MaxLength = 100,
            AccessibleName = "Tên phòng"
        };

        cboScreenType = new ComboBox
        {
            Dock = DockStyle.Bottom,
            DropDownStyle = ComboBoxStyle.DropDownList,
            FormattingEnabled = true
        };

        cboScreenType.DataSource = Enum.GetValues<ScreenType>();
        cboScreenType.Format += (_, e) =>
        {
            if (e.ListItem is ScreenType type)
                e.Value = GetScreenTypeText(type);
        };

        numRows = new NumericUpDown
        {
            Dock = DockStyle.Bottom,
            Minimum = 1,
            Maximum = 26,
            Value = 10,
            Width = 95
        };

        numCols = new NumericUpDown
        {
            Dock = DockStyle.Bottom,
            Minimum = 1,
            Maximum = 40,
            Value = 12,
            Width = 95
        };

        chkActive = new CheckBox
        {
            Text = "Đang hoạt động",
            Dock = DockStyle.Bottom,
            Height = 32,
            Checked = true,
            AccessibleName = "Trạng thái hoạt động của phòng"
        };

        var fields = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 150,
            AutoScroll = true,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = ThemeHelper.Surface
        };

        fields.Controls.Add(CreateField("Tên phòng", txtName, 160));
        fields.Controls.Add(CreateField("Loại màn hình", cboScreenType, 115));
        fields.Controls.Add(CreateField("Số hàng ghế", numRows, 75));
        fields.Controls.Add(CreateField("Số cột ghế", numCols, 75));
        fields.Controls.Add(CreateField("Trạng thái", chkActive, 125));

        var btnAdd = CreateButton(
            "Thêm phòng",
            ThemeHelper.Accent,
            0,
            0,
            110);

        _updateButton = CreateButton(
            "Cập nhật",
            Color.FromArgb(71, 85, 105),
            0,
            0,
            100);

        _deleteButton = CreateButton(
            "Xóa phòng",
            ThemeHelper.Danger,
            0,
            0,
            100);

        _seatMapButton = CreateButton(
            "Sơ đồ ghế",
            ThemeHelper.Primary,
            0,
            0,
            110);
        _seatMapButton.AccessibleName = "Thiết kế sơ đồ ghế";
        _seatMapButton.AccessibleDescription = "Mở trình thiết kế sơ đồ ghế của phòng đã chọn.";

        var btnReset = CreateButton(
            "Làm mới",
            Color.FromArgb(100, 116, 139),
            0,
            0,
            100);

        btnAdd.Click += (_, _) => AddRoom();
        _updateButton.Enabled = false;
        _deleteButton.Enabled = false;
        _seatMapButton.Enabled = false;
        _updateButton.Click += (_, _) => UpdateRoom();
        _deleteButton.Click += (_, _) => DeleteRoom();
        _seatMapButton.Click += (_, _) => OpenSeatDesigner();
        btnReset.Click += (_, _) => ResetForm();

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            Height = 78,
            AutoScroll = true,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = ThemeHelper.Surface,
            Padding = new Padding(0, 8, 0, 0)
        };

        actions.Controls.AddRange(new Control[]
        {
            btnAdd,
            _updateButton,
            _deleteButton,
            _seatMapButton,
            btnReset
        });

        foreach (Control button in actions.Controls)
            button.Margin = new Padding(3, 4, 5, 3);

        var summary = BuildRoomSummary();
        var editorLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = ThemeHelper.Surface,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        editorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        editorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));
        editorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
        editorLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        editorLayout.Controls.Add(fields, 0, 0);
        editorLayout.Controls.Add(actions, 0, 1);
        editorLayout.Controls.Add(summary, 0, 2);
        editor.Controls.Add(editorLayout);

        txtName.TextChanged += (_, _) => UpdateRoomSummary();
        cboScreenType.SelectedIndexChanged += (_, _) => UpdateRoomSummary();
        numRows.ValueChanged += (_, _) => UpdateRoomSummary();
        numCols.ValueChanged += (_, _) => UpdateRoomSummary();
        chkActive.CheckedChanged += (_, _) => UpdateRoomSummary();

        var filterPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeHelper.Background
        };

        txtSearch = new TextBox
        {
            Width = 260,
            Height = 34,
            Margin = new Padding(0, 8, 10, 4),
            PlaceholderText = "Tìm theo tên phòng...",
            AccessibleName = "Tìm phòng theo tên"
        };

        cboStatusFilter = new ComboBox
        {
            Width = 175,
            Height = 34,
            Margin = new Padding(0, 8, 12, 4),
            DropDownStyle = ComboBoxStyle.DropDownList,
            AccessibleName = "Lọc theo trạng thái phòng"
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
            AutoSize = true,
            Margin = new Padding(4, 14, 4, 4),
            Font = ThemeHelper.SectionFont,
            ForeColor = ThemeHelper.Primary,
            AccessibleName = "Số phòng trong danh sách"
        };

        txtSearch.TextChanged += (_, _) => ApplyFilter();
        cboStatusFilter.SelectedIndexChanged += (_, _) => ApplyFilter();

        var filterToolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoScroll = true,
            Padding = Padding.Empty,
            Margin = Padding.Empty,
            BackColor = ThemeHelper.Background
        };
        filterToolbar.Controls.AddRange(new Control[]
            { txtSearch, cboStatusFilter, lblTotal });
        filterPanel.Controls.Add(filterToolbar);

        dgvRooms = new EmptyStateDataGridView
        {
            EmptyStateText = "Chưa có phòng chiếu. Tạo phòng mới để bắt đầu cấu hình lịch chiếu.",
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false,
            ColumnHeadersHeight = 52,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersDefaultCellStyle =
            {
                WrapMode = DataGridViewTriState.True,
                Alignment = DataGridViewContentAlignment.MiddleLeft
            },
            RowTemplate = { Height = 42 }
        };

        dgvRooms.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Mã",
            DataPropertyName = nameof(Room.Id),
            FillWeight = 10
        });

        dgvRooms.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Tên phòng",
            DataPropertyName = nameof(Room.Name),
            FillWeight = 28
        });

        dgvRooms.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Màn hình",
            DataPropertyName = nameof(Room.RoomType),
            FillWeight = 17
        });

        dgvRooms.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Hàng",
            DataPropertyName = nameof(Room.TotalRows),
            FillWeight = 12
        });

        dgvRooms.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Cột",
            DataPropertyName = nameof(Room.TotalCols),
            FillWeight = 12
        });

        dgvRooms.Columns.Add(new DataGridViewCheckBoxColumn
        {
            HeaderText = "Hoạt động",
            DataPropertyName = nameof(Room.IsActive),
            FillWeight = 21
        });

        dgvRooms.CellClick += DgvRooms_CellClick;

        var catalogPane = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = ThemeHelper.Background,
            Margin = new Padding(0, 0, 8, 0),
            Padding = Padding.Empty
        };
        catalogPane.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        catalogPane.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
        catalogPane.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        catalogPane.Controls.Add(filterPanel, 0, 0);
        catalogPane.Controls.Add(dgvRooms, 0, 1);

        root.Controls.Add(header, 0, 0);
        root.SetColumnSpan(header, 2);
        root.Controls.Add(catalogPane, 0, 1);
        root.Controls.Add(editor, 1, 1);

        void ApplyResponsiveLayout()
        {
            var stacked = root.ClientSize.Width < 1080;
            root.SuspendLayout();
            editorLayout.SuspendLayout();
            root.ColumnStyles.Clear();
            root.RowStyles.Clear();

            if (stacked)
            {
                root.ColumnCount = 1;
                root.RowCount = 3;
                root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, 180));
                root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                root.SetColumnSpan(header, 1);
                root.SetCellPosition(editor, new TableLayoutPanelCellPosition(0, 1));
                root.SetCellPosition(catalogPane, new TableLayoutPanelCellPosition(0, 2));
                editorLayout.RowStyles.Clear();
                editorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));
                editorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
                editorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 0));
                summary.Visible = false;
            }
            else
            {
                root.ColumnCount = 2;
                root.RowCount = 2;
                root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
                root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
                root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                root.SetColumnSpan(header, 2);
                root.SetCellPosition(catalogPane, new TableLayoutPanelCellPosition(0, 1));
                root.SetCellPosition(editor, new TableLayoutPanelCellPosition(1, 1));
                editorLayout.RowStyles.Clear();
                editorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));
                editorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
                editorLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                summary.Visible = true;
            }

            editorLayout.ResumeLayout(true);
            root.ResumeLayout(true);
        }

        root.Resize += (_, _) => ApplyResponsiveLayout();
        ApplyResponsiveLayout();

        Controls.Add(root);
        UpdateRoomSummary();
    }

    private Panel BuildRoomSummary()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(2, 8, 2, 2),
            Padding = new Padding(16, 12, 14, 12),
            BackColor = ThemeHelper.SurfaceAlt,
            AccessibleName = "Tóm tắt cấu hình phòng"
        };
        var accent = new Panel
        {
            Dock = DockStyle.Left,
            Width = 4,
            BackColor = ThemeHelper.Accent
        };
        _roomSummaryTitle = new Label
        {
            Dock = DockStyle.Top,
            Height = 28,
            Font = ThemeHelper.SectionFont,
            ForeColor = ThemeHelper.Primary,
            AutoEllipsis = true
        };
        _roomSummaryDetail = new Label
        {
            Dock = DockStyle.Top,
            Height = 24,
            Font = ThemeHelper.BodyFont,
            ForeColor = ThemeHelper.Text,
            AutoEllipsis = true
        };
        _roomSummaryCapacity = new Label
        {
            Dock = DockStyle.Top,
            Height = 24,
            Font = ThemeHelper.SmallFont,
            ForeColor = ThemeHelper.Muted,
            AutoEllipsis = true
        };
        _roomSummaryStatus = new Label
        {
            Dock = DockStyle.Top,
            Height = 24,
            Font = ThemeHelper.SmallFont,
            AutoEllipsis = true
        };
        _layoutPreview = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(2, 10, 2, 2),
            BackColor = ThemeHelper.Surface,
            AccessibleRole = AccessibleRole.Graphic,
            AccessibleName = "Minh họa lưới theo số hàng và cột; không hiển thị loại hoặc trạng thái ghế"
        };
        _layoutPreview.Paint += PaintLayoutPreview;
        panel.Controls.Add(_layoutPreview);
        panel.Controls.Add(_roomSummaryStatus);
        panel.Controls.Add(_roomSummaryCapacity);
        panel.Controls.Add(_roomSummaryDetail);
        panel.Controls.Add(_roomSummaryTitle);
        panel.Controls.Add(accent);
        return panel;
    }

    private void UpdateRoomSummary()
    {
        if (_roomSummaryTitle == null)
            return;

        var roomName = string.IsNullOrWhiteSpace(txtName.Text)
            ? "Phòng mới"
            : txtName.Text.Trim();
        var screenType = cboScreenType.SelectedItem is ScreenType type
            ? GetScreenTypeText(type)
            : "Chưa chọn màn hình";
        var rows = (int)numRows.Value;
        var columns = (int)numCols.Value;

        _roomSummaryTitle.Text = _selectedId > 0
            ? $"Cấu hình · {roomName}"
            : "Xem trước cấu hình";
        _roomSummaryDetail.Text = $"{screenType}  ·  {rows} hàng × {columns} cột";
        _roomSummaryCapacity.Text = $"Sức chứa theo lưới: {rows * columns} ghế";
        _roomSummaryStatus.Text = chkActive.Checked
            ? "●  Đang hoạt động"
            : "●  Ngừng hoạt động";
        _roomSummaryStatus.ForeColor = chkActive.Checked
            ? ThemeHelper.Success
            : ThemeHelper.Muted;
        _layoutPreview?.Invalidate();
    }

    private void PaintLayoutPreview(object? sender, PaintEventArgs e)
    {
        if (_layoutPreview.ClientSize.Width < 20 || _layoutPreview.ClientSize.Height < 20)
            return;

        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var width = _layoutPreview.ClientSize.Width;
        var height = _layoutPreview.ClientSize.Height;
        var columns = Math.Max(1, (int)numCols.Value);
        var rows = Math.Max(1, (int)numRows.Value);

        using var headingFont = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        using var noteFont = new Font("Segoe UI", 7.5F);
        using var textBrush = new SolidBrush(ThemeHelper.Muted);
        using var accentBrush = new SolidBrush(ThemeHelper.Primary);
        using var seatBrush = new SolidBrush(Color.FromArgb(86, 121, 158));
        using var screenPen = new Pen(ThemeHelper.Accent, 2F);

        graphics.DrawString("Lưới ghế tham khảo", headingFont, accentBrush, 8, 4);
        graphics.DrawString("Minh họa số hàng và cột · chưa thể hiện loại ghế", noteFont, textBrush, 8, 24);

        var top = 54F;
        var side = 12F;
        var usableWidth = Math.Max(1F, width - side * 2);
        var usableHeight = Math.Max(1F, height - top - 12F);
        var cellWidth = usableWidth / columns;
        var cellHeight = usableHeight / rows;
        var seatWidth = Math.Clamp(cellWidth * 0.64F, 3F, 13F);
        var seatHeight = Math.Clamp(cellHeight * 0.58F, 3F, 9F);
        var gridWidth = cellWidth * columns;
        var startX = (width - gridWidth) / 2F;

        graphics.DrawLine(screenPen, width * 0.28F, top - 8, width * 0.72F, top - 8);

        for (var row = 0; row < rows; row++)
        {
            for (var column = 0; column < columns; column++)
            {
                var x = startX + column * cellWidth + (cellWidth - seatWidth) / 2F;
                var y = top + row * cellHeight + (cellHeight - seatHeight) / 2F;
                graphics.FillEllipse(
                    seatBrush,
                    new RectangleF(x, y, seatWidth, seatHeight));
            }
        }
    }

    private static Panel CreateField(
        string caption,
        Control input,
        int width)
    {
        var field = new Panel
        {
            Width = width,
            Height = 68,
            Margin = new Padding(3, 2, 8, 3),
            BackColor = ThemeHelper.Surface
        };

        var label = new Label
        {
            Text = caption,
            Dock = DockStyle.Top,
            Height = 25,
            Font = ThemeHelper.SectionFont,
            ForeColor = ThemeHelper.Text
        };

        field.Controls.Add(input);
        field.Controls.Add(label);
        return field;
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
            Size = new Size(width, 34),
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

    private static string GetScreenTypeText(ScreenType type)
    {
        return type switch
        {
            ScreenType.TwoD => "2D",
            ScreenType.ThreeD => "3D",
            ScreenType.IMAX => "IMAX",
            _ => type.ToString()
        };
    }

    private Room CreateRoom()
    {
        if (string.IsNullOrWhiteSpace(txtName.Text))
            throw new InvalidOperationException("Vui lòng nhập tên phòng chiếu.");

        if (cboScreenType.SelectedItem is not ScreenType screenType)
            throw new InvalidOperationException("Vui lòng chọn loại màn hình.");

        return new Room
        {
            Name = txtName.Text.Trim(),
            ScreenType = screenType,
            TotalRows = (int)numRows.Value,
            TotalCols = (int)numCols.Value,
            IsActive = chkActive.Checked
        };
    }

    private void AddRoom()
    {
        try
        {
            var room = CreateRoom();
            _roomService.AddRoom(room);

            MessageHelper.Info("Thêm phòng chiếu thành công.");

            ResetForm();
            LoadRooms();
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);
        }
    }

    private void UpdateRoom()
    {
        try
        {
            if (_selectedId <= 0)
            {
                MessageHelper.Warning("Vui lòng chọn phòng cần cập nhật.");
                return;
            }

            var room = CreateRoom();
            room.Id = _selectedId;

            _roomService.UpdateRoom(room);

            MessageHelper.Info("Cập nhật phòng chiếu thành công.");

            ResetForm();
            LoadRooms();
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);
        }
    }

    private void DeleteRoom()
    {
        if (_selectedId <= 0)
        {
            MessageHelper.Warning("Vui lòng chọn phòng cần xóa.");
            return;
        }

        if (!MessageHelper.Confirm(
                $"Bạn có chắc chắn muốn xóa phòng \"{txtName.Text}\"?"))
        {
            return;
        }

        try
        {
            _roomService.DeleteRoom(_selectedId);

            MessageHelper.Info("Xóa phòng chiếu thành công.");

            ResetForm();
            LoadRooms();
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);
        }
    }

    private void OpenSeatDesigner()
    {
        if (_selectedId <= 0)
        {
            MessageHelper.Warning(
                "Vui lòng chọn phòng trước khi thiết kế sơ đồ ghế.");

            return;
        }

        using var form = new frmSeatDesigner(
            _roomService,
            _selectedId);

        if (form.ShowDialog(this) == DialogResult.OK)
            LoadRooms();
    }

    private void DgvRooms_CellClick(
        object? sender,
        DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 ||
            dgvRooms.Rows[e.RowIndex].DataBoundItem is not Room room)
        {
            return;
        }

        _selectedId = room.Id;
        _selectedRoom = room;
        _updateButton!.Enabled = true;
        _deleteButton!.Enabled = true;
        _seatMapButton!.Enabled = true;

        txtName.Text = room.Name;

        cboScreenType.SelectedItem = room.ScreenType;

        numRows.Value = Math.Clamp(
            room.TotalRows,
            (int)numRows.Minimum,
            (int)numRows.Maximum);

        numCols.Value = Math.Clamp(
            room.TotalCols,
            (int)numCols.Minimum,
            (int)numCols.Maximum);

        chkActive.Checked = room.IsActive;
    }

    private void LoadRooms()
    {
        try
        {
            _allRooms = _roomService.GetAllRooms()
                .OrderBy(room => room.Name)
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
        var keyword = txtSearch.Text.Trim();

        IEnumerable<Room> rooms = _allRooms;

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            rooms = rooms.Where(room =>
                room.Name.Contains(
                    keyword,
                    StringComparison.OrdinalIgnoreCase));
        }

        if (cboStatusFilter.SelectedIndex == 1)
            rooms = rooms.Where(room => room.IsActive);
        else if (cboStatusFilter.SelectedIndex == 2)
            rooms = rooms.Where(room => !room.IsActive);

        var result = rooms.ToList();

        dgvRooms.DataSource = null;
        dgvRooms.DataSource = result;

        lblTotal.Text = $"Số phòng: {result.Count}";
    }

    private void ResetForm()
    {
        _selectedId = 0;
        _selectedRoom = null;
        _updateButton!.Enabled = false;
        _deleteButton!.Enabled = false;
        _seatMapButton!.Enabled = false;

        txtName.Clear();
        cboScreenType.SelectedItem = ScreenType.TwoD;
        numRows.Value = 10;
        numCols.Value = 12;
        chkActive.Checked = true;

        dgvRooms.ClearSelection();
        txtName.Focus();
    }
}
