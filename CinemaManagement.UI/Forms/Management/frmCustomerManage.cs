
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Core.DTOS;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.UI.Controls;
using CinemaManagement.UI.Helpers;

namespace CinemaManagement.UI.Forms.Customers;

public partial class frmCustomerManage : Form
{
    private readonly ICustomerService _service;
    private readonly IAuditService _audit;

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

    private readonly TextBox _name = new() { Width = 190 };
    private readonly TextBox _phone = new() { Width = 145 };
    private readonly TextBox _email = new() { Width = 210 };

    private readonly TextBox _search = new()
    {
        Width = 240,
        PlaceholderText = "Tên hoặc số điện thoại...",
        AccessibleName = "Tìm khách hàng theo tên hoặc số điện thoại"
    };

    private readonly Label _status = new();
    private Button? _updateButton;
    private Button? _pointsButton;
    private Button? _historyButton;

    private int _selectedId;

    public frmCustomerManage(ICustomerService service, IAuditService audit)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));

        InitializeComponent();
        BuildLayout();
        RefreshGrid();
        ThemeHelper.Apply(this);
    }

    private void BuildLayout()
    {
        Text = "Quản lý khách hàng";
        Size = new Size(1100, 700);
        MinimumSize = new Size(900, 580);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = ThemeHelper.Background;
        Padding = new Padding(16);

        var title = new PageHeader(
            "Khách hàng",
            "Quản lý hồ sơ, thông tin liên hệ và lịch sử mua của khách hàng.");

        var editor = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 88,
            Padding = new Padding(8),
            ColumnCount = 6,
            RowCount = 2,
            BackColor = Color.White
        };

        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 9));
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24));
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 13));
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 19));
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 8));
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 27));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        editor.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        editor.Controls.Add(CreateLabel("Họ tên"), 0, 0);
        editor.Controls.Add(CreateLabel("Điện thoại"), 2, 0);
        editor.Controls.Add(CreateLabel("Email"), 4, 0);
        editor.Controls.Add(_name, 1, 1);
        editor.Controls.Add(_phone, 3, 1);
        editor.Controls.Add(_email, 5, 1);
        _name.Dock = _phone.Dock = _email.Dock = DockStyle.Fill;
        _name.Margin = _phone.Margin = _email.Margin = new Padding(2, 2, 8, 4);

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

        var add = CreateButton("Thêm khách hàng", ThemeHelper.Accent);
        _updateButton = CreateButton("Cập nhật", ThemeHelper.Primary);
        _pointsButton = CreateButton("Cộng 100 điểm", ThemeHelper.Success);
        _historyButton = CreateButton("Lịch sử mua", ThemeHelper.Primary);
        var reset = CreateButton("Làm mới", ThemeHelper.Muted);
        _updateButton.Enabled = false;
        _pointsButton.Enabled = false;
        _historyButton.Enabled = false;

        add.Click += (_, _) => AddCustomer();
        _updateButton.Click += (_, _) => UpdateCustomer();
        _pointsButton.Click += (_, _) => AddPoints();
        _historyButton.Click += (_, _) => ShowPurchaseHistory();
        reset.Click += (_, _) => ResetEditor();

        commandRow.Controls.AddRange(new Control[]
        {
            add, _updateButton, _pointsButton, _historyButton, reset
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
        _grid.EmptyStateText = "Chưa có khách hàng khớp với nội dung tìm kiếm.";

        _grid.CellClick += (_, e) => SelectCustomer(e.RowIndex);

        _search.TextChanged += (_, _) =>
        {
            RefreshGrid(_search.Text);
        };

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

        AddColumn("Id", "Mã", 8);
        AddColumn("FullName", "Họ và tên", 23);
        AddColumn("Phone", "Số điện thoại", 18);
        AddColumn("Email", "Email", 25);
        AddColumn("Points", "Điểm", 10);
        AddColumn("Tier", "Hạng thành viên", 16);

        ThemeHelper.ApplyDefaultGridStyle(_grid);
    }

    private void AddColumn(string property, string title, float weight)
    {
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = property,
            HeaderText = title,
            Name = property,
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

    private Customer ReadCustomer()
    {
        return new Customer
        {
            FullName = _name.Text.Trim(),
            Phone = _phone.Text.Trim(),
            Email = _email.Text.Trim()
        };
    }

    private void AddCustomer()
    {
        Execute(() =>
        {
            _service.Create(ReadCustomer());
            ResetEditor();
            MessageHelper.Info("Đã thêm khách hàng.");
        });
    }

    private void UpdateCustomer()
    {
        Execute(() =>
        {
            EnsureCustomerSelected();

            var customer = ReadCustomer();
            customer.Id = _selectedId;

            _service.Update(customer);

            MessageHelper.Info("Đã cập nhật khách hàng.");
        });
    }

    private void AddPoints()
    {
        Execute(() =>
        {
            EnsureCustomerSelected();

            if (!MessageHelper.Confirm(
                "Bạn có muốn cộng 100 điểm cho khách hàng này?"))
            {
                return;
            }

            _service.AddPointsAndUpgradeTier(_selectedId, 100);

            try
            {
                _audit.Log(
                    "CUSTOMER_POINTS_ADJUSTMENT",
                    "Customers",
                    _selectedId,
                    "Quản trị viên cộng 100 điểm thủ công.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(
                    "Points for customer {0} were saved, but audit logging failed: {1}",
                    _selectedId,
                    ex);
            }

            MessageHelper.Info("Đã cộng 100 điểm và cập nhật hạng thành viên.");
        });
    }

    private void SelectCustomer(int rowIndex)
    {
        if (rowIndex < 0 ||
            _grid.Rows[rowIndex].DataBoundItem is not Customer customer)
        {
            return;
        }

        _selectedId = customer.Id;
        _updateButton!.Enabled = true;
        _pointsButton!.Enabled = true;
        _historyButton!.Enabled = true;
        _name.Text = customer.FullName;
        _phone.Text = customer.Phone;
        _email.Text = customer.Email;

        _status.Text =
            $"Đang chọn: {customer.FullName}  |  {customer.Points:N0} điểm  |  {customer.Tier}";
    }

    private void ShowPurchaseHistory()
    {
        if (_selectedId <= 0)
        {
            MessageHelper.Warning("Vui lòng chọn khách hàng trước.");
            return;
        }

        try
        {
            var history = _service.GetPurchaseHistory(_selectedId).ToList();

            using var form = new Form
            {
                Text = "Lịch sử mua hàng",
                Size = new Size(850, 480),
                MinimumSize = new Size(650, 360),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = ThemeHelper.Background,
                Padding = new Padding(16),
                Font = ThemeHelper.BodyFont,
                ShowInTaskbar = false
            };

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = ThemeHelper.Background
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.Controls.Add(new PageHeader(
                "Lịch sử mua hàng",
                "Các hóa đơn, vé và tổng tiền đã ghi nhận cho khách hàng."), 0, 0);

            var grid = new EmptyStateDataGridView
            {
                EmptyStateText = "Khách hàng chưa có giao dịch mua hàng.",
                AccessibleName = "Lịch sử mua hàng của khách hàng",
                AccessibleDescription = "Danh sách hóa đơn và số vé đã mua.",
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(CustomerPurchaseHistoryDto.BookingCode),
                HeaderText = "Mã hóa đơn",
                FillWeight = 20
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(CustomerPurchaseHistoryDto.CreatedAt),
                HeaderText = "Ngày mua",
                FillWeight = 22,
                DefaultCellStyle = { Format = "dd/MM/yyyy HH:mm" }
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(CustomerPurchaseHistoryDto.TicketCount),
                HeaderText = "Số vé",
                FillWeight = 12
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(CustomerPurchaseHistoryDto.Total),
                HeaderText = "Tổng tiền",
                FillWeight = 20,
                DefaultCellStyle = { Format = "N0" }
            });

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(CustomerPurchaseHistoryDto.Status),
                HeaderText = "Trạng thái",
                FillWeight = 16
            });

            grid.DataSource = history;
            ThemeHelper.ApplyDefaultGridStyle(grid);
            root.Controls.Add(grid, 0, 1);
            form.Controls.Add(root);

            form.ShowDialog(this);
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);
        }
    }

    private void EnsureCustomerSelected()
    {
        if (_selectedId <= 0)
            throw new InvalidOperationException("Vui lòng chọn khách hàng trước.");
    }

    private void RefreshGrid(string? keyword = null)
    {
        try
        {
            var customers = string.IsNullOrWhiteSpace(keyword)
                ? _service.GetAll().ToList()
                : _service.Search(keyword.Trim()).ToList();

            _grid.DataSource = customers;
            _status.Text = customers.Count == 0
                ? "Không tìm thấy khách hàng phù hợp."
                : $"Đang hiển thị {customers.Count:N0} khách hàng";
        }
        catch (Exception ex)
        {
            _status.Text = "Không thể tải danh sách khách hàng.";
            MessageHelper.Handle(ex, "Không thể tải danh sách khách hàng.");
        }
    }

    private void ResetEditor()
    {
        _selectedId = 0;
        _updateButton!.Enabled = false;
        _pointsButton!.Enabled = false;
        _historyButton!.Enabled = false;
        _name.Clear();
        _phone.Clear();
        _email.Clear();
        _grid.ClearSelection();

        RefreshGrid(_search.Text);
    }

    private void Execute(Action action)
    {
        try
        {
            action();
            RefreshGrid(_search.Text);
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);
        }
    }
}
