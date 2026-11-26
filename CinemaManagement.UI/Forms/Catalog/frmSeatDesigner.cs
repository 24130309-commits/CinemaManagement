using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.UI.Controls;
using CinemaManagement.UI.Helpers;

namespace CinemaManagement.UI.Forms.Rooms;

public partial class frmSeatDesigner : Form
{
    private readonly IRoomService _rooms;
    private readonly ComboBox _room = new();
    private readonly NumericUpDown _rows = new();
    private readonly NumericUpDown _cols = new();
    private readonly FlowLayoutPanel _grid = new();
    private readonly Label _roomInfo = new();
    private readonly Label _seatSummary = new();
    private readonly Button _generateButton = new();
    private readonly Button _saveButton = new();
    private readonly Dictionary<int, Seat> _seatById = new();
    private readonly Dictionary<int, SeatType> _originalSeatTypes = new();

    private bool _isChangingRoom;
    private int _lastRoomId;
    private bool _hasUnsavedChanges;

    public frmSeatDesigner(IRoomService rooms)
    {
        _rooms = rooms ?? throw new ArgumentNullException(nameof(rooms));

        InitializeComponent();
        BuildLayout();
        LoadRooms();

        ThemeHelper.Apply(this);

        FormClosing += FrmSeatDesigner_FormClosing;
    }

    public frmSeatDesigner(IRoomService rooms, int roomId)
        : this(rooms)
    {
        if (roomId <= 0)
            throw new ArgumentOutOfRangeException(nameof(roomId));

        var selectedRoom = _room.Items
            .Cast<Room>()
            .FirstOrDefault(room => room.Id == roomId);

        if (selectedRoom == null)
            throw new InvalidOperationException("Không tìm thấy phòng chiếu.");

        _room.SelectedItem = selectedRoom;
    }

    private void BuildLayout()
    {
        Text = "Thiết kế sơ đồ ghế";
        Size = new Size(1120, 790);
        MinimumSize = new Size(760, 540);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = ThemeHelper.Background;
        Font = ThemeHelper.BodyFont;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            Padding = new Padding(18),
            BackColor = ThemeHelper.Background
        };

        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));

        root.Controls.Add(BuildHeader(), 0, 0);
        root.Controls.Add(BuildToolbar(), 0, 1);
        root.Controls.Add(BuildInfoPanel(), 0, 2);
        root.Controls.Add(BuildSeatArea(), 0, 3);
        root.Controls.Add(BuildLegend(), 0, 4);
        root.Controls.Add(BuildFooter(), 0, 5);

        Controls.Add(root);
        AcceptButton = _saveButton;

        ThemeHelper.FitToWorkingArea(this, new Size(1120, 790), new Size(760, 540));

        _room.SelectedIndexChanged += (_, _) => OnRoomChanged();
    }

    private Control BuildHeader()
    {
        return new PageHeader(
            "Thiết kế sơ đồ ghế",
            "Chọn phòng, tạo sơ đồ và phân loại từng vị trí ghế.");
    }

    private Control BuildToolbar()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeHelper.Surface,
            Padding = new Padding(12, 18, 12, 10),
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            AutoScroll = true
        };

        _room.DropDownStyle = ComboBoxStyle.DropDownList;
        _room.Width = 190;
        _room.Margin = new Padding(4, 2, 16, 0);

        ConfigureNumberInput(_rows, 26, 6, 72);
        ConfigureNumberInput(_cols, 40, 8, 72);

        panel.Controls.Add(CreateToolbarLabel("Phòng chiếu"));
        panel.Controls.Add(_room);
        panel.Controls.Add(CreateToolbarLabel("Số hàng"));
        panel.Controls.Add(_rows);
        panel.Controls.Add(CreateToolbarLabel("Số cột"));
        panel.Controls.Add(_cols);

        _generateButton.Text = "Tạo lại sơ đồ";
        StyleActionButton(_generateButton, ThemeHelper.Primary);
        _generateButton.Click += (_, _) => GenerateSeats();

        _saveButton.Text = "Lưu loại ghế";
        StyleActionButton(_saveButton, ThemeHelper.Accent);
        _saveButton.Enabled = false;
        _saveButton.Click += (_, _) => SaveSeatTypes();

        panel.Controls.Add(_generateButton);
        panel.Controls.Add(_saveButton);

        return panel;
    }

    private Control BuildInfoPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeHelper.Background,
            Padding = new Padding(4, 8, 4, 4)
        };

        _roomInfo.Dock = DockStyle.Left;
        _roomInfo.Width = 560;
        _roomInfo.TextAlign = ContentAlignment.MiddleLeft;
        _roomInfo.Font = ThemeHelper.SectionFont;
        _roomInfo.ForeColor = ThemeHelper.Primary;
        _roomInfo.AutoEllipsis = true;

        _seatSummary.Dock = DockStyle.Right;
        _seatSummary.Width = 440;
        _seatSummary.TextAlign = ContentAlignment.MiddleRight;
        _seatSummary.Font = ThemeHelper.SmallFont;
        _seatSummary.ForeColor = ThemeHelper.Muted;
        _seatSummary.AutoEllipsis = true;

        panel.Resize += (_, _) =>
        {
            _roomInfo.Width = (int)(panel.ClientSize.Width * 0.55);
            _seatSummary.Width = panel.ClientSize.Width - _roomInfo.Width;
        };

        panel.Controls.Add(_seatSummary);
        panel.Controls.Add(_roomInfo);

        return panel;
    }

    private Control BuildSeatArea()
    {
        var outer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeHelper.Surface,
            Padding = new Padding(18)
        };

        var screen = new Panel
        {
            Dock = DockStyle.Top,
            Height = 44,
            Padding = new Padding(90, 4, 90, 8),
            BackColor = ThemeHelper.Surface
        };

        screen.Controls.Add(new Label
        {
            Text = "MÀN HÌNH",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.FromArgb(226, 232, 240),
            ForeColor = ThemeHelper.Primary,
            Font = ThemeHelper.SectionFont
        });

        _grid.Dock = DockStyle.Fill;
        _grid.AutoScroll = true;
        _grid.FlowDirection = FlowDirection.TopDown;
        _grid.WrapContents = false;
        _grid.Padding = new Padding(12, 16, 12, 16);
        _grid.BackColor = ThemeHelper.Surface;
        _grid.Margin = Padding.Empty;

        outer.Controls.Add(_grid);
        outer.Controls.Add(screen);

        outer.Paint += (_, e) =>
        {
            using var pen = new Pen(ThemeHelper.Border);

            e.Graphics.DrawRectangle(
                pen,
                0,
                0,
                outer.Width - 1,
                outer.Height - 1);
        };

        return outer;
    }

    private Control BuildLegend()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeHelper.Background,
            Padding = new Padding(2, 10, 2, 4),
            WrapContents = false,
            AutoScroll = true
        };

        AddLegendItem(panel, GetSeatColor(SeatType.Normal), "Ghế thường");
        AddLegendItem(panel, GetSeatColor(SeatType.VIP), "Ghế VIP");
        AddLegendItem(panel, GetSeatColor(SeatType.Couple), "Ghế đôi");
        AddLegendItem(panel, GetSeatColor(SeatType.Aisle), "Lối đi");

        return panel;
    }

    private Control BuildFooter()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeHelper.Surface,
            Padding = new Padding(14, 12, 14, 10)
        };

        panel.Controls.Add(new Label
        {
            Text = "Nhấn vào ghế để chuyển lần lượt giữa các loại ghế. Nhớ lưu thay đổi trước khi đóng cửa sổ.",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = ThemeHelper.SmallFont,
            ForeColor = ThemeHelper.Muted
        });

        return panel;
    }

    private static void ConfigureNumberInput(
        NumericUpDown input,
        int maximum,
        int defaultValue,
        int width)
    {
        input.Minimum = 1;
        input.Maximum = maximum;
        input.Value = defaultValue;
        input.Width = width;
        input.Font = ThemeHelper.BodyFont;
        input.Margin = new Padding(4, 2, 16, 0);
        input.TextAlign = HorizontalAlignment.Center;
    }

    private static Label CreateToolbarLabel(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            Padding = new Padding(0, 7, 0, 0),
            Font = ThemeHelper.SmallFont,
            ForeColor = ThemeHelper.Text,
            Margin = new Padding(4, 2, 4, 0)
        };
    }

    private static void StyleActionButton(Button button, Color color)
    {
        button.AutoSize = true;
        button.Height = 34;
        ThemeHelper.StyleActionButton(button, color);
        button.Padding = new Padding(10, 2, 10, 2);
        button.Margin = new Padding(8, 0, 4, 0);
    }

    private static void AddLegendItem(
        FlowLayoutPanel panel,
        Color color,
        string text)
    {
        var item = new Panel
        {
            Width = 145,
            Height = 28,
            Margin = new Padding(4, 0, 10, 0)
        };

        item.Controls.Add(new Panel
        {
            Location = new Point(0, 5),
            Size = new Size(18, 18),
            BackColor = color
        });

        item.Controls.Add(new Label
        {
            Text = text,
            Location = new Point(26, 3),
            Size = new Size(115, 22),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = ThemeHelper.SmallFont,
            ForeColor = ThemeHelper.Text
        });

        panel.Controls.Add(item);
    }

    private int RoomId =>
        _room.SelectedItem is Room room ? room.Id : 0;

    private void LoadRooms()
    {
        try
        {
            var rooms = _rooms.GetAllRooms()
                .OrderBy(room => room.Name)
                .ToList();

            _room.DisplayMember = nameof(Room.Name);
            _room.ValueMember = nameof(Room.Id);
            _room.DataSource = rooms;

            var hasRooms = rooms.Count > 0;

            _room.Enabled = hasRooms;
            _rows.Enabled = hasRooms;
            _cols.Enabled = hasRooms;
            _generateButton.Enabled = hasRooms;
            _saveButton.Enabled = false;

            if (!hasRooms)
            {
                _roomInfo.Text = "Chưa có phòng chiếu.";
                _seatSummary.Text = "";
                _grid.Controls.Clear();
                return;
            }

            OnRoomChanged();
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);
        }
    }

    private void OnRoomChanged()
    {
        if (_isChangingRoom || _room.SelectedItem is not Room room)
            return;

        if (_hasUnsavedChanges &&
            _lastRoomId > 0 &&
            room.Id != _lastRoomId)
        {
            if (!MessageHelper.Confirm(
                    "Bạn đang có thay đổi loại ghế chưa lưu. Bỏ các thay đổi này và chuyển phòng?"))
            {
                _isChangingRoom = true;
                _room.SelectedValue = _lastRoomId;
                _isChangingRoom = false;
                return;
            }
        }

        _lastRoomId = room.Id;
        _hasUnsavedChanges = false;
        _saveButton.Enabled = false;

        _rows.Value = Math.Max(
            _rows.Minimum,
            Math.Min(_rows.Maximum, room.TotalRows));

        _cols.Value = Math.Max(
            _cols.Minimum,
            Math.Min(_cols.Maximum, room.TotalCols));

        _roomInfo.Text =
            $"{room.Name}  ·  {GetScreenTypeText(room.ScreenType)}  ·  " +
            $"{room.TotalRows} hàng × {room.TotalCols} cột";

        RenderSeats();
    }


    private void GenerateSeats()
    {
        if (RoomId <= 0)
        {
            MessageHelper.Warning("Vui lòng chọn phòng chiếu.");
            return;
        }

        if (!MessageHelper.Confirm(
                "Tạo lại sơ đồ sẽ thay thế toàn bộ ghế hiện tại và loại ghế đã phân loại. Bạn có chắc chắn muốn tiếp tục?"))
        {
            return;
        }

        try
        {
            if (_room.SelectedItem is not Room room)
            {
                MessageHelper.Warning("Vui lòng chọn phòng chiếu hợp lệ.");
                return;
            }

            var totalRows = (int)_rows.Value;
            var totalCols = (int)_cols.Value;

            // Repository thực hiện thay thế sơ đồ và cập nhật
            // kích thước phòng trong cùng một transaction.
            _rooms.GenerateSeats(
                room.Id,
                totalRows,
                totalCols);

            // Chỉ cập nhật đối tượng đang hiển thị sau khi
            // thao tác database đã thành công.
            room.TotalRows = totalRows;
            room.TotalCols = totalCols;

            _hasUnsavedChanges = false;
            _saveButton.Enabled = false;

            RenderSeats();

            _roomInfo.Text =
                $"{room.Name}  ·  {GetScreenTypeText(room.ScreenType)}  ·  " +
                $"{room.TotalRows} hàng × {room.TotalCols} cột";

            MessageHelper.Info("Đã tạo lại sơ đồ ghế.");
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);
        }
    }


    private void RenderSeats()
    {
        _grid.SuspendLayout();
        _grid.Controls.Clear();
        _seatById.Clear();
        _originalSeatTypes.Clear();

        try
        {
            if (RoomId <= 0)
                return;

            var seats = _rooms.GetSeats(RoomId)
                .OrderBy(seat => seat.RowLabel)
                .ThenBy(seat => seat.ColNumber)
                .ToList();

            foreach (var row in seats.GroupBy(seat => seat.RowLabel))
            {
                var rowPanel = new FlowLayoutPanel
                {
                    AutoSize = true,
                    WrapContents = false,
                    FlowDirection = FlowDirection.LeftToRight,
                    Margin = new Padding(0, 4, 0, 4),
                    Padding = Padding.Empty
                };

                rowPanel.Controls.Add(new Label
                {
                    Text = row.Key,
                    Width = 36,
                    Height = 54,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = ThemeHelper.SectionFont,
                    ForeColor = ThemeHelper.Primary
                });

                foreach (var seat in row)
                {
                    var button = CreateSeatButton(seat);

                    button.Click += (_, _) =>
                        ChangeSeatType(seat, button);

                    _seatById[seat.Id] = seat;
                    _originalSeatTypes[seat.Id] = seat.SeatType;

                    rowPanel.Controls.Add(button);
                }

                _grid.Controls.Add(rowPanel);
            }

            if (seats.Count == 0)
            {
                _grid.Controls.Add(new Label
                {
                    Text = "Phòng chưa có sơ đồ ghế. Chọn số hàng, số cột rồi nhấn Tạo lại sơ đồ.",
                    AutoSize = true,
                    Font = ThemeHelper.BodyFont,
                    ForeColor = ThemeHelper.Muted,
                    Margin = new Padding(20)
                });
            }

            UpdateSeatSummary();
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);
        }
        finally
        {
            _grid.ResumeLayout();
        }
    }

    private static Button CreateSeatButton(Seat seat)
    {
        var button = new Button
        {
            Text = $"{seat.SeatCode}\n{GetSeatTypeText(seat.SeatType)}",
            Size = new Size(70, 54),
            Margin = new Padding(4),
            FlatStyle = FlatStyle.Flat,
            BackColor = GetSeatColor(seat.SeatType),
            ForeColor = ThemeHelper.Text,
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };

        button.FlatAppearance.BorderColor = ThemeHelper.Border;
        button.FlatAppearance.BorderSize = 1;

        button.FlatAppearance.MouseOverBackColor =
            ControlPaint.Light(button.BackColor);

        button.FlatAppearance.MouseDownBackColor =
            ControlPaint.Dark(button.BackColor);

        button.AccessibleName = $"Ghế {seat.SeatCode}";
        button.AccessibleDescription = GetSeatTypeText(seat.SeatType);

        return button;
    }

    private void ChangeSeatType(Seat seat, Button button)
    {
        var types = Enum.GetValues<SeatType>();
        var currentIndex = Array.IndexOf(types, seat.SeatType);
        var nextIndex = (currentIndex + 1) % types.Length;

        seat.SeatType = types[nextIndex];

        button.Text =
            $"{seat.SeatCode}\n{GetSeatTypeText(seat.SeatType)}";

        button.BackColor = GetSeatColor(seat.SeatType);

        button.FlatAppearance.MouseOverBackColor =
            ControlPaint.Light(button.BackColor);

        button.FlatAppearance.MouseDownBackColor =
            ControlPaint.Dark(button.BackColor);

        button.AccessibleDescription =
            GetSeatTypeText(seat.SeatType);

        _hasUnsavedChanges = _seatById.Values.Any(item =>
            !_originalSeatTypes.TryGetValue(item.Id, out var original) ||
            original != item.SeatType);

        _saveButton.Enabled = _hasUnsavedChanges;

        UpdateSeatSummary();
    }


    private void SaveSeatTypes()
    {
        var changedSeats = _seatById.Values
            .Where(seat =>
                !_originalSeatTypes.TryGetValue(seat.Id, out var original) ||
                original != seat.SeatType)
            .ToList();

        if (changedSeats.Count == 0)
        {
            MessageHelper.Warning("Chưa có thay đổi loại ghế cần lưu.");
            _saveButton.Enabled = false;
            return;
        }

        try
        {
            // Lưu tất cả ghế trong một transaction.
            _rooms.UpdateSeatTypes(changedSeats);

            // Chỉ cập nhật trạng thái giao diện sau khi lưu thành công.
            foreach (var seat in changedSeats)
                _originalSeatTypes[seat.Id] = seat.SeatType;

            _hasUnsavedChanges = false;
            _saveButton.Enabled = false;

            MessageHelper.Info(
                $"Đã lưu {changedSeats.Count} thay đổi loại ghế.");
        }
        catch (Exception ex)
        {
            // Giữ nguyên trạng thái chưa lưu để người dùng có thể thử lại.
            MessageHelper.Handle(ex);
        }
    }


    private void UpdateSeatSummary()
    {
        var normal = _seatById.Values.Count(
            seat => seat.SeatType == SeatType.Normal);

        var vip = _seatById.Values.Count(
            seat => seat.SeatType == SeatType.VIP);

        var couple = _seatById.Values.Count(
            seat => seat.SeatType == SeatType.Couple);

        var aisle = _seatById.Values.Count(
            seat => seat.SeatType == SeatType.Aisle);

        _seatSummary.Text =
            $"Tổng {_seatById.Count} ghế  ·  " +
            $"Thường {normal}  ·  VIP {vip}  ·  " +
            $"Đôi {couple}  ·  Lối đi {aisle}";
    }

    private void FrmSeatDesigner_FormClosing(
        object? sender,
        FormClosingEventArgs e)
    {
        if (_hasUnsavedChanges &&
            !MessageHelper.Confirm(
                "Bạn còn thay đổi loại ghế chưa lưu. Đóng cửa sổ và bỏ các thay đổi này?"))
        {
            e.Cancel = true;
        }
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

    private static string GetSeatTypeText(SeatType type)
    {
        return type switch
        {
            SeatType.Normal => "Thường",
            SeatType.VIP => "VIP",
            SeatType.Couple => "Ghế đôi",
            SeatType.Aisle => "Lối đi",
            _ => type.ToString()
        };
    }

    private static Color GetSeatColor(SeatType type)
    {
        return type switch
        {
            SeatType.VIP => Color.FromArgb(254, 243, 199),
            SeatType.Couple => Color.FromArgb(237, 233, 254),
            SeatType.Aisle => Color.FromArgb(226, 232, 240),
            _ => Color.FromArgb(220, 252, 231)
        };
    }
}
