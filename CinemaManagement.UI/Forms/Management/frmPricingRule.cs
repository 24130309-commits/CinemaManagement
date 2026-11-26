using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.UI.Controls;
using CinemaManagement.UI.Helpers;

namespace CinemaManagement.UI.Forms.Management;

public partial class frmPricingRule : UserControl
{
    private readonly IPricingService _pricingService;
    private TextBox txtRuleName = null!;
    private ComboBox cboSeatType = null!;
    private ComboBox cboScreenType = null!;
    private ComboBox cboDayType = null!;
    private DateTimePicker dtpTimeFrom = null!;
    private DateTimePicker dtpTimeTo = null!;
    private NumericUpDown numMultiplier = null!;
    private NumericUpDown numExtraFee = null!;
    private EmptyStateDataGridView dgvRules = null!;
    private int _selectedId;

    public frmPricingRule(IPricingService pricingService)
    {
        _pricingService = pricingService ?? throw new ArgumentNullException(nameof(pricingService));
        InitializeComponent();
        BuildLayout();
        LoadRules();
        ThemeHelper.Apply(this);
    }

    private void BuildLayout()
    {
        Controls.Clear();
        Text = "Quản lý quy tắc giá vé";

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(18),
            BackColor = ThemeHelper.Background
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 245));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var header = new PageHeader(
            "Quy tắc giá vé",
            "Cấu hình hệ số và phụ thu theo ghế, màn hình, ngày và khung giờ.");

        var group = new GroupBox
        {
            Text = "Thông tin quy tắc giá",
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

        txtRuleName = new TextBox { Width = 220 };
        cboSeatType = new ComboBox
        {
            Width = 220,
            DropDownStyle = ComboBoxStyle.DropDownList,
            FormattingEnabled = true,
            DataSource = Enum.GetValues(typeof(SeatType))
        };
        cboScreenType = new ComboBox
        {
            Width = 220,
            DropDownStyle = ComboBoxStyle.DropDownList,
            FormattingEnabled = true,
            DataSource = Enum.GetValues(typeof(ScreenType))
        };
        cboDayType = new ComboBox
        {
            Width = 220,
            DropDownStyle = ComboBoxStyle.DropDownList,
            FormattingEnabled = true
        };
        cboDayType.Items.AddRange(new object[]
        {
            "All", "Weekday", "Weekend"
        });
        cboDayType.SelectedIndex = 0;

        cboSeatType.Format += (_, e) =>
        {
            if (e.ListItem is SeatType seatType)
                e.Value = GetSeatTypeText(seatType);
        };

        cboScreenType.Format += (_, e) =>
        {
            if (e.ListItem is ScreenType screenType)
                e.Value = GetScreenTypeText(screenType);
        };

        cboDayType.Format += (_, e) =>
        {
            if (e.ListItem is string dayType)
                e.Value = GetDayTypeText(dayType);
        };

        dtpTimeFrom = CreateTimePicker();
        dtpTimeTo = CreateTimePicker();
        dtpTimeTo.Value = DateTime.Today.AddHours(23);

        numMultiplier = new NumericUpDown
        {
            Width = 220,
            DecimalPlaces = 2,
            Minimum = 0.10M,
            Maximum = 10M,
            Increment = 0.05M,
            Value = 1.00M
        };
        numExtraFee = new NumericUpDown
        {
            Width = 220,
            Minimum = 0,
            Maximum = 500000,
            Increment = 5000,
            ThousandsSeparator = true
        };

        fields.Controls.Add(CreateField("Tên quy tắc", txtRuleName));
        fields.Controls.Add(CreateField("Loại ghế", cboSeatType));
        fields.Controls.Add(CreateField("Loại màn hình", cboScreenType));
        fields.Controls.Add(CreateField("Ngày áp dụng", cboDayType));
        fields.Controls.Add(CreateField("Từ giờ", dtpTimeFrom));
        fields.Controls.Add(CreateField("Đến giờ", dtpTimeTo));
        fields.Controls.Add(CreateField("Hệ số giá", numMultiplier));
        fields.Controls.Add(CreateField("Phụ thu (VNĐ)", numExtraFee));

        var actions = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = true,
            Margin = new Padding(4, 8, 4, 4)
        };

        var btnAdd = CreateButton("Thêm");
        var btnUpdate = CreateButton("Cập nhật");
        var btnDelete = CreateButton("Xóa");
        var btnReset = CreateButton("Làm mới");

        btnAdd.Click += (s, e) => Execute(() =>
        {
            _pricingService.AddRule(CreateRule());
            ResetForm();
        });

        btnUpdate.Click += (s, e) => Execute(() =>
        {
            if (_selectedId == 0)
                throw new InvalidOperationException("Vui lòng chọn quy tắc cần cập nhật.");

            var rule = CreateRule();
            rule.Id = _selectedId;
            _pricingService.UpdateRule(rule);
        });

        btnDelete.Click += (s, e) => Execute(() =>
        {
            if (_selectedId == 0)
                throw new InvalidOperationException("Vui lòng chọn quy tắc cần xóa.");

            if (!MessageHelper.Confirm(
                    "Bạn có chắc chắn muốn xóa quy tắc này?",
                    "Xác nhận xóa quy tắc giá"))
                return;

            _pricingService.DeleteRule(_selectedId);
            ResetForm();
        });

        btnReset.Click += (s, e) => ResetForm();

        actions.Controls.AddRange(new Control[]
        {
            btnAdd, btnUpdate, btnDelete, btnReset
        });
        fields.Controls.Add(actions);
        group.Controls.Add(fields);

        dgvRules = new EmptyStateDataGridView
        {
            EmptyStateText = "Chưa có quy tắc giá vé được cấu hình.",
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
        dgvRules.CellClick += DgvRules_CellClick;
        dgvRules.CellFormatting += DgvRules_CellFormatting;

        root.Controls.Add(header, 0, 0);
        root.Controls.Add(group, 0, 1);
        root.Controls.Add(dgvRules, 0, 2);
        Controls.Add(root);
    }

    private static Control CreateField(string label, Control editor)
    {
        var panel = new Panel
        {
            Width = 250,
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
        editor.Width = 240;
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

    private static DateTimePicker CreateTimePicker()
    {
        return new DateTimePicker
        {
            Width = 220,
            Format = DateTimePickerFormat.Time,
            ShowUpDown = true,
            Value = DateTime.Today.AddHours(8)
        };
    }

    private PricingRule CreateRule()
    {
        if (string.IsNullOrWhiteSpace(txtRuleName.Text))
            throw new InvalidOperationException("Vui lòng nhập tên quy tắc giá.");

        if (dtpTimeTo.Value.TimeOfDay == dtpTimeFrom.Value.TimeOfDay)
            throw new InvalidOperationException("Giờ bắt đầu và kết thúc không được trùng nhau.");

        if (cboSeatType.SelectedItem is not SeatType seatType ||
            cboScreenType.SelectedItem is not ScreenType screenType)
        {
            throw new InvalidOperationException(
                "Vui lòng chọn loại ghế và loại màn hình.");
        }

        return new PricingRule
        {
            Name = txtRuleName.Text.Trim(),
            SeatType = seatType,
            ScreenType = screenType,
            DayType = cboDayType.SelectedItem?.ToString() ?? "All",
            TimeFrom = dtpTimeFrom.Value.ToString("HH:mm"),
            TimeTo = dtpTimeTo.Value.ToString("HH:mm"),
            Multiplier = numMultiplier.Value,
            ExtraFee = numExtraFee.Value
        };
    }

    private void DgvRules_CellClick(
        object? sender,
        DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 ||
            dgvRules.Rows[e.RowIndex].DataBoundItem is not PricingRule rule)
            return;

        _selectedId = rule.Id;
        txtRuleName.Text = rule.Name;

        cboSeatType.SelectedItem = rule.SeatType;
        cboScreenType.SelectedItem = rule.ScreenType;
        cboDayType.SelectedItem = rule.DayType;

        if (TimeSpan.TryParse(rule.TimeFrom, out var from))
            dtpTimeFrom.Value = DateTime.Today.Add(from);

        if (TimeSpan.TryParse(rule.TimeTo, out var to))
            dtpTimeTo.Value = DateTime.Today.Add(to);

        numMultiplier.Value = Clamp(numMultiplier, rule.Multiplier);
        numExtraFee.Value = Clamp(numExtraFee, rule.ExtraFee);
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
            LoadRules();
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex, "Không thể lưu quy tắc giá.");
        }
    }

    private void LoadRules()
    {
        dgvRules.DataSource = _pricingService.GetAllRules().ToList();
        ConfigureGridColumns();
    }

    private void ConfigureGridColumns()
    {
        SetColumn(nameof(PricingRule.Id), "Mã", false);
        SetColumn(nameof(PricingRule.Name), "Tên quy tắc");
        SetColumn(nameof(PricingRule.SeatType), "Loại ghế");
        SetColumn(nameof(PricingRule.ScreenType), "Loại màn hình");
        SetColumn(nameof(PricingRule.DayType), "Ngày áp dụng");
        SetColumn(nameof(PricingRule.TimeFrom), "Từ giờ");
        SetColumn(nameof(PricingRule.TimeTo), "Đến giờ");
        SetColumn(nameof(PricingRule.Multiplier), "Hệ số", format: "N2");
        SetColumn(nameof(PricingRule.ExtraFee), "Phụ thu (VNĐ)", format: "N0");
    }

    private void SetColumn(
        string name,
        string header,
        bool visible = true,
        string? format = null)
    {
        if (!dgvRules.Columns.Contains(name))
            return;

        var column = dgvRules.Columns[name]!;
        column.HeaderText = header;
        column.Visible = visible;

        if (format is not null)
            column.DefaultCellStyle.Format = format;
    }

    private void DgvRules_CellFormatting(
        object? sender,
        DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
            return;

        var columnName = dgvRules.Columns[e.ColumnIndex].Name;
        if (columnName == nameof(PricingRule.SeatType) && e.Value is SeatType seatType)
        {
            e.Value = GetSeatTypeText(seatType);
            e.FormattingApplied = true;
        }
        else if (columnName == nameof(PricingRule.ScreenType) && e.Value is ScreenType screenType)
        {
            e.Value = GetScreenTypeText(screenType);
            e.FormattingApplied = true;
        }
        else if (columnName == nameof(PricingRule.DayType) && e.Value is string dayType)
        {
            e.Value = GetDayTypeText(dayType);
            e.FormattingApplied = true;
        }
    }

    private static string GetSeatTypeText(SeatType seatType)
    {
        return seatType switch
        {
            SeatType.Normal => "Thường",
            SeatType.VIP => "VIP",
            SeatType.Couple => "Đôi",
            SeatType.Aisle => "Lối đi",
            _ => seatType.ToString()
        };
    }

    private static string GetScreenTypeText(ScreenType screenType)
    {
        return screenType switch
        {
            ScreenType.TwoD => "2D",
            ScreenType.ThreeD => "3D",
            ScreenType.IMAX => "IMAX",
            _ => screenType.ToString()
        };
    }

    private static string GetDayTypeText(string dayType)
    {
        return dayType switch
        {
            "All" => "Tất cả ngày",
            "Weekday" => "Ngày thường",
            "Weekend" => "Cuối tuần",
            _ => dayType
        };
    }

    private void ResetForm()
    {
        _selectedId = 0;
        txtRuleName.Clear();

        cboSeatType.SelectedIndex = 0;
        cboScreenType.SelectedIndex = 0;
        cboDayType.SelectedIndex = 0;

        dtpTimeFrom.Value = DateTime.Today.AddHours(8);
        dtpTimeTo.Value = DateTime.Today.AddHours(23);

        numMultiplier.Value = 1.00M;
        numExtraFee.Value = 0;

        dgvRules.ClearSelection();
    }
}
