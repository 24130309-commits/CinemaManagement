
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
        private int _selectedId;

        public frmRoomManage(IRoomService roomService)
        {
            _roomService = roomService
                ?? throw new ArgumentNullException(nameof(roomService));

            InitializeComponent();
            BuildLayout();
            LoadRooms();
        }

        private void BuildLayout()
        {
            Text = "Quản lý phòng chiếu";
            Size = new Size(880, 520);
            StartPosition = FormStartPosition.CenterParent;

            var group = new GroupBox
            {
                Text = "Thông tin phòng chiếu",
                Location = new Point(15, 15),
                Size = new Size(835, 130)
            };

            group.Controls.Add(new Label
            {
                Text = "Tên phòng:",
                Location = new Point(20, 30),
                AutoSize = true
            });

            txtName = new TextBox
            {
                Location = new Point(100, 27),
                Width = 220
            };

            chkActive = new CheckBox
            {
                Text = "Đang hoạt động",
                Location = new Point(350, 29),
                Checked = true,
                AutoSize = true
            };

            group.Controls.Add(new Label
            {
                Text = "Số hàng ghế:",
                Location = new Point(20, 68),
                AutoSize = true
            });

            numRows = new NumericUpDown
            {
                Location = new Point(110, 65),
                Width = 70,
                Minimum = 1,
                Maximum = 26,
                Value = 10
            };

            group.Controls.Add(new Label
            {
                Text = "Số cột ghế:",
                Location = new Point(200, 68),
                AutoSize = true
            });

            numCols = new NumericUpDown
            {
                Location = new Point(280, 65),
                Width = 70,
                Minimum = 1,
                Maximum = 30,
                Value = 12
            };

            var btnAdd = new Button
            {
                Text = "Thêm phòng",
                Location = new Point(380, 63),
                Size = new Size(105, 30)
            };

            var btnUpdate = new Button
            {
                Text = "Cập nhật",
                Location = new Point(495, 63),
                Size = new Size(95, 30)
            };

            var btnDelete = new Button
            {
                Text = "Xóa",
                Location = new Point(600, 63),
                Size = new Size(80, 30)
            };

            btnAdd.Click += (s, e) => Execute(() =>
            {
                _roomService.AddRoom(CreateRoom());
                ResetForm();
                MessageBox.Show("Thêm phòng chiếu thành công.");
            });

            btnUpdate.Click += (s, e) => Execute(() =>
            {
                if (_selectedId == 0)
                    throw new InvalidOperationException(
                        "Vui lòng chọn phòng cần sửa.");

                var room = CreateRoom();
                room.Id = _selectedId;

                _roomService.UpdateRoom(room);
                MessageBox.Show("Cập nhật phòng chiếu thành công.");
            });

            btnDelete.Click += (s, e) => Execute(() =>
            {
                if (_selectedId == 0)
                    throw new InvalidOperationException(
                        "Vui lòng chọn phòng cần xóa.");

                if (MessageBox.Show(
                    "Bạn có chắc chắn muốn xóa phòng này?",
                    "Xác nhận",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes)
                    return;

                _roomService.DeleteRoom(_selectedId);
                ResetForm();
            });

            group.Controls.AddRange(new Control[]
            {
                txtName, chkActive, numRows, numCols,
                btnAdd, btnUpdate, btnDelete
            });

            dgvRooms = new DataGridView
            {
                Location = new Point(15, 160),
                Size = new Size(835, 300),
                ReadOnly = true,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                MultiSelect = false
            };

            dgvRooms.CellClick += DgvRooms_CellClick;

            Controls.Add(group);
            Controls.Add(dgvRooms);
        }

        private Room CreateRoom()
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
                throw new InvalidOperationException(
                    "Vui lòng nhập tên phòng.");

            return new Room
            {
                Name = txtName.Text.Trim(),
                TotalRows = (int)numRows.Value,
                TotalCols = (int)numCols.Value,
                IsActive = chkActive.Checked
            };
        }

        private void DgvRooms_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 ||
                dgvRooms.Rows[e.RowIndex].DataBoundItem is not Room room)
                return;

            _selectedId = room.Id;
            txtName.Text = room.Name;
            numRows.Value = Math.Max(
                numRows.Minimum,
                Math.Min(numRows.Maximum, room.TotalRows));

            numCols.Value = Math.Max(
                numCols.Minimum,
                Math.Min(numCols.Maximum, room.TotalCols));

            chkActive.Checked = room.IsActive;
        }

        private void Execute(Action action)
        {
            try
            {
                action();
                LoadRooms();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Thông báo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void LoadRooms()
        {
            dgvRooms.DataSource = _roomService.GetAllRooms().ToList();
        }

        private void ResetForm()
        {
            _selectedId = 0;
            txtName.Clear();
            numRows.Value = 10;
            numCols.Value = 12;
            chkActive.Checked = true;
            dgvRooms.ClearSelection();
        }
    }
}
