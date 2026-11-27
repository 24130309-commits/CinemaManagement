
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Core.DTOs;
using CinemaManagement.Core.Enums;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.UI.Controls;
using CinemaManagement.UI.Helpers;

namespace CinemaManagement.UI.Forms.Sales;

public partial class frmSeatSelection : Form
{
    private readonly ISeatMapService _seatMap;
    private readonly ISeatHoldService _holds;
    private readonly SalesOrderContext _order;

    private readonly FlowLayoutPanel _seatPanel = new();
    private readonly Label _summary = new();
    private readonly Label _selectedInfo = new();

    private readonly Dictionary<int, Button> _buttons = new();
    private readonly Dictionary<int, SeatDisplayDto> _seats = new();
    private readonly System.Windows.Forms.Timer _countdownTimer = new()
    {
        Interval = 1000
    };

    private bool _confirmed;

    private static readonly Color AvailableColor =
        Color.FromArgb(219, 234, 254);

    private static readonly Color VipColor =
        Color.FromArgb(254, 243, 199);

    private static readonly Color CoupleColor =
        Color.FromArgb(237, 233, 254);

    private static readonly Color SelectedColor =
        Color.FromArgb(255, 138, 50);

    private static readonly Color HoldingColor =
        Color.FromArgb(255, 237, 213);

    private static readonly Color BookedColor =
        Color.FromArgb(254, 202, 202);

    private static readonly Color DisabledColor =
        Color.FromArgb(203, 213, 225);

    public frmSeatSelection(
        ISeatMapService seatMap,
        ISeatHoldService holds,
        SalesOrderContext order)
    {
        InitializeComponent();

        _seatMap = seatMap;
        _holds = holds;
        _order = order;

        BuildUi();
        LoadSeats();
        _countdownTimer.Tick += (_, _) => UpdateCountdown();
        _countdownTimer.Start();
        ThemeHelper.Apply(this);
        ThemeHelper.FitToWorkingArea(this, new Size(1000, 740), new Size(680, 520));

        FormClosing += (_, _) =>
        {
            _countdownTimer.Stop();
            _countdownTimer.Dispose();
            if (!_confirmed)
                ReleaseSelectedSeats();
        };
    }

    private void BuildUi()
    {
        Text = "Bán vé - Chọn ghế";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(1000, 740);
        MinimumSize = new Size(680, 520);
        BackColor = ThemeHelper.Background;
        Font = ThemeHelper.BodyFont;
        Padding = new Padding(16);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            BackColor = ThemeHelper.Background
        };

        layout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));

        layout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 78));

        layout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 76));

        layout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 48));

        layout.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        layout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 64));

        layout.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 78));

        var heading = new PageHeader(
            "Chọn ghế",
            "Chọn ghế trống trên sơ đồ. Ghế giữ chỗ sẽ được dành riêng cho giao dịch hiện tại.");

        var showtimeInfo = BuildShowtimeInfo();

        var screen = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(90, 5, 90, 5),
            BackColor = ThemeHelper.Background
        };

        screen.Controls.Add(new Label
        {
            Text = "MÀN HÌNH",
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(203, 213, 225),
            ForeColor = ThemeHelper.Primary,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        });

        _seatPanel.Dock = DockStyle.Fill;
        _seatPanel.AutoScroll = true;
        _seatPanel.FlowDirection = FlowDirection.TopDown;
        _seatPanel.WrapContents = false;
        _seatPanel.Padding = new Padding(12);
        _seatPanel.BackColor = Color.White;
        _seatPanel.Margin = new Padding(0);

        layout.Controls.Add(heading, 0, 0);
        layout.Controls.Add(showtimeInfo, 0, 1);
        layout.Controls.Add(screen, 0, 2);
        layout.Controls.Add(_seatPanel, 0, 3);
        layout.Controls.Add(BuildLegend(), 0, 4);
        layout.Controls.Add(BuildFooter(), 0, 5);

        Controls.Add(layout);

    }

    private Control BuildShowtimeInfo()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(14, 8, 14, 8),
            Margin = new Padding(0, 0, 0, 8)
        };

        var showtime = _order.Showtime;

        panel.Controls.Add(new Label
        {
            Text = showtime == null
                ? "Chưa có thông tin suất chiếu"
                : $"Phòng: {showtime.RoomName}    |    " +
                  $"{showtime.StartTime:dd/MM/yyyy}    |    " +
                  $"{showtime.StartTime:HH:mm} - {showtime.EndTime:HH:mm}",

            Dock = DockStyle.Fill,
            Font = ThemeHelper.SmallFont,
            ForeColor = ThemeHelper.Muted,
            TextAlign = ContentAlignment.MiddleLeft
        });

        panel.Controls.Add(new Label
        {
            Text = showtime?.MovieTitle ?? "Chưa chọn phim",
            Dock = DockStyle.Top,
            Height = 28,
            Font = ThemeHelper.SectionFont,
            ForeColor = ThemeHelper.Primary
        });

        return panel;
    }

    private Control BuildLegend()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            BackColor = ThemeHelper.Background,
            Padding = new Padding(4, 3, 4, 3),
            AutoScroll = true
        };

        AddLegendItem(panel, AvailableColor, "Ghế thường");
        AddLegendItem(panel, VipColor, "Ghế VIP");
        AddLegendItem(panel, CoupleColor, "Ghế đôi");
        AddLegendItem(panel, SelectedColor, "Đang chọn");
        AddLegendItem(panel, HoldingColor, "Đang giữ");
        AddLegendItem(panel, BookedColor, "Đã bán");
        AddLegendItem(panel, DisabledColor, "Ngừng sử dụng");

        return panel;
    }

    private static void AddLegendItem(
        FlowLayoutPanel panel,
        Color color,
        string text)
    {
        var item = new Panel
        {
            Width = 112,
            Height = 28,
            Margin = new Padding(2, 0, 2, 0)
        };

        item.Controls.Add(new Panel
        {
            Location = new Point(0, 5),
            Size = new Size(16, 16),
            BackColor = color
        });

        item.Controls.Add(new Label
        {
            Text = text,
            Location = new Point(21, 3),
            Size = new Size(90, 22),
            Font = new Font("Segoe UI", 7.5F),
            ForeColor = ThemeHelper.Text,
            TextAlign = ContentAlignment.MiddleLeft
        });

        panel.Controls.Add(item);
    }

    private Control BuildFooter()
    {
        var footer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(12, 7, 12, 7)
        };

        _summary.Dock = DockStyle.Top;
        _summary.Height = 28;
        _summary.Font = ThemeHelper.SectionFont;
        _summary.ForeColor = ThemeHelper.Primary;
        _summary.TextAlign = ContentAlignment.MiddleLeft;

        _selectedInfo.Dock = DockStyle.Fill;
        _selectedInfo.Font = ThemeHelper.SmallFont;
        _selectedInfo.ForeColor = ThemeHelper.Muted;
        _selectedInfo.TextAlign = ContentAlignment.MiddleLeft;

        var next = new Button
        {
            Text = "Tiếp tục  →",
            Dock = DockStyle.Right,
            Width = 145,
            BackColor = ThemeHelper.Accent,
            ForeColor = ThemeHelper.Primary,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
            Font = ThemeHelper.SectionFont
        };

        next.FlatAppearance.BorderSize = 0;
        next.Click += (_, _) => ConfirmSelection();

        var refresh = new Button
        {
            Text = "Làm mới ghế",
            Dock = DockStyle.Right,
            Width = 125,
            BackColor = ThemeHelper.Primary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
            Font = ThemeHelper.SmallFont
        };

        refresh.FlatAppearance.BorderSize = 0;
        refresh.Click += (_, _) => RefreshSeats();

        footer.Controls.Add(_selectedInfo);
        footer.Controls.Add(_summary);
        footer.Controls.Add(refresh);
        footer.Controls.Add(next);

        return footer;
    }

    private void LoadSeats()
    {
        if (_order.Showtime == null)
            throw new InvalidOperationException(
                "Chưa chọn suất chiếu.");

        var seats = _seatMap.GetSeatMapByShowtime(
            _order.Showtime.Id);

        _seatPanel.Controls.Clear();
        _buttons.Clear();
        _seats.Clear();

        foreach (var row in seats
                     .GroupBy(seat => seat.RowLabel)
                     .OrderBy(group => group.Key))
        {
            var rowPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight,
                Margin = new Padding(0, 4, 0, 4),
                Anchor = AnchorStyles.None
            };

            rowPanel.Controls.Add(new Label
            {
                Text = row.Key,
                Width = 34,
                Height = 42,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = ThemeHelper.Primary,
                Margin = new Padding(2, 3, 6, 3)
            });

            foreach (var seat in row.OrderBy(item => item.ColNumber))
            {
                var button = CreateSeatButton(seat);

                button.Click += (_, _) =>
                    ToggleSeat(seat, button);

                rowPanel.Controls.Add(button);

                _buttons[seat.Id] = button;
                _seats[seat.Id] = seat;
            }

            _seatPanel.Controls.Add(rowPanel);
        }

        if (seats.Count == 0)
        {
            _seatPanel.Controls.Add(new Label
            {
                Text = "Không có dữ liệu sơ đồ ghế cho phòng này.",
                AutoSize = true,
                ForeColor = ThemeHelper.Muted,
                Font = ThemeHelper.SectionFont,
                Margin = new Padding(20)
            });
        }

        UpdateSummary();
    }

    private Button CreateSeatButton(SeatDisplayDto seat)
    {
        var button = new Button
        {
            Text = $"{seat.RowLabel}{seat.ColNumber}",
            Size = new Size(54, 42),
            Margin = new Padding(3),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Tag = seat,
            UseVisualStyleBackColor = false,
            AccessibleName = $"Ghế {seat.RowLabel}{seat.ColNumber}"
        };

        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = ThemeHelper.Border;

        // Ghế đang được giữ bởi giao dịch hiện tại
        bool selectedByCurrentOrder =
            _order.SelectedSeatIds.Contains(seat.Id) &&
            _order.HoldIds.ContainsKey(seat.Id);

        if (selectedByCurrentOrder)
        {
            button.Enabled = true;
            button.BackColor = SelectedColor;
            button.ForeColor = ThemeHelper.Primary;
            button.AccessibleDescription = "Đang chọn và được giữ cho giao dịch hiện tại.";
            return button;
        }

        switch (seat.Status)
        {
            case SeatStatus.Available:
                button.Enabled = true;
                button.BackColor = GetSeatColor(seat);
                button.ForeColor = ThemeHelper.Text;
                button.AccessibleDescription = "Ghế đang trống.";
                break;

            case SeatStatus.Holding:
                button.Enabled = false;
                button.BackColor = HoldingColor;
                button.ForeColor = Color.FromArgb(124, 45, 18);
                button.Cursor = Cursors.Default;
                button.AccessibleDescription = "Ghế đang được giữ bởi giao dịch khác.";
                break;

            case SeatStatus.Booked:
                button.Enabled = false;
                button.BackColor = BookedColor;
                button.ForeColor = Color.FromArgb(153, 27, 27);
                button.Cursor = Cursors.Default;
                button.AccessibleDescription = "Ghế đã được bán.";
                break;

            case SeatStatus.Disabled:
            default:
                button.Enabled = false;
                button.BackColor = DisabledColor;
                button.ForeColor = Color.FromArgb(100, 116, 139);
                button.Cursor = Cursors.Default;
                button.AccessibleDescription = "Ghế không khả dụng.";
                break;
        }

        return button;
    }

    private static Color GetSeatColor(SeatDisplayDto seat)
    {
        return seat.SeatType switch
        {
            SeatType.VIP => VipColor,
            SeatType.Couple => CoupleColor,
            _ => AvailableColor
        };
    }

    // Chọn hoặc bỏ chọn ghế
    private void ToggleSeat(
        SeatDisplayDto seat,
        Button button)
    {
        if (_order.Showtime == null ||
            _order.Customer == null)
        {
            MessageHelper.Warning(
                "Vui lòng xác nhận khách hàng và suất chiếu trước.");

            return;
        }

        try
        {
            if (_order.SelectedSeatIds.Contains(seat.Id))
            {
                if (_order.HoldIds.TryGetValue(
                    seat.Id, out var holdId))
                {
                    _holds.ReleaseHold(holdId);
                }

                _order.HoldIds.Remove(seat.Id);
                _order.HoldExpiresAt.Remove(seat.Id);
                _order.SelectedSeatIds.Remove(seat.Id);

                button.BackColor = GetSeatColor(seat);
                button.ForeColor = ThemeHelper.Text;
                button.AccessibleDescription = "Ghế đang trống.";
            }
            else
            {
                var hold = _holds.HoldSeat(
                    _order.Showtime.Id,
                    seat.Id,
                    _order.Customer.Id);

                _order.HoldIds[seat.Id] = hold.Id;
                _order.HoldExpiresAt[seat.Id] = hold.ExpiresAt;
                _order.SelectedSeatIds.Add(seat.Id);

                button.BackColor = SelectedColor;
                button.ForeColor = ThemeHelper.Primary;
                button.AccessibleDescription = "Đang chọn và được giữ cho giao dịch hiện tại.";
            }

            UpdateSummary();
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);

            // Tải lại trạng thái ghế nếu thao tác giữ chỗ thất bại
            try
            {
                LoadSeats();
            }
            catch
            {
                // Giữ nguyên thông báo lỗi ban đầu.
            }
        }
    }

    // Làm mới sơ đồ và kiểm tra lại các ghế đang giữ
    private void RefreshSeats()
    {
        if (_order.Showtime == null ||
            _order.Customer == null)
        {
            MessageHelper.Warning(
                "Thông tin suất chiếu hoặc khách hàng không hợp lệ.");

            return;
        }

        var lostSeats = new List<string>();

        foreach (var seatId in _order.SelectedSeatIds.ToList())
        {
            if (!_seats.TryGetValue(seatId, out var seat))
                continue;

            try
            {
                // Gia hạn hoặc xác nhận lại quyền giữ ghế
                var hold = _holds.HoldSeat(
                    _order.Showtime.Id,
                    seatId,
                    _order.Customer.Id);

                _order.HoldIds[seatId] = hold.Id;
                _order.HoldExpiresAt[seatId] = hold.ExpiresAt;
            }
            catch
            {
                lostSeats.Add(
                    $"{seat.RowLabel}{seat.ColNumber}");

                if (_order.HoldIds.TryGetValue(
                    seatId, out var oldHoldId))
                {
                    try
                    {
                        _holds.ReleaseHold(oldHoldId);
                    }
                    catch
                    {
                    }
                }

                _order.HoldIds.Remove(seatId);
                _order.HoldExpiresAt.Remove(seatId);
                _order.SelectedSeatIds.Remove(seatId);
            }
        }

        try
        {
            LoadSeats();

            if (lostSeats.Count > 0)
            {
                MessageHelper.Warning(
                    "Một số ghế không còn giữ được:\n" +
                    string.Join(", ", lostSeats) +
                    "\n\nVui lòng chọn ghế khác.");
            }
            else
            {
                MessageHelper.Info(
                    "Đã cập nhật sơ đồ ghế mới nhất.");
            }
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);
        }
    }

    private void UpdateSummary()
    {
        var selected = _order.SelectedSeatIds
            .Where(_seats.ContainsKey)
            .Select(id => _seats[id])
            .ToList();

        var total = selected.Sum(seat => seat.Price);

        _summary.Text =
            $"Đã chọn: {selected.Count} ghế    |    " +
            $"Tạm tính: {total:N0} đ";

        _selectedInfo.Text = selected.Count == 0
            ? "Chưa có ghế nào được chọn."
            : $"Ghế đã chọn: {string.Join(", ",
                selected.Select(seat =>
                    $"{seat.RowLabel}{seat.ColNumber}"))}";

        UpdateCountdownText();
    }

    private void UpdateCountdown()
    {
        var now = DateTime.Now;
        var expiredSeatIds = _order.HoldExpiresAt
            .Where(pair => pair.Value <= now)
            .Select(pair => pair.Key)
            .ToList();

        foreach (var seatId in expiredSeatIds)
        {
            if (_order.HoldIds.TryGetValue(seatId, out var holdId))
            {
                try { _holds.ReleaseHold(holdId); }
                catch { /* The hold may already have expired in the database. */ }
            }

            _order.HoldIds.Remove(seatId);
            _order.SelectedSeatIds.Remove(seatId);
            _order.HoldExpiresAt.Remove(seatId);
        }

        if (expiredSeatIds.Count > 0)
        {
            LoadSeats();
            MessageHelper.Warning(
                "Đã hết thời gian giữ ghế. Ghế được chọn đã nhả, vui lòng chọn lại.");
            return;
        }

        UpdateCountdownText();
    }

    private void UpdateCountdownText()
    {
        if (_order.HoldExpiresAt.Count == 0)
            return;

        const string countdownPrefix = "    |    Thời gian giữ ghế còn:";
        var existingCountdown = _selectedInfo.Text.IndexOf(
            countdownPrefix,
            StringComparison.Ordinal);
        if (existingCountdown >= 0)
            _selectedInfo.Text = _selectedInfo.Text[..existingCountdown];

        var remaining = _order.HoldExpiresAt.Values.Min() - DateTime.Now;
        var totalSeconds = Math.Max(0, (int)Math.Ceiling(remaining.TotalSeconds));
        var minutes = totalSeconds / 60;
        var seconds = totalSeconds % 60;
        _selectedInfo.Text += $"    |    Thời gian giữ ghế còn: {minutes:00}:{seconds:00}";
    }

    private void ConfirmSelection()
    {
        if (_order.SelectedSeatIds.Count == 0)
        {
            MessageHelper.Warning(
                "Vui lòng chọn ít nhất một ghế.");

            return;
        }

        if (_order.Showtime == null ||
            _order.Customer == null)
        {
            MessageHelper.Warning(
                "Thông tin khách hàng hoặc suất chiếu không hợp lệ.");

            return;
        }

        try
        {
            // Kiểm tra và gia hạn tất cả ghế trước khi chuyển bước
            foreach (var seatId in _order.SelectedSeatIds.ToList())
            {
                var hold = _holds.HoldSeat(
                    _order.Showtime.Id,
                    seatId,
                    _order.Customer.Id);

                _order.HoldIds[seatId] = hold.Id;
                _order.HoldExpiresAt[seatId] = hold.ExpiresAt;
            }

            _confirmed = true;
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);

            // Không chuyển sang bước tiếp theo nếu có ghế bị mất
            try
            {
                RefreshSeats();
            }
            catch
            {
            }
        }
    }

    private void ReleaseSelectedSeats()
    {
        foreach (var holdId in _order.HoldIds.Values.ToList())
        {
            try
            {
                _holds.ReleaseHold(holdId);
            }
            catch
            {
                // Tiếp tục giải phóng các ghế còn lại.
            }
        }

        _order.HoldIds.Clear();
        _order.HoldExpiresAt.Clear();
        _order.SelectedSeatIds.Clear();
    }
}
