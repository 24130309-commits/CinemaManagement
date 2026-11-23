
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

        private TextBox txtRuleName = null!;
        private ComboBox cboSeatType = null!;
        private ComboBox cboScreenType = null!;
        private ComboBox cboDayType = null!;
        private DateTimePicker dtpTimeFrom = null!;
        private DateTimePicker dtpTimeTo = null!;
        private NumericUpDown numMultiplier = null!;
        private NumericUpDown numExtraFee = null!;
        private DataGridView dgvRules = null!;

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
            Size = new Size(960, 650);
            MinimumSize = new Size(900, 600);
            StartPosition = FormStartPosition.CenterParent;

            var group = new GroupBox
            {
                Text = "Thông tin quy tắc giá",
                Location = new Point(15, 15),
                Size = new Size(915, 245)
            };

            AddLabel(group, "Tên quy tắc:", 20, 32);

            txtRuleName = new TextBox
            {
                Location = new Point(110, 29),
                Width = 200
            };

            AddLabel(group, "Loại ghế:", 335, 32);

            cboSeatType = new ComboBox
            {
                Location = new Point(410, 29),
                Width = 150,
                DropDownStyle = ComboBoxStyle.DropDownList,
                DataSource = Enum.GetValues(typeof(SeatType))
            };

            AddLabel(group, "Loại màn hình:", 585, 32);

            cboScreenType = new ComboBox
            {
                Location = new Point(690, 29),
                Width = 180,
                DropDownStyle = ComboBoxStyle.DropDownList,
                DataSource = Enum.GetValues(typeof(ScreenType))
            };

            AddLabel(group, "Ngày áp dụng:", 20, 77);

            cboDayType = new ComboBox
            {
                Location = new Point(110, 74),
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            cboDayType.Items.AddRange(new object[]
            {
                "All",
                "Weekday",
                "Weekend",
                "Holiday"
            });

            cboDayType.SelectedIndex = 0;

            AddLabel(group, "Từ giờ:", 335, 77);

            dtpTimeFrom = CreateTimePicker(410, 74);

            AddLabel(group, "Đến giờ:", 585, 77);

            dtpTimeTo = CreateTimePicker(690, 74);
            dtpTimeTo.Value = DateTime.Today.AddHours(23);

            AddLabel(group, "Hệ số giá:", 20, 122);

            numMultiplier = new NumericUpDown
            {
                Location = new Point(110, 119),
                Width = 200,
                DecimalPlaces = 2,
                Minimum = 0.10M,
                Maximum = 10M,
                Increment = 0.05M,
                Value = 1.00M
            };

            AddLabel(group, "Phụ thu (VNĐ):", 335, 122);

            numExtraFee = new NumericUpDown
            {
                Location = new Point(435, 119),
                Width = 200,
                Minimum = -200000,
                Maximum = 500000,
                Increment = 5000,
                ThousandsSeparator = true
            };

            var btnAdd = new Button
            {
                Text = "Thêm",
                Location = new Point(20, 180),
                Size = new Size(100, 32)
            };

            var btnUpdate = new Button
            {
                Text = "Cập nhật",
                Location = new Point(135, 180),
                Size = new Size(100, 32)
            };

            var btnDelete = new Button
            {
                Text = "Xóa",
                Location = new Point(250, 180),
                Size = new Size(100, 32)
            };

            var btnReset = new Button
            {
                Text = "Làm mới",
                Location = new Point(365, 180),
                Size = new Size(100, 32)
            };

            btnAdd.Click += (s, e) => Execute(() =>
            {
                _pricingService.AddRule(CreateRule());
                ResetForm();
            });

            btnUpdate.Click += (s, e) => Execute(() =>
            {
                if (_selectedId == 0)
                {
                    throw new InvalidOperationException(
                        "Vui lòng chọn quy tắc cần cập nhật.");
                }

                var rule = CreateRule();
                rule.Id = _selectedId;

                _pricingService.UpdateRule(rule);
            });

            btnDelete.Click += (s, e) => Execute(() =>
            {
                if (_selectedId == 0)
                {
                    throw new InvalidOperationException(
                        "Vui lòng chọn quy tắc cần xóa.");
                }

                var confirm = MessageBox.Show(
                    "Bạn có chắc chắn muốn xóa quy tắc này?",
                    "Xác nhận",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirm != DialogResult.Yes)
                {
                    return;
                }

                _pricingService.DeleteRule(_selectedId);
                ResetForm();
            });

            btnReset.Click += (s, e) => ResetForm();

            group.Controls.AddRange(new Control[]
            {
                txtRuleName,
                cboSeatType,
                cboScreenType,
                cboDayType,
                dtpTimeFrom,
                dtpTimeTo,
                numMultiplier,
                numExtraFee,
                btnAdd,
                btnUpdate,
                btnDelete,
                btnReset
            });

            dgvRules = new DataGridView
            {
                Location = new Point(15, 275),
                Size = new Size(915, 320),
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

        private static void AddLabel(
            Control parent,
            string text,
            int x,
            int y)
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
                Width = 150,
                Format = DateTimePickerFormat.Time,
                ShowUpDown = true,
                Value = DateTime.Today.AddHours(8)
            };
        }

        private PricingRule CreateRule()
        {
            if (string.IsNullOrWhiteSpace(txtRuleName.Text))
            {
                throw new InvalidOperationException(
                    "Vui lòng nhập tên quy tắc giá.");
            }

            if (dtpTimeTo.Value.TimeOfDay <= dtpTimeFrom.Value.TimeOfDay)
            {
                throw new InvalidOperationException(
                    "Giờ kết thúc phải sau giờ bắt đầu.");
            }

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
            {
                return;
            }

            _selectedId = rule.Id;

            txtRuleName.Text = rule.Name;
            cboSeatType.SelectedItem = rule.SeatType;
            cboScreenType.SelectedItem = rule.ScreenType;
            cboDayType.SelectedItem = rule.DayType;

            if (TimeSpan.TryParse(rule.TimeFrom, out var from))
            {
                dtpTimeFrom.Value = DateTime.Today.Add(from);
            }

            if (TimeSpan.TryParse(rule.TimeTo, out var to))
            {
                dtpTimeTo.Value = DateTime.Today.Add(to);
            }

            numMultiplier.Value = Math.Max(
                numMultiplier.Minimum,
                Math.Min(numMultiplier.Maximum, rule.Multiplier));

            numExtraFee.Value = Math.Max(
                numExtraFee.Minimum,
                Math.Min(numExtraFee.Maximum, rule.ExtraFee));
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
}
