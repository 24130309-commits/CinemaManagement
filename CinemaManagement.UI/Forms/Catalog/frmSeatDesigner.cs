using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.UI.Helpers;

namespace CinemaManagement.UI.Forms.Rooms;

public partial class frmSeatDesigner : Form
{
    private readonly IRoomService _rooms;

    private readonly ComboBox _room = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = 160
    };

    private readonly NumericUpDown _rows = new()
    {
        Minimum = 1,
        Maximum = 26,
        Value = 6,
        Width = 65
    };

    private readonly NumericUpDown _cols = new()
    {
        Minimum = 1,
        Maximum = 40,
        Value = 8,
        Width = 65
    };

    private readonly FlowLayoutPanel _grid = new()
    {
        Dock = DockStyle.Fill,
        AutoScroll = true,
        Padding = new Padding(12)
    };

    private readonly Dictionary<int, Seat> _seatById = new();

    public frmSeatDesigner(IRoomService rooms)
    {
        _rooms = rooms;

        InitializeComponent();

        BuildLayout();
        LoadRooms();
    }

    private void BuildLayout()
    {
        Text = "Thiết kế sơ đồ ghế";
        Width = 950;
        Height = 650;
        StartPosition = FormStartPosition.CenterParent;

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 58,
            Padding = new Padding(8),
            WrapContents = false
        };

        toolbar.Controls.AddRange(new Control[]
        {
            new Label
            {
                Text = "Phòng:",
                AutoSize = true,
                Padding = new Padding(0, 8, 0, 0)
            },
            _room,

            new Label
            {
                Text = "Số hàng:",
                AutoSize = true,
                Padding = new Padding(0, 8, 0, 0)
            },
            _rows,

            new Label
            {
                Text = "Số cột:",
                AutoSize = true,
                Padding = new Padding(0, 8, 0, 0)
            },
            _cols
        });

        var generate = new Button
        {
            Text = "Tạo lại sơ đồ",
            AutoSize = true
        };

        var save = new Button
        {
            Text = "Lưu loại ghế",
            AutoSize = true
        };

        generate.Click += (_, _) => GenerateSeats();
        save.Click += (_, _) => SaveSeatTypes();

        toolbar.Controls.AddRange(new Control[]
        {
            generate,
            save
        });

        _room.SelectedIndexChanged +=
            (_, _) => OnRoomChanged();

        Controls.Add(_grid);
        Controls.Add(toolbar);
    }

    private void LoadRooms()
    {
        _room.DataSource = _rooms.GetAllRooms().ToList();

        _room.DisplayMember = nameof(Room.Name);
        _room.ValueMember = nameof(Room.Id);

        if (_room.Items.Count > 0)
            OnRoomChanged();
    }

    private int RoomId =>
        _room.SelectedValue is int id ? id : 0;

    private void OnRoomChanged()
    {
        var room = _rooms.GetRoomById(RoomId);

        if (room != null)
        {
            _rows.Value = Math.Max(
                _rows.Minimum,
                Math.Min(_rows.Maximum, room.TotalRows));

            _cols.Value = Math.Max(
                _cols.Minimum,
                Math.Min(_cols.Maximum, room.TotalCols));
        }

        RenderSeats();
    }

    private void GenerateSeats()
    {
        if (RoomId <= 0)
            return;

        if (!MessageHelper.Confirm(
            "Tạo lại sơ đồ sẽ thay thế toàn bộ ghế hiện có. Bạn có muốn tiếp tục?"))
        {
            return;
        }

        try
        {
            _rooms.GenerateSeats(
                RoomId,
                (int)_rows.Value,
                (int)_cols.Value);

            RenderSeats();
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);
        }
    }

    private void RenderSeats()
    {
        _grid.Controls.Clear();
        _seatById.Clear();

        if (RoomId <= 0)
            return;

        foreach (var seat in _rooms.GetSeats(RoomId))
        {
            var button = new Button
            {
                Text = seat.SeatCode,
                Width = 64,
                Height = 46,
                Margin = new Padding(4),
                Tag = seat.Id,
                BackColor = GetSeatColor(seat.SeatType)
            };

            button.Click += (_, _) =>
            {
                seat.SeatType =
                    (SeatType)(((int)seat.SeatType + 1) %
                    Enum.GetValues<SeatType>().Length);

                button.BackColor =
                    GetSeatColor(seat.SeatType);

                button.Text =
                    $"{seat.SeatCode}\n{seat.SeatType}";
            };

            _seatById[seat.Id] = seat;
            _grid.Controls.Add(button);
        }
    }

    private void SaveSeatTypes()
    {
        try
        {
            foreach (var seat in _seatById.Values)
            {
                _rooms.UpdateSeatType(
                    seat.Id,
                    seat.SeatType);
            }

            MessageHelper.Info("Đã lưu sơ đồ ghế.");
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);
        }
    }

    private static Color GetSeatColor(SeatType type)
    {
        return type switch
        {
            SeatType.VIP => Color.Gold,
            SeatType.Couple => Color.LightPink,
            SeatType.Aisle => Color.LightGray,
            _ => Color.White
        };
    }
}