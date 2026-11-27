
using System.Drawing;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.UI.Controls;
using CinemaManagement.UI.Helpers;

namespace CinemaManagement.UI.Forms.Sales;

public partial class frmComboSelection : Form
{
    private readonly IProductService _products;
    private readonly SalesOrderContext _order;
    private readonly ComboBox _productPicker = new();
    private readonly NumericUpDown _quantity = new();
    private readonly EmptyStateDataGridView _cart = new();
    private readonly Label _total = new();
    private readonly Label _holdCountdown = new();
    private readonly System.Windows.Forms.Timer _holdTimer = new()
    {
        Interval = 1000
    };
    private List<Product> _availableProducts = new();

    public frmComboSelection(IProductService products, SalesOrderContext order)
    {
        InitializeComponent();
        _products = products;
        _order = order;
        BuildUi();
        LoadProducts();
        RefreshCart();
        _holdTimer.Tick += (_, _) => UpdateHoldCountdown();
        _holdTimer.Start();
        Shown += (_, _) => UpdateHoldCountdown();
        FormClosed += (_, _) =>
        {
            _holdTimer.Stop();
            _holdTimer.Dispose();
        };
        ThemeHelper.Apply(this);
        ThemeHelper.FitToWorkingArea(this, new Size(900, 620), new Size(760, 520));
    }

    private void BuildUi()
    {
        Text = "Bán vé - Bắp nước và sản phẩm";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(900, 620);
        MinimumSize = new Size(760, 520);
        BackColor = ThemeHelper.Background;
        Font = ThemeHelper.BodyFont;
        Padding = new Padding(18);

        var title = new PageHeader(
            "Bắp nước & sản phẩm",
            "Thêm món đi kèm vào đơn vé; kiểm tra số lượng và giá trước khi tiếp tục.");

        var addPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 66,
            WrapContents = false,
            Padding = new Padding(8, 12, 8, 8),
            BackColor = Color.White
        };

        _productPicker.Width = 390;
        _productPicker.DropDownStyle = ComboBoxStyle.DropDownList;
        _productPicker.Font = ThemeHelper.BodyFont;

        _quantity.Width = 82;
        _quantity.Minimum = 1;
        _quantity.Maximum = 50;
        _quantity.Value = 1;
        _quantity.Font = ThemeHelper.BodyFont;

        var add = CreateButton("Thêm sản phẩm", ThemeHelper.Accent);
        add.Click += (_, _) => AddProduct();

        addPanel.Controls.AddRange([_productPicker, _quantity, add]);

        _cart.Dock = DockStyle.Fill;
        _cart.EmptyStateText = "Chưa có sản phẩm trong đơn hàng.";
        _cart.ReadOnly = true;
        _cart.AllowUserToAddRows = false;
        _cart.AllowUserToDeleteRows = false;
        _cart.RowHeadersVisible = false;
        _cart.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _cart.MultiSelect = false;
        _cart.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        _cart.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Product",
            HeaderText = "Sản phẩm",
            FillWeight = 40
        });

        _cart.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Quantity",
            HeaderText = "Số lượng",
            FillWeight = 14
        });

        _cart.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Price",
            HeaderText = "Đơn giá",
            FillWeight = 20
        });

        _cart.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Total",
            HeaderText = "Thành tiền",
            FillWeight = 26
        });

        ThemeHelper.ApplyDefaultGridStyle(_cart);

        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 76,
            BackColor = Color.White,
            Padding = new Padding(12)
        };

        _total.Dock = DockStyle.Left;
        _total.Width = 350;
        _total.Font = ThemeHelper.SectionFont;
        _total.ForeColor = ThemeHelper.Primary;
        _total.TextAlign = ContentAlignment.MiddleLeft;

        _holdCountdown.Dock = DockStyle.Bottom;
        _holdCountdown.Height = 22;
        _holdCountdown.ForeColor = ThemeHelper.Danger;
        _holdCountdown.TextAlign = ContentAlignment.MiddleLeft;

        var remove = CreateButton("Xóa sản phẩm", ThemeHelper.Danger);
        remove.Dock = DockStyle.Right;
        remove.Click += (_, _) => RemoveProduct();

        var continueButton = CreateButton("Tiếp tục thanh toán  →", ThemeHelper.Accent);
        continueButton.Dock = DockStyle.Right;
        continueButton.Click += (_, _) => ContinueCheckout();

        footer.Controls.Add(continueButton);
        footer.Controls.Add(remove);
        footer.Controls.Add(_total);
        footer.Controls.Add(_holdCountdown);

        Controls.Add(_cart);
        Controls.Add(footer);
        Controls.Add(addPanel);
        Controls.Add(title);
    }

    private void LoadProducts()
    {
        try
        {
            _availableProducts = _products.GetActive()
                .Where(product => product.Price >= 0)
                .OrderBy(product => product.Category)
                .ThenBy(product => product.Name)
                .ToList();

            foreach (var product in _availableProducts)
                _order.ProductNames[product.Id] = product.Name;

            _productPicker.DataSource = _availableProducts;
            _productPicker.DisplayMember = nameof(Product.Name);
            _productPicker.ValueMember = nameof(Product.Id);
            _productPicker.Enabled = _availableProducts.Count > 0;
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);
        }
    }

    private void AddProduct()
    {
        if (_productPicker.SelectedItem is not Product product)
        {
            MessageHelper.Warning("Hiện không có sản phẩm đang kinh doanh.");
            return;
        }

        var quantity = (int)_quantity.Value;
        var existing = _order.Products.FirstOrDefault(item => item.ProductId == product.Id);

        if (existing == null)
        {
            _order.Products.Add(new BookingProduct
            {
                ProductId = product.Id,
                Quantity = quantity,
                Price = product.Price
            });
        }
        else
        {
            if (existing.Quantity + quantity > 999)
            {
                MessageHelper.Warning("Số lượng sản phẩm không được vượt quá 999.");
                return;
            }

            existing.Quantity += quantity;
            existing.Price = product.Price;
        }

        RefreshCart();
    }

    private void RemoveProduct()
    {
        if (_cart.CurrentRow?.Tag is not int productId)
        {
            MessageHelper.Warning("Vui lòng chọn sản phẩm cần xóa.");
            return;
        }

        _order.Products.RemoveAll(item => item.ProductId == productId);
        RefreshCart();
    }

    private void RefreshCart()
    {
        _cart.Rows.Clear();

        foreach (var item in _order.Products)
        {
            var product = _availableProducts.FirstOrDefault(p => p.Id == item.ProductId);
            var name = product?.Name
                ?? _order.ProductNames.GetValueOrDefault(
                    item.ProductId,
                    $"Sản phẩm #{item.ProductId}");

            var row = _cart.Rows.Add(
                name,
                item.Quantity,
                $"{item.Price:N0} đ",
                $"{item.Price * item.Quantity:N0} đ");

            _cart.Rows[row].Tag = item.ProductId;
        }

        _total.Text = $"TỔNG TIỀN SẢN PHẨM: " +
                      $"{_order.Products.Sum(item => item.Price * item.Quantity):N0} đ";
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
            _holdTimer.Stop();
            _holdCountdown.Text = "Ghế đã hết thời gian giữ";
            MessageHelper.Warning(
                "Đã hết thời gian giữ ghế. Giao dịch sẽ kết thúc và cần chọn ghế lại.");
            DialogResult = DialogResult.Cancel;
            Close();
            return;
        }

        var totalSeconds = (int)Math.Ceiling(remaining.TotalSeconds);
        _holdCountdown.Text =
            $"Giữ ghế còn: {totalSeconds / 60:00}:{totalSeconds % 60:00}";
    }

    private void ContinueCheckout()
    {
        DialogResult = DialogResult.OK;
        Close();
    }

    private static Button CreateButton(string text, Color color)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            Height = 36,
            BackColor = color,
            ForeColor = color == ThemeHelper.Accent
                ? ThemeHelper.Primary
                : Color.White,
            FlatStyle = FlatStyle.Flat,
            Padding = new Padding(12, 3, 12, 3),
            Margin = new Padding(7, 0, 7, 0),
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };

        ThemeHelper.StyleActionButton(button, color);
        return button;
    }
}
