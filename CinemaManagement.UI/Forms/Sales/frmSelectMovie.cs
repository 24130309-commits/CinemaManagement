
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.UI.Controls;
using CinemaManagement.UI.Helpers;

namespace CinemaManagement.UI.Forms.Sales;

public partial class frmSelectMovie : Form
{
    private readonly IShowtimesService _showtimes;
    private readonly ICustomerService _customers;
    private readonly ISeatHoldService _holds;
    private readonly FormFactory _forms;
    private readonly SalesOrderContext _order;

    private readonly EmptyStateDataGridView _grid = new();
    private readonly TextBox _phone = new();
    private readonly TextBox _fullName = new();
    private readonly TextBox _email = new();
    private readonly Label _customerStatus = new();

    // Bộ lọc suất chiếu
    private readonly TextBox _txtShowtimeSearch = new();
    private readonly CheckBox _chkDateFilter = new();
    private readonly DateTimePicker _dtpShowtimeDate = new();

    // Lưu danh sách suất chiếu để lọc mà không phải truy vấn lại
    private List<Showtime> _allShowtimes = new();

    public frmSelectMovie(
        IShowtimesService showtimes,
        ICustomerService customers,
        ISeatHoldService holds,
        FormFactory forms,
        SalesOrderContext order)
    {
        InitializeComponent();

        _showtimes = showtimes;
        _customers = customers;
        _holds = holds;
        _forms = forms;
        _order = order;

        BuildUi();
        LoadShowtimes();
        ThemeHelper.Apply(this);
        ThemeHelper.FitToWorkingArea(this, new Size(1100, 720), new Size(800, 520));
    }

    private void BuildUi()
    {
        Text = "Bán vé - Chọn suất chiếu";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(1100, 720);
        MinimumSize = new Size(800, 520);
        BackColor = ThemeHelper.Background;
        Font = ThemeHelper.BodyFont;
        Padding = new Padding(18);

        var heading = new PageHeader(
            "Bán vé",
            "Chọn suất chiếu, xác nhận khách hàng và tiếp tục đến sơ đồ ghế.");

        ConfigureGrid();

        // Khu vực tìm kiếm và lọc suất chiếu
        var filterPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 58,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoScroll = true,
            Padding = new Padding(0, 6, 0, 4),
            BackColor = ThemeHelper.Background
        };

        _txtShowtimeSearch.Width = 280;
        _txtShowtimeSearch.Height = 30;
        _txtShowtimeSearch.PlaceholderText =
            "Tìm theo tên phim hoặc phòng...";
        _txtShowtimeSearch.Margin = new Padding(0, 0, 12, 0);

        _txtShowtimeSearch.TextChanged +=
            (_, _) => ApplyShowtimeFilters();

        _chkDateFilter.Text = "Lọc theo ngày";
        _chkDateFilter.AutoSize = true;
        _chkDateFilter.Margin = new Padding(4, 7, 8, 0);

        _chkDateFilter.CheckedChanged += (_, _) =>
        {
            _dtpShowtimeDate.Enabled = _chkDateFilter.Checked;
            ApplyShowtimeFilters();
        };

        _dtpShowtimeDate.Format = DateTimePickerFormat.Custom;
        _dtpShowtimeDate.CustomFormat = "dd/MM/yyyy";
        _dtpShowtimeDate.Width = 130;
        _dtpShowtimeDate.Enabled = false;
        _dtpShowtimeDate.Margin = new Padding(0, 0, 8, 0);

        _dtpShowtimeDate.ValueChanged +=
            (_, _) => ApplyShowtimeFilters();

        filterPanel.Controls.AddRange(new Control[]
        {
            _txtShowtimeSearch,
            _chkDateFilter,
            _dtpShowtimeDate
        });

        // Thông tin khách hàng
        var customerPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 205,
            BackColor = Color.White,
            Padding = new Padding(16)
        };

        var customerTitle = new Label
        {
            Text = "THÔNG TIN KHÁCH HÀNG",
            Dock = DockStyle.Top,
            Height = 28,
            Font = ThemeHelper.SectionFont,
            ForeColor = ThemeHelper.Primary
        };

        var fields = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 76,
            ColumnCount = 3,
            RowCount = 2
        };

        fields.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 34));

        fields.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 33));

        fields.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 33));

        fields.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 24));

        fields.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));

        AddField(fields, "Số điện thoại", _phone, 0);
        AddField(fields, "Họ và tên", _fullName, 1);
        AddField(fields, "Email", _email, 2);

        _phone.PlaceholderText = "Nhập số điện thoại";
        _fullName.PlaceholderText = "Nhập họ tên khách hàng";
        _email.PlaceholderText = "Không bắt buộc";

        // Các nút thao tác
        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 72,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoScroll = true
        };

        var search = CreateButton(
            "Tìm khách hàng",
            ThemeHelper.Primary);

        search.Click += (_, _) => FindCustomer();

        var create = CreateButton(
            "Tạo khách hàng",
            ThemeHelper.Success);

        create.Click += (_, _) => CreateCustomer();

        var refresh = CreateButton(
            "Làm mới suất chiếu",
            ThemeHelper.Muted);

        refresh.Click += (_, _) => LoadShowtimes();

        var next = CreateButton(
            "Tiếp tục chọn ghế  →",
            ThemeHelper.Accent);

        next.Click += (_, _) => StartSale();

        actions.Controls.AddRange([
            search,
            create,
            refresh,
            next
        ]);

        _customerStatus.Dock = DockStyle.Bottom;
        _customerStatus.Height = 24;
        _customerStatus.ForeColor = ThemeHelper.Muted;
        _customerStatus.Text = "Chưa chọn khách hàng.";

        customerPanel.Controls.Add(_customerStatus);
        customerPanel.Controls.Add(actions);
        customerPanel.Controls.Add(fields);
        customerPanel.Controls.Add(customerTitle);

        // Sắp xếp các khu vực giao diện
        Controls.Add(_grid);
        Controls.Add(customerPanel);
        Controls.Add(filterPanel);
        Controls.Add(heading);
    }

    private void ConfigureGrid()
    {
        _grid.Dock = DockStyle.Fill;
        _grid.EmptyStateText = "Chưa có suất chiếu phù hợp. Hãy đổi ngày hoặc từ khóa tìm kiếm.";
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.MultiSelect = false;
        _grid.SelectionMode =
            DataGridViewSelectionMode.FullRowSelect;

        _grid.AutoGenerateColumns = false;
        _grid.RowHeadersVisible = false;

        _grid.AutoSizeColumnsMode =
            DataGridViewAutoSizeColumnsMode.Fill;

        _grid.Columns.Add(
            new DataGridViewTextBoxColumn
            {
                Name = "Movie",
                HeaderText = "Phim",
                FillWeight = 34
            });

        _grid.Columns.Add(
            new DataGridViewTextBoxColumn
            {
                Name = "Room",
                HeaderText = "Phòng",
                FillWeight = 14
            });

        _grid.Columns.Add(
            new DataGridViewTextBoxColumn
            {
                Name = "Start",
                HeaderText = "Thời gian",
                FillWeight = 24
            });

        _grid.Columns.Add(
            new DataGridViewTextBoxColumn
            {
                Name = "End",
                HeaderText = "Kết thúc",
                FillWeight = 18
            });

        _grid.Columns.Add(
            new DataGridViewTextBoxColumn
            {
                Name = "Price",
                HeaderText = "Giá cơ bản",
                FillWeight = 18
            });

        ThemeHelper.ApplyDefaultGridStyle(_grid);
    }

    // Tải danh sách suất chiếu từ hệ thống
    private void LoadShowtimes()
    {
        try
        {
            _allShowtimes = _showtimes.GetAllShowtimes()
                .Where(item =>
                    item.IsActive &&
                    item.StartTime > DateTime.Now)
                .OrderBy(item => item.StartTime)
                .ToList();

            ApplyShowtimeFilters();
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);
        }
    }

    // Tìm kiếm và lọc suất chiếu
    private void ApplyShowtimeFilters()
    {
        var keyword = _txtShowtimeSearch.Text.Trim();

        IEnumerable<Showtime> result = _allShowtimes;

        // Tìm theo tên phim hoặc tên phòng
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            result = result.Where(item =>
                (item.MovieTitle ?? string.Empty).Contains(
                    keyword,
                    StringComparison.OrdinalIgnoreCase) ||
                (item.RoomName ?? string.Empty).Contains(
                    keyword,
                    StringComparison.OrdinalIgnoreCase));
        }

        // Lọc theo ngày chiếu
        if (_chkDateFilter.Checked)
        {
            var selectedDate = _dtpShowtimeDate.Value.Date;

            result = result.Where(item =>
                item.StartTime.Date == selectedDate);
        }

        var filtered = result
            .OrderBy(item => item.StartTime)
            .ToList();

        _grid.Rows.Clear();

        foreach (var item in filtered)
        {
            var index = _grid.Rows.Add(
                item.MovieTitle,
                item.RoomName,
                item.StartTime.ToString("dd/MM/yyyy HH:mm"),
                item.EndTime.ToString("HH:mm"),
                $"{item.BasePrice:N0} đ");

            _grid.Rows[index].Tag = item;
        }

        // Tự chọn suất chiếu đầu tiên nếu có dữ liệu
        if (_grid.Rows.Count > 0)
        {
            _grid.ClearSelection();
            _grid.Rows[0].Selected = true;
            _grid.CurrentCell = _grid.Rows[0].Cells[0];
        }
    }

    private void FindCustomer()
    {
        var phone = _phone.Text.Trim();

        if (string.IsNullOrWhiteSpace(phone))
        {
            MessageHelper.Warning(
                "Vui lòng nhập số điện thoại khách hàng.");

            _phone.Focus();
            return;
        }

        try
        {
            var customer = _customers.FindByPhone(phone);

            if (customer is null)
            {
                _order.Customer = null;

                _customerStatus.ForeColor = ThemeHelper.Danger;
                _customerStatus.Text =
                    "Không tìm thấy khách hàng. Bạn có thể tạo khách hàng mới.";

                return;
            }

            SelectCustomer(customer);

            MessageHelper.Info(
                "Đã tìm thấy thông tin khách hàng.");
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);
        }
    }

    private void CreateCustomer()
    {
        var fullName = _fullName.Text.Trim();
        var phone = _phone.Text.Trim();
        var email = _email.Text.Trim();

        if (string.IsNullOrWhiteSpace(fullName))
        {
            MessageHelper.Warning(
                "Vui lòng nhập họ và tên khách hàng.");

            _fullName.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(phone))
        {
            MessageHelper.Warning(
                "Vui lòng nhập số điện thoại khách hàng.");

            _phone.Focus();
            return;
        }

        try
        {
            var customer = _customers.Create(new Customer
            {
                FullName = fullName,
                Phone = phone,
                Email = email
            });

            SelectCustomer(customer);

            MessageHelper.Info(
                "Tạo khách hàng thành công.");
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);
        }
    }

    private void SelectCustomer(Customer customer)
    {
        _order.Customer = customer;

        _phone.Text = customer.Phone;
        _fullName.Text = customer.FullName;
        _email.Text = customer.Email;

        _customerStatus.ForeColor = ThemeHelper.Success;
        _customerStatus.Text =
            $"Đã chọn: {customer.FullName} · {customer.Phone}";
    }

    private void StartSale()
    {
        if (_grid.CurrentRow?.Tag is not Showtime showtime)
        {
            MessageHelper.Warning(
                "Vui lòng chọn một suất chiếu.");

            return;
        }

        var customer = _order.Customer;

        if (customer is null)
        {
            MessageHelper.Warning(
                "Vui lòng tìm hoặc tạo khách hàng trước khi bán vé.");

            _phone.Focus();
            return;
        }

        if (customer.Id <= 0 ||
            string.IsNullOrWhiteSpace(customer.Phone) ||
            !string.Equals(
                customer.Phone.Trim(),
                _phone.Text.Trim(),
                StringComparison.Ordinal))
        {
            _order.Customer = null;

            _customerStatus.ForeColor = ThemeHelper.Danger;
            _customerStatus.Text =
                "Thông tin khách hàng đã thay đổi. Vui lòng tìm lại khách hàng.";

            MessageHelper.Warning(
                "Vui lòng xác nhận lại thông tin khách hàng.");

            return;
        }

        if (!showtime.IsActive ||
            showtime.StartTime <= DateTime.Now)
        {
            MessageHelper.Warning(
                "Suất chiếu này không còn hợp lệ. Vui lòng tải lại danh sách.");

            LoadShowtimes();
            return;
        }

        try
        {
            _order.Reset();

            _order.Showtime = showtime;
            _order.Customer = customer;

            using (var seats = _forms.Create<frmSeatSelection>())
            {
                if (seats.ShowDialog(this) != DialogResult.OK)
                {
                    ReleaseOrderHolds();
                    return;
                }
            }

            using (var products = _forms.Create<frmComboSelection>())
            {
                if (products.ShowDialog(this) != DialogResult.OK)
                {
                    ReleaseOrderHolds();
                    return;
                }
            }

            using (var checkout = _forms.Create<frmCheckout>())
            {
                if (checkout.ShowDialog(this) != DialogResult.OK)
                {
                    ReleaseOrderHolds();
                    return;
                }
            }

            using (var preview = _forms.Create<frmTicketPreview>())
            {
                preview.ShowDialog(this);
            }

            _order.Reset();

            MessageHelper.Info(
                "Giao dịch đã hoàn tất.");
        }
        catch (Exception ex)
        {
            ReleaseOrderHolds();
            MessageHelper.Handle(ex);
        }
    }

    private void ReleaseOrderHolds()
    {
        foreach (var holdId in _order.HoldIds.Values.ToList())
        {
            try
            {
                _holds.ReleaseHold(holdId);
            }
            catch
            {
                // Ghế có thể đã hết thời gian giữ.
            }
        }

        _order.Reset();
    }

    private static void AddField(
        TableLayoutPanel table,
        string title,
        TextBox input,
        int column)
    {
        table.Controls.Add(
            new Label
            {
                Text = title,
                Dock = DockStyle.Fill,
                ForeColor = ThemeHelper.Muted,
                TextAlign = ContentAlignment.MiddleLeft
            },
            column,
            0);

        input.Dock = DockStyle.Fill;
        input.Margin = new Padding(0, 3, 12, 4);

        table.Controls.Add(
            input,
            column,
            1);
    }

    private static Button CreateButton(
        string text,
        Color color)
    {
        var button = new Button
        {
            Text = text,
            Height = 32,
            AutoSize = true,
            BackColor = color,
            ForeColor = color == ThemeHelper.Accent
                ? ThemeHelper.Primary
                : Color.White,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 0, 8, 0),
            Padding = new Padding(12, 2, 12, 2),
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };

        ThemeHelper.StyleActionButton(button, color);

        return button;
    }
}
