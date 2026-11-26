using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.BLL.Session;
using CinemaManagement.Core.Entities;
using CinemaManagement.Core.Exceptions;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.UI.Controls;
using CinemaManagement.UI.Helpers;

namespace CinemaManagement.UI.Forms.Catalog;

public partial class frmGenreManage : UserControl
{
    private readonly IGenreService _genreService;

    private TextBox txtName = null!;
    private EmptyStateDataGridView dgvGenres = null!;
    private Label lblTotal = null!;
    private Button _updateButton = null!;
    private Button _deleteButton = null!;

    private int _selectedId;

    public frmGenreManage(
        IGenreService genreService,
        CurrentUserSession session)
    {
        if (session is null || !session.IsAdmin)
        {
            throw new BusinessException(
                "Bạn không có quyền quản lý thể loại phim.");
        }

        _genreService = genreService
            ?? throw new ArgumentNullException(nameof(genreService));

        BuildLayout();
        LoadGenres();

        ThemeHelper.Apply(this);
    }

    private void BuildLayout()
    {
        Text = "Quản lý thể loại phim";
        BackColor = ThemeHelper.Background;
        Font = ThemeHelper.BodyFont;

        var header = new PageHeader(
            "Quản lý thể loại",
            "Tạo và duy trì thể loại dùng để phân loại phim.");

        var inputPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 138,
            Padding = new Padding(24, 9, 24, 8),
            BackColor = ThemeHelper.Surface,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoScroll = true
        };

        var nameField = new Panel
        {
            Width = 300,
            Height = 68,
            Margin = new Padding(0, 3, 12, 3),
            BackColor = ThemeHelper.Surface
        };

        var nameLabel = new Label
        {
            Text = "Tên thể loại",
            Dock = DockStyle.Top,
            Height = 25,
            Font = ThemeHelper.SectionFont,
            ForeColor = ThemeHelper.Text
        };

        txtName = new TextBox
        {
            Dock = DockStyle.Bottom,
            Height = 34,
            MaxLength = 100,
            PlaceholderText = "Nhập tên thể loại...",
            AccessibleName = "Tên thể loại"
        };

        nameField.Controls.Add(txtName);
        nameField.Controls.Add(nameLabel);

        var btnAdd = CreateButton(
            "Thêm mới",
            ThemeHelper.Accent,
            370,
            40,
            105);

        _updateButton = CreateButton(
            "Cập nhật",
            Color.FromArgb(71, 85, 105),
            485,
            40,
            105);

        _deleteButton = CreateButton(
            "Xóa",
            ThemeHelper.Danger,
            600,
            40,
            85);

        var btnClear = CreateButton(
            "Làm mới",
            Color.FromArgb(100, 116, 139),
            695,
            40,
            100);

        btnAdd.Click += (_, _) => AddGenre();
        _updateButton.Enabled = false;
        _deleteButton.Enabled = false;
        _updateButton.Click += (_, _) => UpdateGenre();
        _deleteButton.Click += (_, _) => DeleteGenre();
        btnClear.Click += (_, _) => ResetForm();

        foreach (var button in new[] { btnAdd, _updateButton, _deleteButton, btnClear })
        {
            button.Margin = new Padding(4, 19, 4, 3);
            button.FlatAppearance.BorderSize = 0;
            button.UseVisualStyleBackColor = false;
        }

        inputPanel.Controls.AddRange(new Control[]
        {
            nameField,
            btnAdd,
            _updateButton,
            _deleteButton,
            btnClear
        });

        var gridPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24, 15, 24, 10),
            BackColor = ThemeHelper.Background
        };

        lblTotal = new Label
        {
            Dock = DockStyle.Top,
            Height = 35,
            Text = "Tổng số thể loại: 0",
            Font = ThemeHelper.SectionFont,
            ForeColor = ThemeHelper.Primary,
            TextAlign = ContentAlignment.MiddleLeft
        };

        dgvGenres = new EmptyStateDataGridView
        {
            EmptyStateText = "Chưa có thể loại nào. Thêm thể loại đầu tiên để phân loại phim.",
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false
        };

        dgvGenres.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Id",
            HeaderText = "MÃ THỂ LOẠI",
            DataPropertyName = nameof(Genre.Id),
            FillWeight = 20
        });

        dgvGenres.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Name",
            HeaderText = "TÊN THỂ LOẠI",
            DataPropertyName = nameof(Genre.Name),
            FillWeight = 80
        });

        dgvGenres.CellClick += DgvGenres_CellClick;

        dgvGenres.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0)
                SelectCurrentGenre();
        };

        gridPanel.Controls.Add(dgvGenres);
        gridPanel.Controls.Add(lblTotal);

        Controls.Add(gridPanel);
        Controls.Add(inputPanel);
        Controls.Add(header);
    }

    private static Button CreateButton(
        string text,
        Color backColor,
        int x,
        int y,
        int width)
    {
        var button = new Button
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(width, 34),
            BackColor = backColor,
            ForeColor = backColor == ThemeHelper.Accent
                ? ThemeHelper.Primary
                : Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        ThemeHelper.StyleActionButton(button, backColor);
        return button;
    }

    private void LoadGenres()
    {
        try
        {
            var genres = _genreService
                .GetAll()
                .OrderBy(x => x.Name)
                .ToList();

            dgvGenres.DataSource = null;
            dgvGenres.DataSource = genres;

            lblTotal.Text = $"Tổng số thể loại: {genres.Count}";

            if (genres.Count == 0)
                ResetForm();
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);
        }
    }

    private void DgvGenres_CellClick(
        object? sender,
        DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0)
            return;

        SelectCurrentGenre();
    }

    private void SelectCurrentGenre()
    {
        if (dgvGenres.CurrentRow?.DataBoundItem is not Genre genre)
            return;

        _selectedId = genre.Id;
        _updateButton.Enabled = true;
        _deleteButton.Enabled = true;
        txtName.Text = genre.Name;
    }

    private void AddGenre()
    {
        try
        {
            var name = txtName.Text.Trim();

            if (!ValidateName(name))
                return;

            var genre = new Genre
            {
                Name = name
            };

            _genreService.Create(genre);

            MessageHelper.Info(
                "Thêm thể loại thành công.");

            ResetForm();
            LoadGenres();
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);
        }
    }

    private void UpdateGenre()
    {
        try
        {
            if (_selectedId <= 0)
            {
                MessageHelper.Warning(
                    "Vui lòng chọn thể loại cần cập nhật.");

                return;
            }

            var name = txtName.Text.Trim();

            if (!ValidateName(name))
                return;

            var genre = new Genre
            {
                Id = _selectedId,
                Name = name
            };

            _genreService.Update(genre);

            MessageHelper.Info(
                "Cập nhật thể loại thành công.");

            ResetForm();
            LoadGenres();
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);
        }
    }

    private void DeleteGenre()
    {
        try
        {
            if (_selectedId <= 0)
            {
                MessageHelper.Warning(
                    "Vui lòng chọn thể loại cần xóa.");

                return;
            }

            if (!MessageHelper.Confirm(
                    $"Bạn có chắc chắn muốn xóa thể loại \"{txtName.Text}\"?"))
            {
                return;
            }

            _genreService.Delete(_selectedId);

            MessageHelper.Info(
                "Xóa thể loại thành công.");

            ResetForm();
            LoadGenres();
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);
        }
    }

    private bool ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageHelper.Warning(
                "Vui lòng nhập tên thể loại.");

            txtName.Focus();
            return false;
        }

        if (name.Length > 100)
        {
            MessageHelper.Warning(
                "Tên thể loại không được vượt quá 100 ký tự.");

            txtName.Focus();
            return false;
        }

        return true;
    }

    private void ResetForm()
    {
        _selectedId = 0;
        _updateButton.Enabled = false;
        _deleteButton.Enabled = false;
        txtName.Clear();
        dgvGenres.ClearSelection();
        txtName.Focus();
    }
}
