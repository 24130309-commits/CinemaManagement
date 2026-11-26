
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.UI.Controls;
using CinemaManagement.UI.Helpers;

namespace CinemaManagement.UI.Forms.Products;

public partial class frmProductManage : Form
{
    private readonly IProductService _service;
    private List<Product> _products = new();

    private readonly EmptyStateDataGridView _grid = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        AutoGenerateColumns = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false
    };

    private readonly TextBox _name = new() { Width = 210 };
    private readonly TextBox _category = new() { Width = 150 };

    private readonly NumericUpDown _price = new()
    {
        Width = 150,
        Minimum = 0,
        Maximum = 10000000,
        Increment = 1000,
        ThousandsSeparator = true
    };

    private readonly CheckBox _active = new()
    {
        Text = "Đang kinh doanh",
        Checked = true,
        AutoSize = true
    };

    private readonly TextBox _search = new()
    {
        Width = 230,
        PlaceholderText = "Tìm tên hoặc danh mục...",
        AccessibleName = "Tìm sản phẩm theo tên hoặc danh mục"
    };

    private readonly Label _status = new();
    private Button? _updateButton;
    private Button? _deleteButton;

    private int _selectedId;

    public frmProductManage(IProductService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));

        InitializeComponent();
        BuildLayout();
        LoadProducts();
        ThemeHelper.Apply(this);
    }

    private void BuildLayout()
    {
        Text = "Quản lý sản phẩm";
        Size = new Size(1050, 680);
        MinimumSize = new Size(850, 560);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = ThemeHelper.Background;
        Padding = new Padding(16);

        var title = new PageHeader(
            "Bắp nước & sản phẩm",
            "Quản lý danh mục bán kèm và trạng thái kinh doanh tại quầy.");

        var editor = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 104,
            Padding = new Padding(12, 8, 12, 8),
            ColumnCount = 4,
            RowCount = 1,
            BackColor = Color.White
        };

        for (var column = 0; column < 4; column++)
            editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

        editor.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        editor.Controls.Add(CreateEditorField("Tên sản phẩm", _name), 0, 0);
        editor.Controls.Add(CreateEditorField("Danh mục", _category), 1, 0);
        editor.Controls.Add(CreateEditorField("Giá bán (VNĐ)", _price), 2, 0);
        editor.Controls.Add(CreateEditorField("Trạng thái", _active), 3, 0);

        var actions = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 80,
            Padding = new Padding(7, 3, 7, 3),
            ColumnCount = 1,
            RowCount = 2,
            BackColor = ThemeHelper.Background
        };
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        actions.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        actions.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var commandRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            AutoScroll = true,
            Margin = Padding.Empty
        };

        var add = CreateButton("Thêm sản phẩm", ThemeHelper.Accent);
        _updateButton = CreateButton("Cập nhật", ThemeHelper.Primary);
        _deleteButton = CreateButton("Xóa", ThemeHelper.Danger);
        var reset = CreateButton("Làm mới", ThemeHelper.Muted);
        _updateButton.Enabled = false;
        _deleteButton.Enabled = false;

        add.Click += (_, _) => AddProduct();
        _updateButton.Click += (_, _) => UpdateProduct();
        _deleteButton.Click += (_, _) => DeleteProduct();
        reset.Click += (_, _) => ResetEditor();

        commandRow.Controls.AddRange(new Control[]
        {
            add, _updateButton, _deleteButton, reset
        });

        var searchRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Margin = Padding.Empty
        };
        searchRow.Controls.Add(_search);
        searchRow.Controls.Add(CreateLabel("Tìm kiếm"));
        actions.Controls.Add(commandRow, 0, 0);
        actions.Controls.Add(searchRow, 0, 1);

        ConfigureGrid();
        _grid.EmptyStateText = "Chưa có sản phẩm khớp với bộ lọc hiện tại.";

        _grid.CellClick += (_, e) => SelectProduct(e.RowIndex);
        _search.TextChanged += (_, _) => ApplySearch();

        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 38,
            Padding = new Padding(5),
            BackColor = ThemeHelper.Background
        };

        _status.Dock = DockStyle.Fill;
        _status.TextAlign = ContentAlignment.MiddleLeft;
        _status.ForeColor = ThemeHelper.Muted;
        footer.Controls.Add(_status);

        Controls.Add(_grid);
        Controls.Add(footer);
        Controls.Add(actions);
        Controls.Add(editor);
        Controls.Add(title);
    }

    private void ConfigureGrid()
    {
        _grid.Columns.Clear();

        AddColumn(nameof(Product.Id), "Mã", 8);
        AddColumn(nameof(Product.Name), "Tên sản phẩm", 28);
        AddColumn(nameof(Product.Category), "Danh mục", 20);

        var priceColumn = new DataGridViewTextBoxColumn
        {
            Name = nameof(Product.Price),
            DataPropertyName = nameof(Product.Price),
            HeaderText = "Giá bán",
            FillWeight = 18,
            DefaultCellStyle = { Format = "N0" }
        };

        _grid.Columns.Add(priceColumn);

        _grid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            Name = nameof(Product.IsActive),
            DataPropertyName = nameof(Product.IsActive),
            HeaderText = "Đang bán",
            FillWeight = 12
        });

        ThemeHelper.ApplyDefaultGridStyle(_grid);
    }

    private void AddColumn(string property, string title, float weight)
    {
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = property,
            DataPropertyName = property,
            HeaderText = title,
            FillWeight = weight
        });
    }

    private static Label CreateLabel(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            Padding = new Padding(0, 9, 0, 0),
            ForeColor = ThemeHelper.Text
        };
    }

    private static Panel CreateEditorField(string caption, Control input)
    {
        var field = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ThemeHelper.Surface,
            Padding = new Padding(3),
            Margin = new Padding(4, 0, 8, 0)
        };

        var label = new Label
        {
            Text = caption,
            Dock = DockStyle.Top,
            Height = 25,
            Font = ThemeHelper.SectionFont,
            ForeColor = ThemeHelper.Text,
            TextAlign = ContentAlignment.MiddleLeft
        };

        input.Dock = DockStyle.Bottom;
        input.Height = 32;
        input.Margin = new Padding(0);

        field.Controls.Add(input);
        field.Controls.Add(label);
        return field;
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
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
            Margin = new Padding(4, 0, 4, 0)
        };

        ThemeHelper.StyleActionButton(button, color);
        return button;
    }

    private Product ReadProduct()
    {
        return new Product
        {
            Name = _name.Text.Trim(),
            Category = _category.Text.Trim(),
            Price = _price.Value,
            IsActive = _active.Checked
        };
    }

    private void AddProduct()
    {
        Execute(() =>
        {
            _service.Create(ReadProduct());
            ResetEditor();
            MessageHelper.Info("Đã thêm sản phẩm.");
        });
    }

    private void UpdateProduct()
    {
        Execute(() =>
        {
            EnsureProductSelected();

            var product = ReadProduct();
            product.Id = _selectedId;

            _service.Update(product);

            MessageHelper.Info("Đã cập nhật sản phẩm.");
        });
    }

    private void DeleteProduct()
    {
        Execute(() =>
        {
            EnsureProductSelected();

            if (!MessageHelper.Confirm(
                "Bạn có chắc chắn muốn xóa sản phẩm này?"))
            {
                return;
            }

            _service.Delete(_selectedId);
            ResetEditor();

            MessageHelper.Info("Đã xóa sản phẩm.");
        });
    }

    private void SelectProduct(int rowIndex)
    {
        if (rowIndex < 0 ||
            _grid.Rows[rowIndex].DataBoundItem is not Product product)
        {
            return;
        }

        _selectedId = product.Id;
        _updateButton!.Enabled = true;
        _deleteButton!.Enabled = true;
        _name.Text = product.Name;
        _category.Text = product.Category;
        _price.Value = Math.Min(_price.Maximum, product.Price);
        _active.Checked = product.IsActive;

        _status.Text =
            $"Đang chọn: {product.Name}  |  {product.Price:N0} đ";
    }

    private void EnsureProductSelected()
    {
        if (_selectedId <= 0)
            throw new InvalidOperationException("Vui lòng chọn sản phẩm trước.");
    }

    private void LoadProducts()
    {
        try
        {
            _products = _service.GetAll()
                .OrderBy(product => product.Name)
                .ToList();

            ApplySearch();
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);
        }
    }

    private void ApplySearch()
    {
        var keyword = _search.Text.Trim();

        var filtered = string.IsNullOrWhiteSpace(keyword)
            ? _products
            : _products.Where(product =>
                product.Name.Contains(
                    keyword,
                    StringComparison.OrdinalIgnoreCase)
                || product.Category.Contains(
                    keyword,
                    StringComparison.OrdinalIgnoreCase))
                .ToList();

        _grid.DataSource = filtered.ToList();
        _status.Text = $"Đang hiển thị {filtered.Count:N0}/{_products.Count:N0} sản phẩm";
    }

    private void ResetEditor()
    {
        _selectedId = 0;
        _updateButton!.Enabled = false;
        _deleteButton!.Enabled = false;
        _name.Clear();
        _category.Clear();
        _price.Value = 0;
        _active.Checked = true;
        _grid.ClearSelection();

        LoadProducts();
    }

    private void Execute(Action action)
    {
        try
        {
            action();
            LoadProducts();
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);
        }
    }
}
