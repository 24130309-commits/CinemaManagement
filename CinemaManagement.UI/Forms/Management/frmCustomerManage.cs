using System;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.UI.Helpers;

namespace CinemaManagement.UI.Forms.Customers;

public partial class frmCustomerManage : Form
{
    private readonly ICustomerService _service;

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

    private readonly TextBox _phone = new()
    {
        Width = 130
    };

    private readonly TextBox _email = new()
    {
        Width = 180
    };

    private readonly TextBox _search = new()
    {
        Width = 180,
        PlaceholderText = "Tên hoặc số điện thoại"
    };

    private int _selectedId;

    public frmCustomerManage(ICustomerService service)
    {
        _service = service;

        InitializeComponent();

        BuildLayout();
        RefreshGrid();
    }

    private void BuildLayout()
    {
        Text = "Quản lý khách hàng";
        Width = 980;
        Height = 600;
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
                Text = "Họ tên:",
                AutoSize = true,
                Padding = new Padding(0, 8, 0, 0)
            },
            _name,

            new Label
            {
                Text = "Điện thoại:",
                AutoSize = true,
                Padding = new Padding(0, 8, 0, 0)
            },
            _phone,

            new Label
            {
                Text = "Email:",
                AutoSize = true,
                Padding = new Padding(0, 8, 0, 0)
            },
            _email
        });

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 48,
            Padding = new Padding(6),
            WrapContents = false
        };

        var add = new Button { Text = "Thêm" };
        var update = new Button { Text = "Cập nhật" };
        var points = new Button { Text = "Cộng 100 điểm" };
        var history = new Button { Text = "Lịch sử mua" };
        var refresh = new Button { Text = "Làm mới" };

        actions.Controls.AddRange(new Control[]
        {
            add,
            update,
            points,
            history,
            refresh,

            new Label
            {
                Text = "Tìm:",
                AutoSize = true,
                Padding = new Padding(4, 8, 0, 0)
            },
            _search
        });

        add.Click += (_, _) => Execute(() =>
        {
            _service.Create(ReadCustomer());
            ResetEditor();
        });

        update.Click += (_, _) => Execute(() =>
        {
            var customer = ReadCustomer();
            customer.Id = _selectedId;

            _service.Update(customer);
        });

        points.Click += (_, _) => Execute(() =>
        {
            EnsureCustomerSelected();

            _service.AddPointsAndUpgradeTier(
                _selectedId,
                100);
        });

        history.Click += (_, _) => ShowPurchaseHistory();

        refresh.Click += (_, _) =>
        {
            _search.Clear();
            RefreshGrid();
        };

        _search.TextChanged += (_, _) =>
        {
            _grid.DataSource =
                _service.Search(_search.Text).ToList();
        };

        _grid.CellClick += (_, e) =>
            SelectCustomer(e.RowIndex);

        Controls.Add(_grid);
        Controls.Add(actions);
        Controls.Add(editor);
    }

    private Customer ReadCustomer()
    {
        return new Customer
        {
            FullName = _name.Text.Trim(),
            Phone = _phone.Text.Trim(),
            Email = _email.Text.Trim()
        };
    }

    private void SelectCustomer(int rowIndex)
    {
        if (rowIndex < 0 ||
            _grid.Rows[rowIndex].DataBoundItem is not Customer customer)
        {
            return;
        }

        _selectedId = customer.Id;
        _name.Text = customer.FullName;
        _phone.Text = customer.Phone;
        _email.Text = customer.Email;
    }

    private void ShowPurchaseHistory()
    {
        if (_selectedId <= 0)
        {
            MessageHelper.Warning(
                "Vui lòng chọn khách hàng trước.");

            return;
        }

        var history =
            _service.GetPurchaseHistory(_selectedId).ToList();

        using var form = new Form
        {
            Text = "Lịch sử mua hàng",
            Width = 760,
            Height = 420,
            StartPosition = FormStartPosition.CenterParent
        };

        form.Controls.Add(new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AutoSizeColumnsMode =
                DataGridViewAutoSizeColumnsMode.Fill,
            DataSource = history
        });

        form.ShowDialog(this);
    }

    private void EnsureCustomerSelected()
    {
        if (_selectedId <= 0)
        {
            throw new InvalidOperationException(
                "Vui lòng chọn khách hàng trước.");
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
        _phone.Clear();
        _email.Clear();

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