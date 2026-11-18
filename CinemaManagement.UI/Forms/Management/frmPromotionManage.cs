
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.UI.Forms.Management
{
    public partial class frmPromotionManage : Form
    {
        private readonly IPromotionService _promotionService;

        private TextBox txtCode;
        private ComboBox cboType;
        private NumericUpDown numValue;
        private NumericUpDown numMinOrder;
        private NumericUpDown numUsageLimit;
        private DateTimePicker dtpStartDate;
        private DateTimePicker dtpEndDate;
        private DataGridView dgvPromotions;

        private int _selectedId;

        public frmPromotionManage(IPromotionService promotionService)
        {
            _promotionService = promotionService
                ?? throw new ArgumentNullException(nameof(promotionService));

            InitializeComponent();
            BuildLayout();
            LoadPromotions();
        }

        private void BuildLayout()
        {
            Controls.Clear();

            Text = "Quản lý khuyến mãi";
            Size = new Size(940, 570);
            StartPosition = FormStartPosition.CenterParent;

            var group = new GroupBox
            {
                Text = "Thông tin chương trình khuyến mãi",
                Location = new Point(15, 15),
                Size = new Size(895, 210)
            };

            AddLabel(group, "Mã khuyến mãi:", 20, 30);
            txtCode = new TextBox
            {
                Location = new Point(130, 27),
                Width = 180,
                CharacterCasing = CharacterCasing.Upper
            };

            AddLabel(group, "Loại giảm:", 340, 30);
            cboType = new ComboBox
            {
                Location = new Point(420, 27),
                Width = 150,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            cboType.Items.AddRange(new object[]
            {
                "Percent",
                "Fixed"
            });
            cboType.SelectedIndex = 0;

            AddLabel(group, "Giá trị:", 600, 30);
            numValue = new NumericUpDown
            {
                Location = new Point(660, 27),
                Width = 150,
                Minimum = 1,
                Maximum = 100000000,
                ThousandsSeparator = true
            };

            AddLabel(group, "Đơn tối thiểu:", 20, 75);
            numMinOrder = new NumericUpDown
            {
                Location = new Point(130, 72),
                Width = 180,
                Minimum = 0,
                Maximum = 100000000,
                Increment = 5000,
                ThousandsSeparator = true
            };

            AddLabel(group, "Giới hạn lượt dùng:", 340, 75);
            numUsageLimit = new NumericUpDown
            {
                Location = new Point(460, 72),
                Width = 150,
                Minimum = 0,
                Maximum = 1000000,
                Value = 100
            };

            AddLabel(group, "Ngày bắt đầu:", 20, 120);
            dtpStartDate = new DateTimePicker
            {
                Location = new Point(130, 117),
                Width = 180,
                Format = DateTimePickerFormat.Short
            };

            AddLabel(group, "Ngày kết thúc:", 340, 120);
            dtpEndDate = new DateTimePicker
            {
                Location = new Point(460, 117),
                Width = 180,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today.AddDays(30)
            };

            var btnAdd = new Button
            {
                Text = "Thêm mã",
                Location = new Point(130, 160),
                Size = new Size(100, 30)
            };

            var btnUpdate = new Button
            {
                Text = "Cập nhật",
                Location = new Point(240, 160),
                Size = new Size(100, 30)
            };

            var btnDelete = new Button
            {
                Text = "Xóa",
                Location = new Point(350, 160),
                Size = new Size(85, 30)
            };

            var btnReset = new Button
            {
                Text = "Làm mới",
                Location = new Point(445, 160),
                Size = new Size(100, 30)
            };

            btnAdd.Click += (s, e) => Execute(() =>
            {
                _promotionService.AddPromotion(CreatePromotion());
                ResetForm();
            });

            btnUpdate.Click += (s, e) => Execute(() =>
            {
                if (_selectedId == 0)
                    throw new InvalidOperationException(
                        "Vui lòng chọn khuyến mãi cần cập nhật.");

                var promotion = CreatePromotion();
                promotion.Id = _selectedId;

                _promotionService.UpdatePromotion(promotion);
            });

            btnDelete.Click += (s, e) => Execute(() =>
            {
                if (_selectedId == 0)
                    throw new InvalidOperationException(
                        "Vui lòng chọn khuyến mãi cần xóa.");

                if (MessageBox.Show(
                    "Bạn có chắc chắn muốn xóa mã khuyến mãi này?",
                    "Xác nhận",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes)
                    return;

                _promotionService.DeletePromotion(_selectedId);
                ResetForm();
            });

            btnReset.Click += (s, e) => ResetForm();

            group.Controls.AddRange(new Control[]
            {
                txtCode, cboType, numValue, numMinOrder,
                numUsageLimit, dtpStartDate, dtpEndDate,
                btnAdd, btnUpdate, btnDelete, btnReset
            });

            dgvPromotions = new DataGridView
            {
                Location = new Point(15, 240),
                Size = new Size(895, 275),
                ReadOnly = true,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                MultiSelect = false
            };

            dgvPromotions.CellClick += DgvPromotions_CellClick;

            Controls.Add(group);
            Controls.Add(dgvPromotions);
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

        private Promotion CreatePromotion()
        {
            if (string.IsNullOrWhiteSpace(txtCode.Text))
                throw new InvalidOperationException(
                    "Vui lòng nhập mã khuyến mãi.");

            if (dtpEndDate.Value.Date < dtpStartDate.Value.Date)
                throw new InvalidOperationException(
                    "Ngày kết thúc không được trước ngày bắt đầu.");

            return new Promotion
            {
                Code = txtCode.Text.Trim().ToUpperInvariant(),
                Type = cboType.SelectedItem?.ToString(),
                Value = numValue.Value,
                MinOrder = numMinOrder.Value,
                UsageLimit = (int)numUsageLimit.Value,
                StartDate = dtpStartDate.Value.Date,
                EndDate = dtpEndDate.Value.Date.AddDays(1).AddTicks(-1)
            };
        }

        private void DgvPromotions_CellClick(
            object sender,
            DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 ||
                dgvPromotions.Rows[e.RowIndex].DataBoundItem is not Promotion promotion)
                return;

            _selectedId = promotion.Id;

            txtCode.Text = promotion.Code;
            cboType.SelectedItem = promotion.Type;

            numValue.Value = Math.Max(
                numValue.Minimum,
                Math.Min(numValue.Maximum, promotion.Value));

            numMinOrder.Value = Math.Max(
                numMinOrder.Minimum,
                Math.Min(numMinOrder.Maximum, promotion.MinOrder));

            numUsageLimit.Value = Math.Max(
                numUsageLimit.Minimum,
                Math.Min(numUsageLimit.Maximum, promotion.UsageLimit));

            dtpStartDate.Value = promotion.StartDate;
            dtpEndDate.Value = promotion.EndDate;
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
                MessageBox.Show(
                    ex.Message,
                    "Lỗi khuyến mãi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void LoadPromotions()
        {
            dgvPromotions.DataSource =
                _promotionService.GetAllPromotions().ToList();
        }

        private void ResetForm()
        {
            _selectedId = 0;
            txtCode.Clear();
            cboType.SelectedIndex = 0;
            numValue.Value = numValue.Minimum;
            numMinOrder.Value = 0;
            numUsageLimit.Value = 100;
            dtpStartDate.Value = DateTime.Today;
            dtpEndDate.Value = DateTime.Today.AddDays(30);
            dgvPromotions.ClearSelection();
        }
    }
}
