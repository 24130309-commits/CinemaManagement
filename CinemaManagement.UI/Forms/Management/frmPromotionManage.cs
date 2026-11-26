using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.UI.Controls;
using CinemaManagement.UI.Helpers;

namespace CinemaManagement.UI.Forms.Management;

public partial class frmPromotionManage : UserControl
{
    private readonly IPromotionService _promotionService;
    private TextBox txtCode = null!;
    private TextBox txtDescription = null!;
    private ComboBox cboType = null!;
    private NumericUpDown numValue = null!;
    private NumericUpDown numMaxDiscount = null!;
    private NumericUpDown numMinOrder = null!;
    private NumericUpDown numUsageLimit = null!;
    private DateTimePicker dtpStartDate = null!;
    private DateTimePicker dtpEndDate = null!;
    private CheckBox chkActive = null!;
    private EmptyStateDataGridView dgvPromotions = null!;
    private int _selectedId;

    public frmPromotionManage(IPromotionService promotionService)
    {
        _promotionService = promotionService ?? throw new ArgumentNullException(nameof(promotionService));
        InitializeComponent();
        BuildLayout();
        LoadPromotions();
        ThemeHelper.Apply(this);
    }

    private void BuildLayout()
    {
        Controls.Clear();
        Text = "Quản lý khuyến mãi";

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(18),
            BackColor = ThemeHelper.Background
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 285));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var header = new PageHeader(
            "Khuyến mãi",
            "Thiết lập mã ưu đãi, điều kiện áp dụng, thời hạn và giới hạn sử dụng.");

        var group = new GroupBox
        {
            Text = "Thông tin chương trình khuyến mãi",
            Dock = DockStyle.Fill,
            Padding = new Padding(12)
        };
        var fields = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(4),
            BackColor = Color.Transparent
        };

        txtCode = new TextBox
        {
            CharacterCasing = CharacterCasing.Upper,
            Width = 205
        };
        cboType = new ComboBox
        {
            Width = 205,
            DropDownStyle = ComboBoxStyle.DropDownList,
            FormattingEnabled = true
        };
        cboType.Items.AddRange(new object[] { "Percent", "Fixed" });
        cboType.SelectedIndex = 0;
        cboType.SelectedIndexChanged += CboType_SelectedIndexChanged;
        cboType.Format += (_, e) =>
        {
            if (e.ListItem is string type)
                e.Value = string.Equals(type, "Percent", StringComparison.OrdinalIgnoreCase)
                    ? "Phần trăm"
                    : "Số tiền cố định";
        };

        numValue = new NumericUpDown
        {
            Width = 205,
            Minimum = 1,
            Maximum = 100,
            ThousandsSeparator = true
        };
        numMaxDiscount = new NumericUpDown
        {
            Width = 205,
            Minimum = 0,
            Maximum = 100000000,
            Increment = 5000,
            ThousandsSeparator = true
        };
        numMinOrder = new NumericUpDown
        {
            Width = 205,
            Minimum = 0,
            Maximum = 100000000,
            Increment = 5000,
            ThousandsSeparator = true
        };
        numUsageLimit = new NumericUpDown
        {
            Width = 205,
            Minimum = 0,
            Maximum = 1000000,
            Value = 100,
            ThousandsSeparator = true
        };
        dtpStartDate = new DateTimePicker
        {
            Width = 205,
            Format = DateTimePickerFormat.Short
        };
        dtpEndDate = new DateTimePicker
        {
            Width = 205,
            Format = DateTimePickerFormat.Short,
            Value = DateTime.Today.AddDays(30)
        };
        chkActive = new CheckBox
        {
            Text = "Kích hoạt khuyến mãi",
            AutoSize = true,
            Checked = true,
            Margin = new Padding(8, 25, 18, 8)
        };
        txtDescription = new TextBox
        {
            Width = 460,
            MaxLength = 500
        };

        fields.Controls.Add(CreateField("Mã khuyến mãi", txtCode));
        fields.Controls.Add(CreateField("Loại giảm", cboType));
        fields.Controls.Add(CreateField("Giá trị", numValue));
        fields.Controls.Add(CreateField("Giảm tối đa (VNĐ)", numMaxDiscount));
        fields.Controls.Add(CreateField("Đơn tối thiểu (VNĐ)", numMinOrder));
        fields.Controls.Add(CreateField("Giới hạn lượt dùng (0 = không giới hạn)", numUsageLimit));
        fields.Controls.Add(CreateField("Ngày bắt đầu", dtpStartDate));
        fields.Controls.Add(CreateField("Ngày kết thúc", dtpEndDate));
        fields.Controls.Add(chkActive);
        fields.Controls.Add(CreateField("Mô tả", txtDescription, 478));

        var actions = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = true,
            Margin = new Padding(4, 8, 4, 4)
        };
        var btnAdd = CreateButton("Thêm mã");
        var btnUpdate = CreateButton("Cập nhật");
        var btnDelete = CreateButton("Xóa");
        var btnReset = CreateButton("Làm mới");

        btnAdd.Click += (s, e) => Execute(() =>
        {
            _promotionService.AddPromotion(CreatePromotion());
            ResetForm();
        });

        btnUpdate.Click += (s, e) => Execute(() =>
        {
            if (_selectedId == 0)
                throw new InvalidOperationException("Vui lòng chọn khuyến mãi cần cập nhật.");

            var promotion = CreatePromotion();
            promotion.Id = _selectedId;
            _promotionService.UpdatePromotion(promotion);
        });

        btnDelete.Click += (s, e) => Execute(() =>
        {
            if (_selectedId == 0)
                throw new InvalidOperationException("Vui lòng chọn khuyến mãi cần xóa.");

            if (!MessageHelper.Confirm(
                    "Bạn có chắc chắn muốn xóa mã khuyến mãi này?",
                    "Xác nhận xóa khuyến mãi"))
                return;

            _promotionService.DeletePromotion(_selectedId);
            ResetForm();
        });

        btnReset.Click += (s, e) => ResetForm();

        actions.Controls.AddRange(new Control[]
        {
            btnAdd, btnUpdate, btnDelete, btnReset
        });
        fields.Controls.Add(actions);
        group.Controls.Add(fields);

        dgvPromotions = CreateGrid();
        dgvPromotions.CellClick += DgvPromotions_CellClick;
        dgvPromotions.CellFormatting += DgvPromotions_CellFormatting;

        root.Controls.Add(header, 0, 0);
        root.Controls.Add(group, 0, 1);
        root.Controls.Add(dgvPromotions, 0, 2);
        Controls.Add(root);
    }

    private static Control CreateField(
        string label,
        Control editor,
        int width = 235)
    {
        var panel = new Panel
        {
            Width = width,
            Height = 62,
            Margin = new Padding(4, 2, 12, 3)
        };

        panel.Controls.Add(new Label
        {
            Text = label,
            AutoSize = true,
            Location = new Point(0, 0),
            ForeColor = ThemeHelper.Muted,
            Font = ThemeHelper.SmallFont
        });

        editor.Location = new Point(0, 23);
        editor.Width = width - 8;
        panel.Controls.Add(editor);

        return panel;
    }

    private static Button CreateButton(string text)
    {
        return new Button
        {
            Text = text,
            Width = 105,
            Height = 34,
            Margin = new Padding(0, 2, 9, 0),
            Cursor = Cursors.Hand
        };
    }

    private static EmptyStateDataGridView CreateGrid()
    {
        return new EmptyStateDataGridView
        {
            EmptyStateText = "Chưa có chương trình khuyến mãi.",
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            MultiSelect = false,
            RowHeadersVisible = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None
        };
    }

    private void CboType_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (numValue == null || cboType.SelectedItem == null)
            return;

        var isPercent = cboType.SelectedItem.ToString() == "Percent";
        var maximum = isPercent ? 100M : 100000000M;

        numValue.Maximum = maximum;

        if (numValue.Value > maximum)
            numValue.Value = maximum;
    }

    private Promotion CreatePromotion()
    {
        if (string.IsNullOrWhiteSpace(txtCode.Text))
            throw new InvalidOperationException("Vui lòng nhập mã khuyến mãi.");

        if (cboType.SelectedItem == null)
            throw new InvalidOperationException("Vui lòng chọn loại giảm giá.");

        if (dtpEndDate.Value.Date < dtpStartDate.Value.Date)
            throw new InvalidOperationException("Ngày kết thúc không được trước ngày bắt đầu.");

        return new Promotion
        {
            Code = txtCode.Text.Trim().ToUpperInvariant(),
            Description = txtDescription.Text.Trim(),
            Type = cboType.SelectedItem.ToString() ?? "Percent",
            Value = numValue.Value,
            MaxDiscountAmount = numMaxDiscount.Value,
            MinOrder = numMinOrder.Value,
            UsageLimit = (int)numUsageLimit.Value,
            StartDate = dtpStartDate.Value.Date,
            EndDate = dtpEndDate.Value.Date.AddDays(1).AddTicks(-1),
            IsActive = chkActive.Checked
        };
    }

    private void DgvPromotions_CellClick(
        object? sender,
        DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 ||
            dgvPromotions.Rows[e.RowIndex].DataBoundItem is not Promotion promotion)
            return;

        _selectedId = promotion.Id;
        txtCode.Text = promotion.Code;
        txtDescription.Text = promotion.Description;
        cboType.SelectedItem = promotion.Type;

        numValue.Value = Clamp(numValue, promotion.Value);
        numMaxDiscount.Value = Clamp(numMaxDiscount, promotion.MaxDiscountAmount);
        numMinOrder.Value = Clamp(numMinOrder, promotion.MinOrder);
        numUsageLimit.Value = Clamp(numUsageLimit, promotion.UsageLimit);

        dtpStartDate.Value = promotion.StartDate;
        dtpEndDate.Value = promotion.EndDate;
        chkActive.Checked = promotion.IsActive;
    }

    private static decimal Clamp(NumericUpDown control, decimal value)
    {
        return Math.Max(control.Minimum, Math.Min(control.Maximum, value));
    }

    private void Execute(Action action)
    {
        try
        {
            action();
            LoadPromotions();
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex, "Không thể lưu chương trình khuyến mãi.");
        }
    }

    private void LoadPromotions()
    {
        dgvPromotions.DataSource =
            _promotionService.GetAllPromotions().ToList();

        ConfigurePromotionColumns();
    }

    private void ConfigurePromotionColumns()
    {
        SetColumn(nameof(Promotion.Id), "Mã", false);
        SetColumn(nameof(Promotion.Code), "Mã ưu đãi");
        SetColumn(nameof(Promotion.Description), "Mô tả");
        SetColumn(nameof(Promotion.Type), "Loại giảm");
        SetColumn(nameof(Promotion.Value), "Mức giảm");
        SetColumn(nameof(Promotion.DiscountPercent), "Giảm (%)", false);
        SetColumn(nameof(Promotion.MaxDiscountAmount), "Giảm tối đa", format: "N0");
        SetColumn(nameof(Promotion.StartDate), "Bắt đầu", format: "dd/MM/yyyy");
        SetColumn(nameof(Promotion.EndDate), "Kết thúc", format: "dd/MM/yyyy");
        SetColumn(nameof(Promotion.MinOrder), "Đơn tối thiểu", format: "N0");
        SetColumn(nameof(Promotion.UsageLimit), "Giới hạn lượt");
        SetColumn(nameof(Promotion.UsedCount), "Đã sử dụng");
        SetColumn(nameof(Promotion.IsActive), "Đang bật");
        SetColumn(nameof(Promotion.IsCurrentlyValid), "Còn hiệu lực");
    }

    private void SetColumn(
        string name,
        string header,
        bool visible = true,
        string? format = null)
    {
        if (!dgvPromotions.Columns.Contains(name))
            return;

        var column = dgvPromotions.Columns[name]!;
        column.HeaderText = header;
        column.Visible = visible;

        if (format is not null)
            column.DefaultCellStyle.Format = format;
    }

    private void DgvPromotions_CellFormatting(
        object? sender,
        DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0 ||
            dgvPromotions.Rows[e.RowIndex].DataBoundItem is not Promotion promotion)
        {
            return;
        }

        var columnName = dgvPromotions.Columns[e.ColumnIndex].Name;
        switch (columnName)
        {
            case nameof(Promotion.Type) when e.Value is string type:
                e.Value = string.Equals(type, "Percent", StringComparison.OrdinalIgnoreCase)
                    ? "Phần trăm"
                    : "Số tiền";
                e.FormattingApplied = true;
                break;

            case nameof(Promotion.Value) when e.Value is decimal value:
                e.Value = string.Equals(promotion.Type, "Percent", StringComparison.OrdinalIgnoreCase)
                    ? $"{value:N0}%"
                    : $"{value:N0} đ";
                e.FormattingApplied = true;
                break;

            case nameof(Promotion.UsageLimit) when e.Value is int usageLimit && usageLimit == 0:
                e.Value = "Không giới hạn";
                e.FormattingApplied = true;
                break;

            case nameof(Promotion.IsActive) when e.Value is bool isActive:
                e.CellStyle.ForeColor = isActive ? ThemeHelper.Success : ThemeHelper.Muted;
                break;

            case nameof(Promotion.IsCurrentlyValid) when e.Value is bool isValid:
                e.CellStyle.ForeColor = isValid ? ThemeHelper.Success : ThemeHelper.Muted;
                break;
        }
    }

    private void ResetForm()
    {
        _selectedId = 0;
        txtCode.Clear();
        txtDescription.Clear();
        cboType.SelectedIndex = 0;

        numValue.Value = 10;
        numMaxDiscount.Value = 0;
        numMinOrder.Value = 0;
        numUsageLimit.Value = 100;

        dtpStartDate.Value = DateTime.Today;
        dtpEndDate.Value = DateTime.Today.AddDays(30);

        chkActive.Checked = true;
        dgvPromotions.ClearSelection();
    }
}
