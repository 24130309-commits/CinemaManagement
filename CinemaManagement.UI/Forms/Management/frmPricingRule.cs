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
        private TextBox txtRuleName, txtDayType;
        private ComboBox cboSeatType;
        private NumericUpDown numExtraFee;
        private DataGridView dgvRules;
        private int _selectedId = 0;

        public frmPricingRule(IPricingService pricingService)
        {
            _pricingService = pricingService;
            InitializeComponent();
            SetupLayout();
            LoadRules();
        }

        private void SetupLayout()
        {
            if (Controls.Count > 0) return;

            Text = "Cấu Hình Quy Tắc Giá Vé";
            Size = new Size(860, 500);
            StartPosition = FormStartPosition.CenterParent;

            var grp = new GroupBox { Text = "Thông Tin Quy Tắc Phụ Thu", Location = new Point(15, 15), Size = new Size(815, 120) };

            grp.Controls.Add(new Label { Text = "Tên Quy Tắc:", Location = new Point(20, 30), AutoSize = true });
            txtRuleName = new TextBox { Location = new Point(110, 27), Width = 200 };

            grp.Controls.Add(new Label { Text = "Loại Ngày:", Location = new Point(330, 30), AutoSize = true });
            txtDayType = new TextBox { Location = new Point(405, 27), Width = 140, PlaceholderText = "Weekday / Weekend" };

            grp.Controls.Add(new Label { Text = "Loại Ghế:", Location = new Point(565, 30), AutoSize = true });
            cboSeatType = new ComboBox { Location = new Point(635, 27), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            cboSeatType.DataSource = Enum.GetNames(typeof(SeatType));

            grp.Controls.Add(new Label { Text = "Phụ Thu (VNĐ):", Location = new Point(20, 70), AutoSize = true });
            numExtraFee = new NumericUpDown { Location = new Point(115, 67), Width = 130, Minimum = -200000, Maximum = 500000, Increment = 5000 };

            var btnAdd = new Button { Text = "Thêm Quy Tắc", Location = new Point(275, 65), Size = new Size(115, 30) };
            var btnUpdate = new Button { Text = "Cập Nhật", Location = new Point(400, 65), Size = new Size(100, 30) };
            var btnDelete = new Button { Text = "Xóa", Location = new Point(510, 65), Size = new Size(85, 30) };

            btnAdd.Click += (s, e) => Execute(() =>
            {
                _pricingService.AddRule(new PricingRule
                {
                    Name = txtRuleName.Text.Trim(),
                    DayType = txtDayType.Text.Trim(),
                    SeatType = cboSeatType.SelectedItem?.ToString(),
                    ExtraFee = numExtraFee.Value
                });
            });

            btnUpdate.Click += (s, e) => Execute(() =>
            {
                if (_selectedId == 0) throw new Exception("Vui lòng chọn quy tắc cần sửa.");
                _pricingService.UpdateRule(new PricingRule
                {
                    Id = _selectedId,
                    Name = txtRuleName.Text.Trim(),
                    DayType = txtDayType.Text.Trim(),
                    SeatType = cboSeatType.SelectedItem?.ToString(),
                    ExtraFee = numExtraFee.Value
                });
            });

            btnDelete.Click += (s, e) => Execute(() =>
            {
                if (_selectedId == 0) return;
                _pricingService.DeleteRule(_selectedId);
                _selectedId = 0;
            });

            grp.Controls.AddRange(new Control[] { txtRuleName, txtDayType, cboSeatType, numExtraFee, btnAdd, btnUpdate, btnDelete });

            dgvRules = new DataGridView
            {
                Location = new Point(15, 150),
                Size = new Size(815, 290),
                ReadOnly = true,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgvRules.CellClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && dgvRules.Rows[e.RowIndex].DataBoundItem is PricingRule r)
                {
                    _selectedId = r.Id;
                    txtRuleName.Text = r.Name;
                    txtDayType.Text = r.DayType;
                    cboSeatType.SelectedItem = r.SeatType;
                    numExtraFee.Value = r.ExtraFee;
                }
            };

            Controls.Add(grp);
            Controls.Add(dgvRules);
        }

        private void Execute(Action action)
        {
            try { action(); LoadRules(); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }

        private void LoadRules() => dgvRules.DataSource = _pricingService.GetAllRules().ToList();
    }
}