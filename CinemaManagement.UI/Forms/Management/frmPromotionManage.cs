// File: CinemaManagement.UI/Forms/Management/frmPromotionManage.cs
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
        private TextBox txtCode, txtDescription;
        private NumericUpDown numDiscountPercent, numMaxDiscount;
        private DateTimePicker dtpStartDate, dtpEndDate;
        private CheckBox chkActive;
        private DataGridView dgvPromotions;
        private int _selectedId = 0;

        public frmPromotionManage(IPromotionService promotionService)
        {
            _promotionService = promotionService;
            InitializeComponent();
            SetupLayout();
            LoadPromotions();
        }

        private void SetupLayout()
        {
            if (Controls.Count > 0) return;

            Text = "Quản Lý Khuyến Mãi";
            Size = new Size(900, 540);
            StartPosition = FormStartPosition.CenterParent;

            var grp = new GroupBox { Text = "Thông Tin Mã Giảm Giá", Location = new Point(15, 15), Size = new Size(855, 165) };

            grp.Controls.Add(new Label { Text = "Mã Code:", Location = new Point(20, 30), AutoSize = true });
            txtCode = new TextBox { Location = new Point(95, 27), Width = 150 };

            grp.Controls.Add(new Label { Text = "Giảm (%):", Location = new Point(265, 30), AutoSize = true });
            numDiscountPercent = new NumericUpDown { Location = new Point(335, 27), Width = 75, Minimum = 1, Maximum = 100, Value = 10 };

            grp.Controls.Add(new Label { Text = "Tối Đa (VNĐ):", Location = new Point(430, 30), AutoSize = true });
            numMaxDiscount = new NumericUpDown { Location = new Point(525, 27), Width = 120, Maximum = 1000000, Increment = 5000, Value = 50000 };

            chkActive = new CheckBox { Text = "Kích Hoạt", Location = new Point(670, 29), Checked = true, AutoSize = true };

            grp.Controls.Add(new Label { Text = "Bắt Đầu:", Location = new Point(20, 68), AutoSize = true });
            dtpStartDate = new DateTimePicker { Location = new Point(95, 65), Width = 150, Format = DateTimePickerFormat.Short };

            grp.Controls.Add(new Label { Text = "Kết Thúc:", Location = new Point(265, 68), AutoSize = true });
            dtpEndDate = new DateTimePicker { Location = new Point(335, 65), Width = 150, Format = DateTimePickerFormat.Short, Value = DateTime.Now.AddDays(30) };

            grp.Controls.Add(new Label { Text = "Mô Tả:", Location = new Point(20, 105), AutoSize = true });
            txtDescription = new TextBox { Location = new Point(95, 102), Width = 550 };

            var btnAdd = new Button { Text = "Thêm Mã", Location = new Point(95, 132), Size = new Size(100, 26) };
            var btnUpdate = new Button { Text = "Cập Nhật", Location = new Point(205, 132), Size = new Size(100, 26) };
            var btnDelete = new Button { Text = "Xóa", Location = new Point(315, 132), Size = new Size(85, 26) };

            btnAdd.Click += (s, e) => Execute(() =>
            {
                _promotionService.AddPromotion(new Promotion
                {
                    Code = txtCode.Text.Trim().ToUpper(),
                    DiscountPercent = numDiscountPercent.Value,
                    MaxDiscountAmount = numMaxDiscount.Value,
                    StartDate = dtpStartDate.Value.Date,
                    EndDate = dtpEndDate.Value.Date.AddDays(1).AddSeconds(-1),
                    Description = txtDescription.Text.Trim(),
                    IsActive = chkActive.Checked
                });
            });

            btnUpdate.Click += (s, e) => Execute(() =>
            {
                if (_selectedId == 0) throw new Exception("Vui lòng chọn mã khuyến mãi cần sửa.");
                _promotionService.UpdatePromotion(new Promotion
                {
                    Id = _selectedId,
                    Code = txtCode.Text.Trim().ToUpper(),
                    DiscountPercent = numDiscountPercent.Value,
                    MaxDiscountAmount = numMaxDiscount.Value,
                    StartDate = dtpStartDate.Value.Date,
                    EndDate = dtpEndDate.Value.Date.AddDays(1).AddSeconds(-1),
                    Description = txtDescription.Text.Trim(),
                    IsActive = chkActive.Checked
                });
            });

            btnDelete.Click += (s, e) => Execute(() =>
            {
                if (_selectedId == 0) return;
                _promotionService.DeletePromotion(_selectedId);
                _selectedId = 0;
            });

            grp.Controls.AddRange(new Control[] { txtCode, numDiscountPercent, numMaxDiscount, chkActive, dtpStartDate, dtpEndDate, txtDescription, btnAdd, btnUpdate, btnDelete });

            dgvPromotions = new DataGridView
            {
                Location = new Point(15, 195),
                Size = new Size(855, 290),
                ReadOnly = true,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgvPromotions.CellClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && dgvPromotions.Rows[e.RowIndex].DataBoundItem is Promotion p)
                {
                    _selectedId = p.Id;
                    txtCode.Text = p.Code;
                    numDiscountPercent.Value = p.DiscountPercent;
                    numMaxDiscount.Value = p.MaxDiscountAmount;
                    dtpStartDate.Value = p.StartDate;
                    dtpEndDate.Value = p.EndDate;
                    txtDescription.Text = p.Description;
                    chkActive.Checked = p.IsActive;
                }
            };

            Controls.Add(grp);
            Controls.Add(dgvPromotions);
        }

        private void Execute(Action action)
        {
            try { action(); LoadPromotions(); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }

        private void LoadPromotions() => dgvPromotions.DataSource = _promotionService.GetAllPromotions().ToList();
    }
}