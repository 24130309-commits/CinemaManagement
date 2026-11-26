using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using CinemaManagement.Core.Constants;
using CinemaManagement.Core.Interfaces;
using CinemaManagement.UI.Controls;
using CinemaManagement.UI.Helpers;

namespace CinemaManagement.UI.Forms.Management;

public partial class frmSettings : Form
{
    private readonly ISettingService _settings;
    private readonly IBackupService _backup;
    private readonly IAuditService _audit;
    private readonly IUserService _users;

    private readonly NumericUpDown _hold = new()
    {
        Minimum = 1,
        Maximum = 30,
        Width = 120
    };

    private readonly NumericUpDown _clean = new()
    {
        Minimum = 0,
        Maximum = 180,
        Width = 120
    };

    private readonly TextBox _cinema = new()
    {
        Width = 300
    };

    private readonly EmptyStateDataGridView _logs = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AutoGenerateColumns = true,
        AllowUserToAddRows = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
    };

    private readonly DateTimePicker _from = new()
    {
        Value = DateTime.Today.AddDays(-7),
        Width = 150,
        Format = DateTimePickerFormat.Short
    };

    private readonly DateTimePicker _to = new()
    {
        Value = DateTime.Today,
        Width = 150,
        Format = DateTimePickerFormat.Short
    };

    private readonly ComboBox _userFilter = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = 180
    };

    private readonly Label _auditStatus = new()
    {
        Dock = DockStyle.Bottom,
        Height = 30,
        TextAlign = ContentAlignment.MiddleLeft,
        ForeColor = ThemeHelper.Muted
    };

    public frmSettings(
        ISettingService settings,
        IBackupService backup,
        IAuditService audit,
        IUserService users)
    {
        _settings = settings
            ?? throw new ArgumentNullException(nameof(settings));

        _backup = backup
            ?? throw new ArgumentNullException(nameof(backup));

        _audit = audit
            ?? throw new ArgumentNullException(nameof(audit));

        _users = users
            ?? throw new ArgumentNullException(nameof(users));

        InitializeComponent();

        Text = "Cài đặt hệ thống";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(760, 560);
        Size = new Size(1050, 720);
        _logs.EmptyStateText = "Không có nhật ký nào trong bộ lọc thời gian đã chọn.";

        var tabs = new TabControl
        {
            Dock = DockStyle.Fill
        };

        var config = new TabPage("Cấu hình")
        {
            Padding = new Padding(8)
        };

        var backupPage = new TabPage("Sao lưu / Khôi phục")
        {
            Padding = new Padding(8)
        };

        var auditPage = new TabPage("Nhật ký thao tác")
        {
            Padding = new Padding(8)
        };

        BuildConfig(config);
        BuildBackup(backupPage);
        BuildAudit(auditPage);

        tabs.TabPages.AddRange(new[]
        {
            config,
            backupPage,
            auditPage
        });

        Controls.Add(tabs);
        Controls.Add(new PageHeader(
            "Cài đặt hệ thống",
            "Cấu hình vận hành, sao lưu an toàn và xem nhật ký quản trị."));

        Load += (_, _) =>
        {
            LoadSettings();
            LoadUserFilter();
            LoadAudit();
        };

        ThemeHelper.Apply(this);
    }

    private void BuildConfig(TabPage page)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(18),
            BackColor = ThemeHelper.Surface
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));

        var title = new Label
        {
            Text = "Thiết lập vận hành",
            Dock = DockStyle.Fill,
            Font = ThemeHelper.SectionFont,
            ForeColor = ThemeHelper.Primary,
            TextAlign = ContentAlignment.MiddleLeft
        };
        layout.Controls.Add(title, 0, 0);
        layout.Controls.Add(CreateSettingField("Tên rạp", _cinema), 0, 1);
        layout.Controls.Add(CreateSettingField("Thời gian giữ ghế (phút)", _hold), 0, 2);
        layout.Controls.Add(CreateSettingField("Thời gian dọn phòng (phút)", _clean), 0, 3);

        var save = new Button
        {
            Text = "Lưu cấu hình",
            Size = new Size(150, 40),
            Anchor = AnchorStyles.Left | AnchorStyles.Top
        };

        save.Click += (_, _) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_cinema.Text))
                {
                    throw new InvalidOperationException(
                        "Tên rạp không được để trống.");
                }

                _settings.SaveOperationalSettings(
                    _cinema.Text,
                    (int)_hold.Value,
                    (int)_clean.Value);

                _audit.Log(
                    "SETTING_UPDATE",
                    "Cập nhật cấu hình hệ thống.");

                MessageHelper.ShowInfo("Đã lưu cấu hình.");
            }
            catch (Exception ex)
            {
                MessageHelper.Handle(ex);
            }
        };

        layout.Controls.Add(save, 0, 4);
        page.Controls.Add(layout);
    }

    private void BuildBackup(TabPage page)
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(18),
            BackColor = ThemeHelper.Surface
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));

        root.Controls.Add(new Label
        {
            Text = "Sao lưu và khôi phục dữ liệu",
            Dock = DockStyle.Fill,
            Font = ThemeHelper.SectionFont,
            ForeColor = ThemeHelper.Primary,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        var export = new Button
        {
            Text = "Sao lưu cơ sở dữ liệu",
            Size = new Size(240, 42),
            Anchor = AnchorStyles.Left
        };

        export.Click += (_, _) =>
        {
            using var dialog = new SaveFileDialog
            {
                Title = "Chọn nơi lưu bản sao lưu",
                Filter = "SQLite database (*.db)|*.db",
                FileName = $"cinema_backup_{DateTime.Now:yyyyMMdd_HHmm}.db",
                RestoreDirectory = true
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                _backup.BackupDatabase(dialog.FileName);

                _audit.Log(
                    "DATABASE_BACKUP",
                    "Tạo bản sao lưu cơ sở dữ liệu.");

                MessageHelper.ShowInfo("Sao lưu thành công.");
            }
            catch (Exception ex)
            {
                MessageHelper.Handle(ex);
            }
        };

        var restore = new Button
        {
            Text = "Khôi phục từ bản sao lưu",
            Size = new Size(240, 42),
            Anchor = AnchorStyles.Left
        };

        restore.Click += (_, _) =>
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Chọn bản sao lưu để khôi phục",
                Filter = "SQLite database (*.db)|*.db|Tất cả tệp (*.*)|*.*",
                RestoreDirectory = true
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            if (!MessageHelper.Confirm(
                    "Khôi phục sẽ thay thế dữ liệu hiện tại. Bạn đã sao lưu dữ liệu mới nhất chưa?"))
            {
                return;
            }

            try
            {
                _backup.RestoreDatabase(dialog.FileName);

                MessageHelper.ShowInfo(
                    "Khôi phục thành công. Vui lòng khởi động lại ứng dụng.");

                Application.Restart();
            }
            catch (Exception ex)
            {
                MessageHelper.Handle(ex);
            }
        };

        root.Controls.Add(export, 0, 1);
        root.Controls.Add(restore, 0, 2);
        page.Controls.Add(root);
    }

    private void BuildAudit(TabPage page)
    {
        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 112,
            Padding = new Padding(8),
            WrapContents = true,
            AutoScroll = true,
            BackColor = ThemeHelper.Surface
        };

        bar.Controls.Add(new Label
        {
            Text = "NHẬT KÝ",
            AutoSize = true,
            Font = ThemeHelper.SectionFont,
            ForeColor = ThemeHelper.Primary,
            Padding = new Padding(0, 8, 12, 0)
        });

        bar.Controls.Add(new Label
        {
            Text = "Người dùng",
            AutoSize = true,
            Padding = new Padding(0, 8, 0, 0)
        });

        bar.Controls.Add(_userFilter);

        bar.Controls.Add(new Label
        {
            Text = "Từ ngày",
            AutoSize = true,
            Padding = new Padding(0, 8, 0, 0)
        });

        bar.Controls.Add(_from);

        bar.Controls.Add(new Label
        {
            Text = "Đến ngày",
            AutoSize = true,
            Padding = new Padding(0, 8, 0, 0)
        });

        bar.Controls.Add(_to);

        var search = new Button
        {
            Text = "Lọc",
            AutoSize = true
        };

        search.Click += (_, _) => LoadAudit();

        bar.Controls.Add(search);

        page.Controls.Add(_logs);
        page.Controls.Add(_auditStatus);
        page.Controls.Add(bar);
    }

    private void LoadSettings()
    {
        try
        {
            _cinema.Text = _settings.GetValue(
                AppConstants.SettingCinemaName,
                AppConstants.DefaultCinemaName);

            var holdText = _settings.GetValue(
                AppConstants.SettingHoldMinutes,
                AppConstants.DefaultHoldMinutes.ToString(CultureInfo.InvariantCulture));

            var cleanText = _settings.GetValue(
                AppConstants.SettingCleaningMinutes,
                AppConstants.DefaultCleaningMinutes.ToString(CultureInfo.InvariantCulture));

            if (int.TryParse(
                    holdText,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var holdMinutes))
            {
                _hold.Value = Math.Max(
                    _hold.Minimum,
                    Math.Min(_hold.Maximum, holdMinutes));
            }

            if (int.TryParse(
                    cleanText,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var cleaningMinutes))
            {
                _clean.Value = Math.Max(
                    _clean.Minimum,
                    Math.Min(_clean.Maximum, cleaningMinutes));
            }
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);
        }
    }

    private void LoadUserFilter()
    {
        try
        {
            _userFilter.Items.Clear();

            _userFilter.Items.Add(
                new UserFilterItem(0, "Tất cả người dùng"));

            foreach (var user in _users.GetAllUsers())
            {
                _userFilter.Items.Add(
                    new UserFilterItem(
                        user.Id,
                        $"{user.FullName} ({user.Username})"));
            }

            _userFilter.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);
        }
    }

    private void LoadAudit()
    {
        try
        {
            if (_to.Value.Date < _from.Value.Date)
            {
                throw new InvalidOperationException(
                    "Ngày kết thúc không được trước ngày bắt đầu.");
            }

            var selected = _userFilter.SelectedItem as UserFilterItem;

            _logs.DataSource = _audit.Search(
                selected?.Id ?? 0,
                _from.Value.Date,
                _to.Value.Date.AddDays(1).AddTicks(-1))
                .ToList();

            _auditStatus.Text = $"{_logs.Rows.Count:N0} mục nhật ký phù hợp";

            SetAuditColumn("Id", "Mã nhật ký", false);
            SetAuditColumn("UserId", "Mã người dùng");
            SetAuditColumn("Username", "Người thực hiện");
            SetAuditColumn("Action", "Thao tác");
            SetAuditColumn("EntityName", "Đối tượng");
            SetAuditColumn("EntityId", "Mã đối tượng");
            SetAuditColumn("Detail", "Chi tiết");
            SetAuditColumn("Details", "Chi tiết", false);
            SetAuditColumn(
                "CreatedAt",
                "Thời gian",
                true,
                "dd/MM/yyyy HH:mm:ss");
        }
        catch (Exception ex)
        {
            _auditStatus.Text = "Không thể tải nhật ký theo bộ lọc.";
            MessageHelper.Handle(ex);
        }
    }

    private void SetAuditColumn(
        string name,
        string header,
        bool visible = true,
        string? format = null)
    {
        if (_logs.Columns[name] is not DataGridViewColumn column)
            return;

        column.HeaderText = header;
        column.Visible = visible;

        if (format is not null)
            column.DefaultCellStyle.Format = format;
    }

    private sealed record UserFilterItem(int Id, string Name)
    {
        public override string ToString()
        {
            return Name;
        }
    }

    private static Panel CreateSettingField(string caption, Control input)
    {
        var field = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(2),
            Margin = new Padding(0, 2, 0, 6),
            BackColor = ThemeHelper.Surface
        };

        var label = new Label
        {
            Text = caption,
            Dock = DockStyle.Top,
            Height = 26,
            ForeColor = ThemeHelper.Text,
            Font = ThemeHelper.SectionFont
        };
        input.Dock = DockStyle.Fill;
        field.Controls.Add(input);
        field.Controls.Add(label);
        return field;
    }
}
