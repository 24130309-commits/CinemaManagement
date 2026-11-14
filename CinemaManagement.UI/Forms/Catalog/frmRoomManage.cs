using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Interfaces;

namespace CinemaManagement.UI.Forms.Catalog
{
    public partial class frmRoomManage : Form
    {
        private readonly IRoomService _roomService;
        private TextBox txtName;
        private NumericUpDown numRows, numCols;
        private CheckBox chkActive;
        private DataGridView dgvRooms;
        private int _selectedId = 0;

        public frmRoomManage(IRoomService roomService)
        {
            _roomService = roomService;
            InitializeComponent();
            SetupLayout();
            LoadRooms();
        }

        private void SetupLayout()
        {
            if (Controls.Count > 0) return;

            Text = "Quản Lý Phòng Chiếu";
            Size = new Size(880, 520);
            StartPosition = FormStartPosition.CenterParent;

            var grp = new GroupBox { Text = "Thông Tin Phòng Chiếu", Location = new Point(15, 15), Size = new Size(835, 130) };

            grp.Controls.Add(new Label { Text = "Tên Phòng:", Location = new Point(20, 30), AutoSize = true });
            txtName = new TextBox { Location = new Point(100, 27), Width = 220 };

            chkActive = new CheckBox { Text = "Đang Hoạt Động", Location = new Point(350, 29), Checked = true, AutoSize = true };

            grp.Controls.Add(new Label { Text = "Số Hàng Ghế:", Location = new Point(20, 68), AutoSize = true });
            numRows = new NumericUpDown { Location = new Point(110, 65), Width = 70, Minimum = 1, Maximum = 26, Value = 10 };

            grp.Controls.Add(new Label { Text = "Ghế / Hàng:", Location = new Point(200, 68), AutoSize = true });
            numCols = new NumericUpDown { Location = new Point(280, 65), Width = 70, Minimum = 1, Maximum = 30, Value = 12 };

            var btnAdd = new Button { Text = "Thêm Phòng", Location = new Point(380, 63), Size = new Size(105, 30) };
            var btnUpdate = new Button { Text = "Cập Nhật", Location = new Point(495, 63), Size = new Size(95, 30) };
            var btnDelete = new Button { Text = "Xóa", Location = new Point(600, 63), Size = new Size(80, 30) };

            btnAdd.Click += (s, e) => Execute(() =>
            {
                _roomService.AddRoom(new Room
                {
                    Name = txtName.Text.Trim(),
                    TotalRows = (int)numRows.Value,
                    SeatsPerRow = (int)numCols.Value,
                    IsActive = chkActive.Checked
                });
                MessageBox.Show("Thêm phòng chiếu thành công!");
            });

            btnUpdate.Click += (s, e) => Execute(() =>
            {
                if (_selectedId == 0) throw new Exception("Vui lòng chọn phòng cần sửa.");
                _roomService.UpdateRoom(new Room
                {
                    Id = _selectedId,
                    Name = txtName.Text.Trim(),
                    TotalRows = (int)numRows.Value,
                    SeatsPerRow = (int)numCols.Value,
                    IsActive = chkActive.Checked
                });
                MessageBox.Show("Cập nhật phòng chiếu thành công!");
            });

            btnDelete.Click += (s, e) => Execute(() =>
            {
                if (_selectedId == 0) return;
                _roomService.DeleteRoom(_selectedId);
                _selectedId = 0;
            });

            grp.Controls.AddRange(new Control[] { txtName, chkActive, numRows, numCols, btnAdd, btnUpdate, btnDelete });

            dgvRooms = new DataGridView
            {
                Location = new Point(15, 160),
                Size = new Size(835, 300),
                ReadOnly = true,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgvRooms.CellClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && dgvRooms.Rows[e.RowIndex].DataBoundItem is Room r)
                {
                    _selectedId = r.Id;
                    txtName.Text = r.Name;
                    numRows.Value = r.TotalRows > 0 ? r.TotalRows : 10;
                    numCols.Value = r.SeatsPerRow > 0 ? r.SeatsPerRow : 12;
                    chkActive.Checked = r.IsActive;
                }
            };

            Controls.Add(grp);
            Controls.Add(dgvRooms);
        }

        private void Execute(Action action)
        {
            try { action(); LoadRooms(); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        }

        private void LoadRooms() => dgvRooms.DataSource = _roomService.GetAllRooms().ToList();
    }
}