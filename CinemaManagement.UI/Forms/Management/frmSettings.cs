﻿
using CinemaManagement.Core.Constants;
using CinemaManagement.Core.Interfaces;
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

    private readonly TextBox _cinema = new() { Width = 300 };

    private readonly DataGridView _logs = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AutoGenerateColumns = true
    };

    private readonly DateTimePicker _from = new()
    {
        Value = DateTime.Today.AddDays(-7),
        Width = 150
    };

    private readonly DateTimePicker _to = new()
    {
        Value = DateTime.Today,
        Width = 150
    };

    private readonly ComboBox _userFilter = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = 180
    };

    public frmSettings(
        ISettingService settings,
        IBackupService backup,
        IAuditService audit,
        IUserService users)
    {
        _settings = settings;
        _backup = backup;
        _audit = audit;
        _users = users;

        InitializeComponent();

        Text = "Cài đặt hệ thống";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(980, 680);
        Size = new Size(1050, 720);

        var tabs = new TabControl { Dock = DockStyle.Fill };

        var config = new TabPage("Cấu hình")
        {
            BackColor = ThemeHelper.Surface,
            Padding = new Padding(18)
        };

        var backupPage = new TabPage("Sao lưu / Khôi phục")
        {
            BackColor = ThemeHelper.Surface,
            Padding = new Padding(18)
        };

        var auditPage = new TabPage("Nhật ký thao tác")
        {
            BackColor = ThemeHelper.Surface,
            Padding = new Padding(18)
        };

        BuildConfig(config);
        BuildBackup(backupPage);
        BuildAudit(auditPage);

        tabs.TabPages.AddRange([config, backupPage, auditPage]);

        Controls.Add(tabs);

        Load += (_, _) =>
        {
            LoadSettings();
            LoadUserFilter();
        };

        ThemeHelper.Apply(this);
    }

    private void BuildConfig(TabPage page)
    {
        page.Controls.Add(new Label
        {
            Text = "THIẾT LẬP VẬN HÀNH",
            Location = new Point(35, 8),
            Size = new Size(360, 28),
            Font = ThemeHelper.SectionFont,
            ForeColor = ThemeHelper.Primary
        });

        AddLabel(page, "Tên rạp", 35, 48);

        _cinema.Location = new Point(220, 68);
        page.Controls.Add(_cinema);

        AddLabel(page, "Thời gian giữ ghế (phút)", 35, 116);

        _hold.Location = new Point(220, 136);
        page.Controls.Add(_hold);

        AddLabel(page, "Thời gian dọn phòng (phút)", 35, 184);

        _clean.Location = new Point(220, 204);
        page.Controls.Add(_clean);

        var save = new Button
        {
            Text = "Lưu cấu hình",
            Location = new Point(220, 264),
            Size = new Size(140, 40)
        };

        save.Click += (_, _) =>
        {
            try
            {
                _settings.Set(
                    AppConstants.SettingCinemaName,
                    _cinema.Text);

                _settings.Set(
                    AppConstants.SettingHoldMinutes,
                    ((int)_hold.Value).ToString());

                _settings.Set(
                    AppConstants.SettingCleaningMinutes,
                    ((int)_clean.Value).ToString());

                _audit.Log(
                    "SETTING_UPDATE",
                    "Cập nhật cấu hình hệ thống.");

                MessageHelper.Info("Đã lưu cấu hình.");
            }
            catch (Exception ex)
            {
                MessageHelper.Handle(ex);
            }
        };

        page.Controls.Add(save);
    }

    private void BuildBackup(TabPage page)
    {
        page.Controls.Add(new Label
        {
            Text = "SAO LƯU VÀ KHÔI PHỤC DỮ LIỆU",
            Location = new Point(35, 8),
            Size = new Size(430, 28),
            Font = ThemeHelper.SectionFont,
            ForeColor = ThemeHelper.Primary
        });

        var export = new Button
        {
            Text = "Sao lưu cơ sở dữ liệu",
            Location = new Point(35, 52),
            Size = new Size(210, 42)
        };

        export.Click += (_, _) =>
        {
            using var dialog = new SaveFileDialog
            {
                Filter = "SQLite database (*.db)|*.db",
                FileName = $"cinema_backup_{DateTime.Now:yyyyMMdd_HHmm}.db"
            };

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                _backup.Backup(dialog.FileName);

                _audit.Log(
                    "DATABASE_BACKUP",
                    "Tạo bản sao lưu cơ sở dữ liệu.");

                MessageHelper.Info("Sao lưu thành công.");
            }
            catch (Exception ex)
            {
                MessageHelper.Handle(ex);
            }
        };

        var restore = new Button
        {
            Text = "Khôi phục từ bản sao lưu",
            Location = new Point(35, 117),
            Size = new Size(210, 42)
        };

        restore.Click += (_, _) =>
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "SQLite database (*.db)|*.db|Tất cả tệp (*.*)|*.*"
            };

            if (dialog.ShowDialog() != DialogResult.OK ||
                !MessageHelper.Confirm(
                    "Khôi phục sẽ thay thế dữ liệu hiện tại. Bạn đã sao lưu dữ liệu mới nhất chưa?"))
            {
                return;
            }

            try
            {
                _backup.Restore(dialog.FileName);

                _audit.Log(
                    "DATABASE_RESTORE",
                    "Khôi phục cơ sở dữ liệu từ bản sao lưu.");

                MessageHelper.Info(
                    "Khôi phục thành công. Vui lòng khởi động lại ứng dụng.");
            }
            catch (Exception ex)
            {
                MessageHelper.Handle(ex);
            }
        };

        page.Controls.AddRange([export, restore]);
    }

    private void BuildAudit(TabPage page)
    {
        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 64,
            Padding = new Padding(12, 10, 8, 8),
            WrapContents = false,
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
        page.Controls.Add(bar);
    }

    private void LoadSettings()
    {
        try
        {
            _cinema.Text = _settings.GetCinemaName();
            _hold.Value = _settings.GetHoldMinutes();
            _clean.Value = _settings.GetCleaningMinutes();
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

            foreach (var user in _users.GetAll())
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
            var selected = _userFilter.SelectedItem as UserFilterItem;

            _logs.DataSource = _audit.Search(
                selected?.Id ?? 0,
                _from.Value.Date,
                _to.Value.Date).ToList();

            SetAuditColumn("Id", "Mã nhật ký", false);
            SetAuditColumn("UserId", "Mã người dùng");
            SetAuditColumn("Action", "Thao tác");
            SetAuditColumn("Detail", "Chi tiết");

            SetAuditColumn(
                "CreatedAt",
                "Thời gian",
                true,
                "dd/MM/yyyy HH:mm:ss");
        }
        catch (Exception ex)
        {
            MessageHelper.Handle(ex);
        }
    }

    private void SetAuditColumn(
        string name,
        string header,
        bool visible = true,
        string? format = null)
    {
        if (!_logs.Columns.Contains(name))
            return;

        var column = _logs.Columns[name]!;

        column.HeaderText = header;
        column.Visible = visible;

        if (format is not null)
            column.DefaultCellStyle.Format = format;
    }

    private sealed record UserFilterItem(int Id, string Name)
    {
        public override string ToString() => Name;
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
}