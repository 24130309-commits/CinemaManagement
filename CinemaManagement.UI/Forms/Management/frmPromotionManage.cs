
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
        private DataGridView dgvPromotions = null!;

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
            Size = new Size(1000, 700);
            MinimumSize = new Size(950, 650);
            StartPosition = FormStartPosition.CenterParent;

            var group = new GroupBox
            {
                Text = "Thông tin chương trình khuyến mãi",
                Location = new Point(15, 15),
                Size = new Size(950, 290)
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
            cboType.SelectedIndexChanged += CboType_SelectedIndexChanged;

            AddLabel(group, "Giá trị:", 600, 30);

            numValue = new NumericUpDown
            {
                Location = new Point(660, 27),
                Width = 180,
                Minimum = 1,
                Maximum = 100,
                ThousandsSeparator = true
            };

            AddLabel(group, "Giảm tối đa:", 20, 75);

            numMaxDiscount = new NumericUpDown
            {
                Location = new Point(130, 72),
                Width = 180,
                Minimum = 0,
                Maximum = 100000000,
                Increment = 5000,
                ThousandsSeparator = true
            };

            AddLabel(group, "Đơn tối thiểu:", 340, 75);

            numMinOrder = new NumericUpDown
            {
                Location = new Point(440, 72),
                Width = 180,
                Minimum = 0,
                Maximum = 100000000,
                Increment = 5000,
                ThousandsSeparator = true
            };

            AddLabel(group, "Giới hạn lượt dùng:", 650, 75);

            numUsageLimit = new NumericUpDown
            {
                Location = new Point(770, 72),
                Width = 150,
                Minimum = 0,
                Maximum = 1000000,
                Value = 100,
                ThousandsSeparator = true
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
                Location = new Point(440, 117),
                Width = 180,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today.AddDays(30)
            };

            chkActive = new CheckBox
            {
                Text = "Kích hoạt khuyến mãi",
                Location = new Point(650, 120),
                AutoSize = true,
                Checked = true
            };

            AddLabel(group, "Mô tả:", 20, 165);

            txtDescription = new TextBox
            {
                Location = new Point(130, 162),
                Width = 790
            };

            var btnAdd = new Button
            {
                Text = "Thêm mã",
                Location = new Point(130, 215),
                Size = new Size(100, 32)
            };

            var btnUpdate = new Button
            {
                Text = "Cập nhật",
                Location = new Point(245, 215),
                Size = new Size(100, 32)
            };

            var btnDelete = new Button
            {
                Text = "Xóa",
                Location = new Point(360, 215),
                Size = new Size(85, 32)
            };

            var btnReset = new Button
            {
                Text = "Làm mới",
                Location = new Point(460, 215),
                Size = new Size(100, 32)
            };

            btnAdd.Click += (s, e) => Execute(() =>
            {
                _promotionService.AddPromotion(CreatePromotion());
                ResetForm();
            });

            btnUpdate.Click += (s, e) => Execute(() =>
            {
                if (_selectedId == 0)
                {
                    throw new InvalidOperationException(
                        "Vui lòng chọn khuyến mãi cần cập nhật.");
                }

                var promotion = CreatePromotion();
                promotion.Id = _selectedId;

                _promotionService.UpdatePromotion(promotion);
            });

            btnDelete.Click += (s, e) => Execute(() =>
            {
                if (_selectedId == 0)
                {
                    throw new InvalidOperationException(
                        "Vui lòng chọn khuyến mãi cần xóa.");
                }

                var confirm = MessageBox.Show(
                    "Bạn có chắc chắn muốn xóa mã khuyến mãi này?",
                    "Xác nhận",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirm != DialogResult.Yes)
                {
                    return;
                }

                _promotionService.DeletePromotion(_selectedId);
                ResetForm();
            });

            btnReset.Click += (s, e) => ResetForm();

            group.Controls.AddRange(new Control[]
            {
                txtCode,
                cboType,
                numValue,
                numMaxDiscount,
                numMinOrder,
                numUsageLimit,
                dtpStartDate,
                dtpEndDate,
                chkActive,
                txtDescription,
                btnAdd,
                btnUpdate,
                btnDelete,
                btnReset
            });

            dgvPromotions = new DataGridView
            {
                Location = new Point(15, 320),
                Size = new Size(950, 325),
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

        private void CboType_SelectedIndexChanged(
            object? sender,
            EventArgs e)
        {
            if (numValue == null || cboType.SelectedItem == null)
            {
                return;
            }

            var isPercent = cboType.SelectedItem.ToString() == "Percent";
            var newMaximum = isPercent ? 100M : 100000000M;

            numValue.Maximum = newMaximum;

            if (numValue.Value > newMaximum)
            {
                numValue.Value = newMaximum;
            }
        }

        private Promotion CreatePromotion()
        {
            if (string.IsNullOrWhiteSpace(txtCode.Text))
            {
                throw new InvalidOperationException(
                    "Vui lòng nhập mã khuyến mãi.");
            }

            if (cboType.SelectedItem == null)
            {
                throw new InvalidOperationException(
                    "Vui lòng chọn loại giảm giá.");
            }

            if (dtpEndDate.Value.Date < dtpStartDate.Value.Date)
            {
                throw new InvalidOperationException(
                    "Ngày kết thúc không được trước ngày bắt đầu.");
            }

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
            {
                return;
            }

            _selectedId = promotion.Id;

            txtCode.Text = promotion.Code;
            txtDescription.Text = promotion.Description;

            cboType.SelectedItem = promotion.Type;

            numValue.Value = Math.Max(
                numValue.Minimum,
                Math.Min(numValue.Maximum, promotion.Value));

            numMaxDiscount.Value = Math.Max(
                numMaxDiscount.Minimum,
                Math.Min(numMaxDiscount.Maximum, promotion.MaxDiscountAmount));

            numMinOrder.Value = Math.Max(
                numMinOrder.Minimum,
                Math.Min(numMinOrder.Maximum, promotion.MinOrder));

            numUsageLimit.Value = Math.Max(
                numUsageLimit.Minimum,
                Math.Min(numUsageLimit.Maximum, promotion.UsageLimit));

            dtpStartDate.Value = promotion.StartDate;
            dtpEndDate.Value = promotion.EndDate;

            chkActive.Checked = promotion.IsActive;
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
}
