
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Enums;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.UI.Forms.Management
{
    public partial class frmPricingRule : Form
    {
        private readonly IPricingService _pricingService;

        private ComboBox cboSeatType;
        private ComboBox cboScreenType;
        private ComboBox cboDayType;
        private DateTimePicker dtpTimeFrom;
        private DateTimePicker dtpTimeTo;
        private NumericUpDown numMultiplier;
        private DataGridView dgvRules;

        private int _selectedId;

        public frmPricingRule(IPricingService pricingService)
        {
            _pricingService = pricingService
                ?? throw new ArgumentNullException(nameof(pricingService));

            InitializeComponent();
            BuildLayout();
            LoadRules();
        }

        private void BuildLayout()
        {
            Controls.Clear();

            Text = "Quản lý quy tắc giá vé";
            Size = new Size(900, 540);
            StartPosition = FormStartPosition.CenterParent;

            var group = new GroupBox
            {
                Text = "Thông tin quy tắc giá",
                Location = new Point(15, 15),
                Size = new Size(850, 175)
            };

            AddLabel(group, "Loại ghế:", 20, 30);
            cboSeatType = new ComboBox
            {
                Location = new Point(105, 27),
                Width = 150,
                DropDownStyle = ComboBoxStyle.DropDownList,
                DataSource = Enum.GetValues(typeof(SeatType))
            };

            AddLabel(group, "Loại màn hình:", 280, 30);
            cboScreenType = new ComboBox
            {
                Location = new Point(390, 27),
                Width = 150,
                DropDownStyle = ComboBoxStyle.DropDownList,
                DataSource = Enum.GetValues(typeof(ScreenType))
            };

            AddLabel(group, "Ngày áp dụng:", 20, 70);
            cboDayType = new ComboBox
            {
                Location = new Point(105, 67),
                Width = 150,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            cboDayType.Items.AddRange(new object[]
            {
                "Weekday",
                "Weekend",
                "Holiday"
            });
            cboDayType.SelectedIndex = 0;

            AddLabel(group, "Từ giờ:", 280, 70);
            dtpTimeFrom = CreateTimePicker(350, 67);

            AddLabel(group, "Đến giờ:", 530, 70);
            dtpTimeTo = CreateTimePicker(600, 67);
            dtpTimeTo.Value = DateTime.Today.AddHours(23);

            AddLabel(group, "Hệ số giá:", 20, 110);
            numMultiplier = new NumericUpDown
            {
                Location = new Point(105, 107),
                Width = 150,
                DecimalPlaces = 2,
                Minimum = 0.10M,
                Maximum = 10M,
                Increment = 0.05M,
                Value = 1.00M
            };

            var btnAdd = new Button
            {
                Text = "Thêm",
                Location = new Point(280, 105),
                Size = new Size(85, 30)
            };

            var btnUpdate = new Button
            {
                Text = "Cập nhật",
                Location = new Point(375, 105),
                Size = new Size(90, 30)
            };

            var btnDelete = new Button
            {
                Text = "Xóa",
                Location = new Point(475, 105),
                Size = new Size(85, 30)
            };

            var btnReset = new Button
            {
                Text = "Làm mới",
                Location = new Point(570, 105),
                Size = new Size(90, 30)
            };

            btnAdd.Click += (s, e) => Execute(() =>
            {
                _pricingService.AddRule(CreateRule());
                ResetForm();
            });

            btnUpdate.Click += (s, e) => Execute(() =>
            {
                if (_selectedId == 0)
                    throw new InvalidOperationException(
                        "Vui lòng chọn quy tắc cần cập nhật.");

                var rule = CreateRule();
                rule.Id = _selectedId;

                _pricingService.UpdateRule(rule);
            });

            btnDelete.Click += (s, e) => Execute(() =>
            {
                if (_selectedId == 0)
                    throw new InvalidOperationException(
                        "Vui lòng chọn quy tắc cần xóa.");

                if (MessageBox.Show(
                    "Bạn có chắc chắn muốn xóa quy tắc này?",
                    "Xác nhận",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes)
                    return;

                _pricingService.DeleteRule(_selectedId);
                ResetForm();
            });

            btnReset.Click += (s, e) => ResetForm();

            group.Controls.AddRange(new Control[]
            {
                cboSeatType, cboScreenType, cboDayType,
                dtpTimeFrom, dtpTimeTo, numMultiplier,
                btnAdd, btnUpdate, btnDelete, btnReset
            });

            dgvRules = new DataGridView
            {
                Location = new Point(15, 205),
                Size = new Size(850, 285),
                ReadOnly = true,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                MultiSelect = false
            };

            dgvRules.CellClick += DgvRules_CellClick;

            Controls.Add(group);
            Controls.Add(dgvRules);
        }

        private static void AddLabel(Control parent, string text, int x, int y)
        {
            parent.Controls.Add(new Label
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true
            });
        }

        private static DateTimePicker CreateTimePicker(int x, int y)
        {
            return new DateTimePicker
            {
                Location = new Point(x, y),
                Width = 130,
                Format = DateTimePickerFormat.Time,
                ShowUpDown = true,
                Value = DateTime.Today.AddHours(8)
            };
        }

        private PricingRule CreateRule()
        {
            if (dtpTimeTo.Value.TimeOfDay <= dtpTimeFrom.Value.TimeOfDay)
                throw new InvalidOperationException(
                    "Giờ kết thúc phải sau giờ bắt đầu.");

            return new PricingRule
            {
                SeatType = (SeatType)cboSeatType.SelectedItem,
                ScreenType = (ScreenType)cboScreenType.SelectedItem,
                DayType = cboDayType.SelectedItem?.ToString(),
                TimeFrom = dtpTimeFrom.Value.ToString("HH:mm"),
                TimeTo = dtpTimeTo.Value.ToString("HH:mm"),
                Multiplier = numMultiplier.Value
            };
        }

        private void DgvRules_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 ||
                dgvRules.Rows[e.RowIndex].DataBoundItem is not PricingRule rule)
                return;

            _selectedId = rule.Id;
            cboSeatType.SelectedItem = rule.SeatType;
            cboScreenType.SelectedItem = rule.ScreenType;
            cboDayType.SelectedItem = rule.DayType;

            if (TimeSpan.TryParse(rule.TimeFrom, out var from))
                dtpTimeFrom.Value = DateTime.Today.Add(from);

            if (TimeSpan.TryParse(rule.TimeTo, out var to))
                dtpTimeTo.Value = DateTime.Today.Add(to);

            numMultiplier.Value = Math.Max(
                numMultiplier.Minimum,
                Math.Min(numMultiplier.Maximum, rule.Multiplier));
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
                MessageBox.Show(
                    ex.Message,
                    "Lỗi quy tắc giá",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void LoadRules()
        {
            dgvRules.DataSource =
                _pricingService.GetAllRules().ToList();
        }

        private void ResetForm()
        {
            _selectedId = 0;
            cboSeatType.SelectedIndex = 0;
            cboScreenType.SelectedIndex = 0;
            cboDayType.SelectedIndex = 0;
            dtpTimeFrom.Value = DateTime.Today.AddHours(8);
            dtpTimeTo.Value = DateTime.Today.AddHours(23);
            numMultiplier.Value = 1.00M;
            dgvRules.ClearSelection();
        }
    }
}
