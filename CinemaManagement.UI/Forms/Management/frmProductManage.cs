using System;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.UI.Helpers;

namespace CinemaManagement.UI.Forms.Products;

public partial class frmProductManage : Form
{
    private readonly IProductService _service;

    private readonly DataGridView _grid = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
    };

    private readonly TextBox _name = new()
    {
        Width = 180
    };

    private readonly TextBox _category = new()
    {
        Width = 120
    };

    private readonly NumericUpDown _price = new()
    {
        Width = 130,
        Maximum = 10000000,
        ThousandsSeparator = true
    };

    private readonly CheckBox _active = new()
    {
        Text = "Đang bán",
        Checked = true,
        AutoSize = true
    };

    private int _selectedId;

    public frmProductManage(IProductService service)
    {
        _service = service;

        InitializeComponent();

        BuildLayout();
        RefreshGrid();
    }

    private void BuildLayout()
    {
        Text = "Quản lý sản phẩm / bắp nước";
        Width = 940;
        Height = 580;
        StartPosition = FormStartPosition.CenterParent;

        var editor = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 55,
            Padding = new Padding(8),
            WrapContents = false
        };

        editor.Controls.AddRange(new Control[]
        {
            new Label
            {
                Text = "Tên sản phẩm:",
                AutoSize = true,
                Padding = new Padding(0, 8, 0, 0)
            },
            _name,

            new Label
            {
                Text = "Danh mục:",
                AutoSize = true,
                Padding = new Padding(0, 8, 0, 0)
            },
            _category,

            new Label
            {
                Text = "Giá:",
                AutoSize = true,
                Padding = new Padding(0, 8, 0, 0)
            },
            _price,

            _active
        });

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 45,
            Padding = new Padding(6)
        };

        var add = new Button { Text = "Thêm" };
        var update = new Button { Text = "Cập nhật" };
        var delete = new Button { Text = "Xóa" };
        var reset = new Button { Text = "Làm mới" };

        actions.Controls.AddRange(new Control[]
        {
            add,
            update,
            delete,
            reset
        });

        add.Click += (_, _) => Execute(() =>
        {
            _service.Create(ReadProduct());
            ResetEditor();
        });

        update.Click += (_, _) => Execute(() =>
        {
            EnsureProductSelected();

            var product = ReadProduct();
            product.Id = _selectedId;

            _service.Update(product);
        });

        delete.Click += (_, _) => Execute(() =>
        {
            EnsureProductSelected();

            if (MessageHelper.Confirm(
                "Bạn có chắc chắn muốn xóa sản phẩm đã chọn?"))
            {
                _service.Delete(_selectedId);
                ResetEditor();
            }
        });

        reset.Click += (_, _) => ResetEditor();

        _grid.CellClick += (_, e) =>
            SelectProduct(e.RowIndex);

        Controls.Add(_grid);
        Controls.Add(actions);
        Controls.Add(editor);
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

    private void SelectProduct(int rowIndex)
    {
        if (rowIndex < 0 ||
            _grid.Rows[rowIndex].DataBoundItem is not Product product)
        {
            return;
        }

        _selectedId = product.Id;

        _name.Text = product.Name;
        _category.Text = product.Category;
        _price.Value = Math.Min(_price.Maximum, product.Price);
        _active.Checked = product.IsActive;
    }

    private void EnsureProductSelected()
    {
        if (_selectedId <= 0)
        {
            throw new InvalidOperationException(
                "Vui lòng chọn sản phẩm trước.");
        }
    }

    private void RefreshGrid()
    {
        _grid.DataSource = _service.GetAll().ToList();
    }

    private void ResetEditor()
    {
        _selectedId = 0;

        _name.Clear();
        _category.Clear();
        _price.Value = 0;
        _active.Checked = true;

        RefreshGrid();
    }

    private void Execute(Action action)
    {
        try
        {
            action();
            RefreshGrid();
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);
        }
    }
}