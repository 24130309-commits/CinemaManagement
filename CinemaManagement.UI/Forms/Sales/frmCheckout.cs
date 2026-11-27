
using System.Drawing;
using CinemaManagement.Core.Enums;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.UI.Controls;
using CinemaManagement.UI.Helpers;

namespace CinemaManagement.UI.Forms.Sales;

public partial class frmCheckout : Form
{
    private readonly SalesOrderContext _order;
    private readonly IBookingService _bookings;
    private readonly ISeatMapService _seatMap;
    private readonly IPromotionService _promotions;
    private readonly IProductService _products;

    private readonly ComboBox _payment = new();
    private readonly TextBox _promotionCode = new();
    private readonly Label _discountLabel = new();
    private readonly Label _total = new();
    private readonly Label _holdCountdown = new();
    private readonly TextBox _details = new();
    private readonly Button _confirm = new();
    private readonly System.Windows.Forms.Timer _holdTimer = new()
    {
        Interval = 1000
    };

    private decimal _subtotal;
    private decimal _ticketTotal;
    private decimal _productTotal;
    private string _seatNames = string.Empty;
    private bool _productPricesUpdated;
    private bool _holdExpired;
    private bool _paymentConfirmationOpen;

    public frmCheckout(
        SalesOrderContext order,
        IBookingService bookings,
        ISeatMapService seatMap,
        IPromotionService promotions,
        IProductService products)
    {
        InitializeComponent();

        _order = order;
        _bookings = bookings;
        _seatMap = seatMap;
        _promotions = promotions;
        _products = products;

        BuildUi();
        LoadSummary();
        _holdTimer.Tick += (_, _) => UpdateHoldCountdown();
        _holdTimer.Start();
        Shown += (_, _) => UpdateHoldCountdown();
        FormClosed += (_, _) =>
        {
            _holdTimer.Stop();
            _holdTimer.Dispose();
        };
        ThemeHelper.Apply(this);
        ThemeHelper.FitToWorkingArea(this, new Size(760, 700), new Size(650, 520));
    }

    private void BuildUi()
    {
        Text = "Bán vé - Thanh toán";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(760, 700);
        MinimumSize = new Size(650, 520);
        BackColor = ThemeHelper.Background;
        Font = ThemeHelper.BodyFont;
        Padding = new Padding(18);

        var title = new PageHeader(
            "Xác nhận và thanh toán",
            "Kiểm tra ghế, sản phẩm, ưu đãi và tổng tiền trước khi xác nhận.");

        _details.Dock = DockStyle.Fill;
        _details.Multiline = true;
        _details.ReadOnly = true;
        _details.ScrollBars = ScrollBars.Vertical;
        _details.BackColor = Color.White;
        _details.ForeColor = ThemeHelper.Text;
        _details.BorderStyle = BorderStyle.FixedSingle;
        _details.Font = new Font("Segoe UI", 10);
        _details.Padding = new Padding(12);

        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 250,
            BackColor = Color.White,
            Padding = new Padding(14)
        };

        var methodLabel = new Label
        {
            Text = "Phương thức thanh toán",
            Dock = DockStyle.Top,
            Height = 23,
            ForeColor = ThemeHelper.Muted
        };

        _payment.Dock = DockStyle.Top;
        _payment.DropDownStyle = ComboBoxStyle.DropDownList;
        _payment.DataSource = new[]
        {
            new PaymentOption("Tiền mặt", PaymentMethod.Cash),
            new PaymentOption("Thẻ ngân hàng", PaymentMethod.Card),
            new PaymentOption("Ví điện tử", PaymentMethod.EWallet),
            new PaymentOption("MoMo", PaymentMethod.MOMO),
            new PaymentOption("VNPay", PaymentMethod.VNPay)
        };
        _payment.DisplayMember = nameof(PaymentOption.Label);
        _payment.ValueMember = nameof(PaymentOption.Value);
        _payment.Font = ThemeHelper.BodyFont;

        var promotionPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 42,
            WrapContents = false,
            Padding = new Padding(0, 4, 0, 2)
        };

        _promotionCode.Width = 230;
        _promotionCode.PlaceholderText = "Nhập mã khuyến mãi";
        _promotionCode.TextChanged += (_, _) =>
        {
            if (!string.Equals(
                    _promotionCode.Text.Trim(),
                    _order.PromotionCode,
                    StringComparison.OrdinalIgnoreCase))
            {
                _order.PromotionCode = string.Empty;
                _order.Discount = 0;
                UpdateTotals();
            }
        };

        var apply = CreateButton("Áp dụng mã", ThemeHelper.Primary);
        apply.Click += (_, _) => ApplyPromotion();

        var clear = CreateButton("Bỏ mã", ThemeHelper.Muted);
        clear.Click += (_, _) => ClearPromotion();

        promotionPanel.Controls.AddRange([_promotionCode, apply, clear]);

        _discountLabel.Dock = DockStyle.Top;
        _discountLabel.Height = 24;
        _discountLabel.ForeColor = ThemeHelper.Success;
        _discountLabel.TextAlign = ContentAlignment.MiddleLeft;

        _holdCountdown.Dock = DockStyle.Top;
        _holdCountdown.Height = 22;
        _holdCountdown.ForeColor = ThemeHelper.Danger;
        _holdCountdown.TextAlign = ContentAlignment.MiddleLeft;

        _total.Dock = DockStyle.Top;
        _total.Height = 36;
        _total.Font = new Font("Segoe UI", 15, FontStyle.Bold);
        _total.ForeColor = ThemeHelper.Primary;
        _total.TextAlign = ContentAlignment.MiddleLeft;

        _confirm.Text = "XÁC NHẬN THANH TOÁN";
        _confirm.Dock = DockStyle.Bottom;
        _confirm.Height = 42;
        _confirm.BackColor = ThemeHelper.Accent;
        _confirm.ForeColor = ThemeHelper.Primary;
        _confirm.FlatStyle = FlatStyle.Flat;
        _confirm.UseVisualStyleBackColor = false;
        _confirm.FlatAppearance.BorderSize = 0;
        _confirm.Click += (_, _) => CompleteBooking();

        footer.Controls.Add(_confirm);
        footer.Controls.Add(_total);
        footer.Controls.Add(_holdCountdown);
        footer.Controls.Add(_discountLabel);
        footer.Controls.Add(promotionPanel);
        footer.Controls.Add(_payment);
        footer.Controls.Add(methodLabel);

        Controls.Add(_details);
        Controls.Add(footer);
        Controls.Add(title);
    }

    private void LoadSummary()
    {
        if (_order.Showtime == null || _order.Customer == null)
            throw new InvalidOperationException(
                "Thông tin đơn hàng chưa đầy đủ.");

        if (_order.SelectedSeatIds.Count == 0)
            throw new InvalidOperationException(
                "Đơn hàng chưa có ghế được chọn.");

        var selectedIds = _order.SelectedSeatIds.ToHashSet();

        var seats = _seatMap.GetSeatMapByShowtime(_order.Showtime.Id)
            .Where(seat => selectedIds.Contains(seat.Id))
            .ToList();

        if (seats.Count != selectedIds.Count)
            throw new InvalidOperationException(
                "Không thể tải đầy đủ thông tin ghế đã chọn.");

        _ticketTotal = seats.Sum(seat => seat.Price);

        // Đối chiếu lại giá sản phẩm với database tại thời điểm mở thanh toán.
        // Không dùng giá cũ được lưu từ lúc nhân viên thêm sản phẩm vào giỏ.
        _productPricesUpdated = false;

        foreach (var item in _order.Products)
        {
            if (item.ProductId <= 0 || item.Quantity <= 0)
            {
                throw new InvalidOperationException(
                    "Danh sách sản phẩm trong đơn hàng không hợp lệ.");
            }

            var product = _products.GetById(item.ProductId);

            if (product is null || !product.IsActive)
            {
                throw new InvalidOperationException(
                    $"Sản phẩm #{item.ProductId} không còn kinh doanh. " +
                    "Vui lòng quay lại chọn sản phẩm.");
            }

            if (product.Price < 0)
            {
                throw new InvalidOperationException(
                    $"Giá sản phẩm {product.Name} không hợp lệ.");
            }

            if (item.Price != product.Price)
            {
                item.Price = product.Price;
                _productPricesUpdated = true;
            }

            _order.ProductNames[item.ProductId] = product.Name;
        }

        _productTotal = _order.Products.Sum(
            item => item.Price * item.Quantity);

        _subtotal = _ticketTotal + _productTotal;

        _seatNames = string.Join(
            ", ",
            seats.Select(seat => $"{seat.RowLabel}{seat.ColNumber}"));

        _order.Discount = 0;
        _order.PromotionCode = string.Empty;
        _promotionCode.Clear();

        UpdateTotals();
    }

    private void RefreshDetails()
    {
        if (_order.Showtime == null || _order.Customer == null)
            return;

        var productLines = _order.Products.Select(item =>
        {
            var name = _order.ProductNames.GetValueOrDefault(
                item.ProductId,
                $"Sản phẩm #{item.ProductId}");

            return $"  - {name} x {item.Quantity}: " +
                   $"{item.Price * item.Quantity:N0} đ";
        }).ToList();

        var lines = new List<string>
        {
            "THÔNG TIN KHÁCH HÀNG",
            $"Khách hàng: {_order.Customer.FullName}",
            $"Điện thoại: {_order.Customer.Phone}",
            "",
            "THÔNG TIN SUẤT CHIẾU",
            $"Phim: {_order.Showtime.MovieTitle}",
            $"Phòng: {_order.Showtime.RoomName}",
            $"Suất chiếu: {_order.Showtime.StartTime:dd/MM/yyyy HH:mm}",
            $"Ghế: {_seatNames}",
            $"Tiền vé: {_ticketTotal:N0} đ",
            "",
            "SẢN PHẨM ĐI KÈM"
        };

        if (productLines.Count == 0)
            lines.Add("  Không có sản phẩm đi kèm.");
        else
            lines.AddRange(productLines);

        lines.AddRange([
            "",
            $"Tiền sản phẩm: {_productTotal:N0} đ",
            "--------------------------------------------------",
            $"Tạm tính: {_subtotal:N0} đ",
            $"Giảm giá: {_order.Discount:N0} đ",
            $"Tổng cộng: {Math.Max(0, _subtotal - _order.Discount):N0} đ",
            "",
            "Lưu ý: Phương thức thanh toán được ghi nhận trong hệ thống."
        ]);

        if (_productPricesUpdated)
        {
            lines.Add("");
            lines.Add(
                "Lưu ý: Giá một số sản phẩm đã được cập nhật theo giá hiện hành.");
        }

        _details.Text = string.Join(Environment.NewLine, lines);
    }

    private sealed record PaymentOption(string Label, PaymentMethod Value);

    private void ApplyPromotion()
    {
        try
        {
            var code = _promotionCode.Text.Trim();

            var promotion = _promotions.Validate(code, _subtotal);

            var discount = _promotions.ApplyDiscount(
                promotion.Code,
                _subtotal);

            _order.PromotionCode = promotion.Code;
            _order.Discount = Math.Min(
                _subtotal,
                Math.Max(0, discount));

            UpdateTotals();

            MessageHelper.Info(
                $"Đã áp dụng mã {promotion.Code}. " +
                $"Giảm {_order.Discount:N0} đ.");
        }
        catch (Exception ex)
        {
            _order.PromotionCode = string.Empty;
            _order.Discount = 0;

            UpdateTotals();
            MessageHelper.Handle(ex);
        }
    }

    private void ClearPromotion()
    {
        _promotionCode.Clear();
        _order.PromotionCode = string.Empty;
        _order.Discount = 0;

        UpdateTotals();
    }

    private void UpdateTotals()
    {
        _discountLabel.Text = _order.Discount > 0
            ? $"Đã giảm: {_order.Discount:N0} đ"
            : "Chưa áp dụng khuyến mãi";

        _total.Text =
            $"TỔNG THANH TOÁN: " +
            $"{Math.Max(0, _subtotal - _order.Discount):N0} đ";

        RefreshDetails();
    }

    private void CompleteBooking()
    {
        if (_order.Showtime == null
            || _order.Customer == null
            || _order.SelectedSeatIds.Count == 0)
        {
            MessageHelper.Warning(
                "Thông tin đơn hàng chưa đầy đủ. Vui lòng kiểm tra lại.");

            return;
        }

        var paymentMethod = _payment.SelectedItem is PaymentOption option
            ? option.Value
            : PaymentMethod.Cash;

        var totalAmount = Math.Max(
            0,
            _subtotal - _order.Discount);

        _paymentConfirmationOpen = true;
        var paymentResult = MessageHelper.Decide(
            $"MÔ PHỎNG THANH TOÁN\n\n" +
            $"Phương thức: {paymentMethod}\n" +
            $"Số tiền: {totalAmount:N0} VNĐ\n\n" +
            "Chọn kết quả giao dịch:\n\n" +
            "Chọn một kết quả để tiếp tục. Thanh toán đang được mô phỏng.",
            "Xác nhận kết quả thanh toán",
            MessageBoxIcon.Question,
            MessageBoxButtons.YesNoCancel);
        _paymentConfirmationOpen = false;

        if (_holdExpired)
        {
            MessageHelper.Warning(
                "Thời gian giữ ghế đã hết trong lúc xác nhận. Vui lòng bắt đầu lại và chọn ghế mới.");
            return;
        }

        if (paymentResult == DialogResult.Cancel)
            return;

        if (paymentResult == DialogResult.No)
        {
            MessageHelper.Warning(
                "Thanh toán thất bại!\n\n" +
                "Hệ thống chưa tạo hóa đơn và chưa phát hành vé.\n" +
                "Ghế giữ chỗ vẫn được giữ cho đến khi hết thời hạn.");

            return;
        }

        try
        {
            _confirm.Enabled = false;
            _confirm.Text = "ĐANG GHI NHẬN...";

            _order.BookingId = _bookings.CreateBooking(
                _order.Customer.Id,
                _order.Showtime.Id,
                _order.SelectedSeatIds,
                _order.Products,
                paymentMethod,
                _order.PromotionCode);

            _holdTimer.Stop();
            _order.HoldExpiresAt.Clear();

            MessageHelper.Info(
                "Thanh toán mô phỏng thành công!\n\n" +
                "Hóa đơn, vé và giao dịch thanh toán đã được ghi nhận.",
                "Thanh toán thành công");

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);

            _confirm.Enabled = true;
            _confirm.Text = "XÁC NHẬN THANH TOÁN";
        }
    }

    private void UpdateHoldCountdown()
    {
        if (_order.HoldExpiresAt.Count == 0)
        {
            _holdCountdown.Text = "";
            return;
        }

        var remaining = _order.HoldExpiresAt.Values.Min() - DateTime.Now;
        if (remaining <= TimeSpan.Zero)
        {
            _holdExpired = true;
            _holdTimer.Stop();
            _holdCountdown.Text = "Đã hết thời gian giữ ghế. Giao dịch không thể thanh toán.";
            _confirm.Enabled = false;

            if (!_paymentConfirmationOpen)
            {
                MessageHelper.Warning(
                    "Đã hết thời gian giữ ghế. Vui lòng đóng giao dịch và chọn ghế lại.");
            }

            return;
        }

        var totalSeconds = (int)Math.Ceiling(remaining.TotalSeconds);
        _holdCountdown.Text =
            $"Thời gian giữ ghế còn: {totalSeconds / 60:00}:{totalSeconds % 60:00}";
    }

    private static Button CreateButton(string text, Color color)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            Height = 32,
            BackColor = color,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(5, 0, 5, 0),
            Padding = new Padding(10, 2, 10, 2),
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };

        ThemeHelper.StyleActionButton(button, color);

        return button;
    }
}
