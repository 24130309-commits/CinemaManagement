
using System.Drawing;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.BLL.Session;
using CinemaManagement.UI.Controls;
using CinemaManagement.UI.Helpers;

namespace CinemaManagement.UI.Forms.Sales;

public partial class frmInvoiceHistory : Form
{
    private readonly ICustomerService _customers;
    private readonly IBookingService _bookings;
    private readonly ITicketPrintService _tickets;
    private readonly IPaymentService _payments;
    private readonly CurrentUserSession _session;

    private readonly TextBox _phone = new();
    private readonly EmptyStateDataGridView _grid = new();
    private readonly Label _status = new();

    public frmInvoiceHistory(
        ICustomerService customers,
        IBookingService bookings,
        ITicketPrintService tickets,
        IPaymentService payments,
        CurrentUserSession session)
    {
        InitializeComponent();

        _customers = customers;
        _bookings = bookings;
        _tickets = tickets;
        _payments = payments;
        _session = session;

        BuildUi();
        ThemeHelper.Apply(this);
        ThemeHelper.FitToWorkingArea(this, new Size(1050, 680), new Size(820, 520));
    }

    private void BuildUi()
    {
        Text = "Lịch sử hóa đơn";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(1050, 680);
        MinimumSize = new Size(820, 520);
        BackColor = ThemeHelper.Background;
        Font = ThemeHelper.BodyFont;
        Padding = new Padding(18);

        var title = new PageHeader(
            "Lịch sử hóa đơn",
            "Tra cứu giao dịch theo số điện thoại, xem vé và theo dõi trạng thái thanh toán.");

        var search = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 62,
            BackColor = Color.White,
            Padding = new Padding(10),
            WrapContents = true,
            AutoScroll = true
        };

        _phone.Width = 300;
        _phone.Height = 32;
        _phone.PlaceholderText = "Nhập số điện thoại khách hàng";
        _phone.Font = ThemeHelper.BodyFont;

        _phone.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                LoadHistory();
            }
        };

        var find = CreateButton("Tra cứu", ThemeHelper.Accent);
        find.Click += (_, _) => LoadHistory();

        var clear = CreateButton("Làm mới", ThemeHelper.Muted);
        clear.Click += (_, _) =>
        {
            _phone.Clear();
            _grid.Rows.Clear();
            _status.Text = "Nhập số điện thoại để tra cứu lịch sử giao dịch.";
        };

        search.Controls.AddRange([_phone, find, clear]);

        _grid.Dock = DockStyle.Fill;
        _grid.EmptyStateText = "Nhập số điện thoại để xem các giao dịch của khách hàng.";
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.RowHeadersVisible = false;
        _grid.MultiSelect = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.AutoGenerateColumns = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Id",
            HeaderText = "Mã",
            FillWeight = 9
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Code",
            HeaderText = "Mã giao dịch",
            FillWeight = 24
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "CreatedAt",
            HeaderText = "Ngày tạo",
            FillWeight = 20
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Payment",
            HeaderText = "PTTT",
            FillWeight = 15
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Total",
            HeaderText = "Tổng tiền",
            FillWeight = 17
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Status",
            HeaderText = "Trạng thái",
            FillWeight = 15
        });

        ThemeHelper.ApplyDefaultGridStyle(_grid);
        _grid.CellDoubleClick += (_, _) => ViewTicket();
        _grid.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 ||
                _grid.Columns[e.ColumnIndex].Name != "Status")
            {
                return;
            }

            var tone = Convert.ToString(e.Value) switch
            {
                "Paid" => (ThemeHelper.Success, ThemeHelper.SuccessSoft),
                "Refunded" => (ThemeHelper.Danger, ThemeHelper.DangerSoft),
                "Pending" => (ThemeHelper.Warning, ThemeHelper.WarningSoft),
                _ => (ThemeHelper.Muted, ThemeHelper.SurfaceAlt)
            };

            e.CellStyle.ForeColor = tone.Item1;
            e.CellStyle.BackColor = tone.Item2;
            e.CellStyle.SelectionForeColor = tone.Item1;
            e.CellStyle.SelectionBackColor = tone.Item2;
            e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            e.CellStyle.Font = ThemeHelper.SectionFont;
        };

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 76,
            BackColor = Color.White,
            Padding = new Padding(6),
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = true,
            AutoScroll = true
        };

        _status.Width = 300;
        _status.Height = 40;
        _status.AutoEllipsis = true;
        _status.Margin = new Padding(8, 5, 8, 2);
        _status.ForeColor = ThemeHelper.Muted;
        _status.TextAlign = ContentAlignment.MiddleLeft;

        var export = CreateButton("Xuất PDF", ThemeHelper.Accent);
        export.Click += (_, _) => ExportTicket();

        var view = CreateButton("Xem vé / hóa đơn", ThemeHelper.Primary);
        view.Click += (_, _) => ViewTicket();

        var refund = CreateButton("Ghi nhận hoàn tiền", Color.FromArgb(185, 65, 65));
        refund.Click += (_, _) => RefundBooking();

        footer.Controls.Add(_status);
        if (_session.IsAdmin)
            footer.Controls.Add(refund);
        footer.Controls.Add(view);
        footer.Controls.Add(export);

        Controls.Add(_grid);
        Controls.Add(footer);
        Controls.Add(search);
        Controls.Add(title);
    }

    private void LoadHistory()
    {
        var phone = _phone.Text.Trim();

        if (string.IsNullOrWhiteSpace(phone))
        {
            MessageHelper.Warning("Vui lòng nhập số điện thoại khách hàng.");
            _phone.Focus();
            return;
        }

        try
        {
            var customer = _customers.FindByPhone(phone);

            if (customer == null)
            {
                _grid.Rows.Clear();
                _status.Text = "Không tìm thấy khách hàng.";
                MessageHelper.Warning(
                    "Không tìm thấy khách hàng theo số điện thoại này.");
                return;
            }

            _grid.Rows.Clear();

            var history = _bookings.GetBookingHistory(customer.Id)
                .OrderByDescending(booking => booking.CreatedAt)
                .ToList();

            foreach (var booking in history)
            {
                var row = _grid.Rows.Add(
                    booking.Id,
                    booking.Code,
                    booking.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                    booking.PaymentMethod.ToString(),
                    $"{booking.Total:N0} đ",
                    booking.Status.ToString());

                _grid.Rows[row].Tag = booking;
            }

            _status.Text =
                $"Khách hàng: {customer.FullName} · {customer.Phone} · {history.Count} hóa đơn";

            if (history.Count == 0)
                MessageHelper.Info("Khách hàng chưa có hóa đơn.");
            else
                _grid.Rows[0].Selected = true;
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);
        }
    }

    private void ExportTicket()
    {
        if (_grid.CurrentRow?.Tag is not Booking booking)
        {
            MessageHelper.Warning("Vui lòng chọn một hóa đơn.");
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Title = "Xuất hóa đơn PDF",
            Filter = "PDF (*.pdf)|*.pdf",
            FileName = $"Hoa-don-{booking.Code}.pdf",
            DefaultExt = "pdf",
            AddExtension = true,
            OverwritePrompt = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            _tickets.ExportTicketToPdf(
                booking.Id,
                dialog.FileName);

            MessageHelper.Info("Đã xuất hóa đơn PDF thành công.");
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);
        }
    }

    private void ViewTicket()
    {
        if (_grid.CurrentRow?.Tag is not Booking booking)
        {
            MessageHelper.Warning("Vui lòng chọn một hóa đơn.");
            return;
        }

        try
        {
            var content = _tickets.GenerateTicketContent(booking.Id);

            using var dialog = new Form
            {
                Text = $"Hóa đơn {booking.Code}",
                StartPosition = FormStartPosition.CenterParent,
                Size = new Size(620, 700),
                MinimumSize = new Size(520, 560),
                BackColor = ThemeHelper.Background,
                Padding = new Padding(16),
                Font = ThemeHelper.BodyFont,
                ShowInTaskbar = false
            };

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = ThemeHelper.Background
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));

            var header = new PageHeader(
                $"Hóa đơn {booking.Code}",
                "Bản xem trước nội dung vé và hóa đơn.");

            var preview = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                WordWrap = false,
                ScrollBars = ScrollBars.Both,
                Font = new Font("Consolas", 10),
                BackColor = Color.White,
                Text = content
            };

            var footer = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Padding = new Padding(0, 9, 0, 0),
                BackColor = ThemeHelper.Background
            };

            var close = new Button
            {
                Text = "Đóng",
                Size = new Size(96, 38)
            };
            close.DialogResult = DialogResult.Cancel;
            ThemeHelper.StyleActionButton(close, ThemeHelper.ActionButtonRole.Secondary);
            footer.Controls.Add(close);

            root.Controls.Add(header, 0, 0);
            root.Controls.Add(preview, 0, 1);
            root.Controls.Add(footer, 0, 2);
            dialog.Controls.Add(root);
            dialog.CancelButton = close;
            dialog.AcceptButton = close;

            dialog.ShowDialog(this);
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);
        }
    }

    private void RefundBooking()
    {
        if (!_session.IsAdmin)
        {
            MessageHelper.Warning("Bạn không có quyền ghi nhận hoàn tiền.");
            return;
        }

        if (_grid.CurrentRow?.Tag is not Booking booking)
        {
            MessageHelper.Warning("Vui lòng chọn một hóa đơn.");
            return;
        }

        if (booking.Status != CinemaManagement.Core.Enums.BookingStatus.Paid)
        {
            MessageHelper.Warning("Chỉ hóa đơn đã thanh toán mới có thể hoàn tiền.");
            return;
        }

        var note = PromptRefundReason();
        if (note is null)
            return;

        if (MessageHelper.Decide(
                $"Ghi nhận hoàn tiền cho hóa đơn {booking.Code} với tổng tiền {booking.Total:N0} đ? Vé sẽ được đánh dấu đã hoàn.",
                "Xác nhận hoàn tiền",
                MessageBoxIcon.Warning,
                MessageBoxButtons.YesNo) != DialogResult.Yes)
        {
            return;
        }

        try
        {
            _payments.RefundBooking(booking.Id, note);
            MessageHelper.Info("Đã ghi nhận hoàn tiền và cập nhật trạng thái vé.");
            LoadHistory();
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);
        }
    }

    private string? PromptRefundReason()
    {
        using var dialog = new Form
        {
            Text = "Lý do hoàn tiền",
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MinimizeBox = false,
            MaximizeBox = false,
            ClientSize = new Size(560, 360),
            MinimumSize = new Size(500, 320),
            BackColor = ThemeHelper.Background,
            Font = ThemeHelper.BodyFont,
            ShowInTaskbar = false,
            AccessibleRole = AccessibleRole.Dialog,
            AccessibleName = "Lý do hoàn tiền"
        };

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(16),
            BackColor = ThemeHelper.Background
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));

        var header = new PageHeader(
            "Lý do hoàn tiền",
            "Ghi chú sẽ được lưu kèm giao dịch; tối đa 500 ký tự.");

        var input = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            MaxLength = 500,
            ScrollBars = ScrollBars.Vertical,
            Margin = new Padding(8, 10, 8, 8),
            AccessibleName = "Ghi chú hoàn tiền",
            AccessibleDescription = "Nhập lý do hoàn tiền, tối đa 500 ký tự."
        };

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 10, 0, 0),
            BackColor = ThemeHelper.Background
        };

        var submit = new Button
        {
            Text = "Tiếp tục",
            DialogResult = DialogResult.OK,
            Size = new Size(110, 38),
            Margin = new Padding(6, 0, 0, 0)
        };
        ThemeHelper.StyleActionButton(submit, ThemeHelper.ActionButtonRole.Primary);

        var cancel = new Button
        {
            Text = "Hủy",
            DialogResult = DialogResult.Cancel,
            Size = new Size(90, 38)
        };
        ThemeHelper.StyleActionButton(cancel, ThemeHelper.ActionButtonRole.Secondary);

        actions.Controls.AddRange([submit, cancel]);
        root.Controls.Add(header, 0, 0);
        root.Controls.Add(input, 0, 1);
        root.Controls.Add(actions, 0, 2);
        dialog.Controls.Add(root);
        dialog.AcceptButton = submit;
        dialog.CancelButton = cancel;

        ThemeHelper.Apply(dialog);
        ThemeHelper.FitToWorkingArea(dialog, new Size(560, 360), new Size(500, 320));

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return null;

        if (string.IsNullOrWhiteSpace(input.Text))
        {
            MessageHelper.Warning("Vui lòng nhập lý do hoàn tiền.");
            return null;
        }

        return input.Text.Trim();
    }

    private static Button CreateButton(string text, Color color)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            Height = 34,
            BackColor = color,
            ForeColor = color == ThemeHelper.Accent
                ? ThemeHelper.Primary
                : Color.White,
            FlatStyle = FlatStyle.Flat,
            Padding = new Padding(12, 3, 12, 3),
            Margin = new Padding(6, 0, 6, 0),
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };

        ThemeHelper.StyleActionButton(button, color);
        return button;
    }
}
