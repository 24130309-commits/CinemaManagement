using System.Drawing;
using System.Windows.Forms;

namespace CinemaManagement.UI.Helpers;

public static class ThemeHelper
{

    /// <summary>
    /// Enables per-monitor scaling for forms and user controls whose layouts
    /// are assembled in code instead of a WinForms Designer.
    /// </summary>
    public static void ConfigureDpiScaling(ContainerControl control)
    {
        ArgumentNullException.ThrowIfNull(control);
        control.AutoScaleDimensions = new SizeF(96F, 96F);
        control.AutoScaleMode = AutoScaleMode.Dpi;
    }

    public static readonly Color Primary =
        Color.FromArgb(16, 24, 43); // #10182B

    public static readonly Color PrimaryDark =
        Color.FromArgb(16, 24, 43);

    public static readonly Color Accent =
        Color.FromArgb(255, 138, 50); // Cinema orange #FF8A32

    public static readonly Color AccentHover =
        Color.FromArgb(232, 111, 25);

    public static readonly Color AccentSoft =
        Color.FromArgb(255, 242, 230);

    public static readonly Color Surface =
        Color.White;

    public static readonly Color Background =
        Color.FromArgb(241, 245, 251); // #F1F5FB

    public static readonly Color Text =
        Color.FromArgb(22, 35, 59); // #16233B

    public static readonly Color Muted =
        Color.FromArgb(100, 116, 139); // #64748B

    public static readonly Color Border =
        Color.FromArgb(220, 228, 239); // #DCE4EF

    public static readonly Color HoverSurface =
        Color.FromArgb(247, 249, 252);

    public static readonly Color SelectedSurface =
        Color.FromArgb(255, 242, 230);

    public static readonly Color Focus = Accent;

    public static readonly Color DisabledText =
        Color.FromArgb(148, 163, 184);

    public static readonly Color Success =
        Color.FromArgb(22, 163, 74); // #16A34A

    public static readonly Color SuccessSoft =
        Color.FromArgb(230, 248, 240);

    public static readonly Color Danger =
        Color.FromArgb(220, 38, 38); // #DC2626

    public static readonly Color DangerSoft =
        Color.FromArgb(255, 239, 240);

    public static readonly Color Warning =
        Color.FromArgb(217, 119, 6); // #D97706

    public static readonly Color WarningSoft =
        Color.FromArgb(255, 247, 230);

    public static readonly Color Info =
        Color.FromArgb(37, 99, 235); // #2563EB

    public static readonly Color InfoSoft =
        Color.FromArgb(232, 244, 255);

    public static readonly Color SurfaceAlt =
        Color.FromArgb(247, 249, 252);

    public static readonly Color SurfaceRaised =
        Color.FromArgb(255, 255, 255);

    public static readonly Color Structural =
        Color.FromArgb(25, 38, 64); // #192640

    public static readonly Color StructuralSoft =
        Color.FromArgb(38, 54, 82);

    public static class Spacing
    {
        public const int XSmall = 4;
        public const int Small = 8;
        public const int Medium = 12;
        public const int Large = 18;
        public const int XLarge = 24;
    }

    // Tương thích với code cũ.
    public static readonly Color PrimaryColor = Primary;
    public static readonly Color DarkSidebarColor = PrimaryDark;
    public static readonly Color LightBackgroundColor = Background;

    public static readonly Font BodyFont =
        new("Segoe UI", 9.5f);

    public static readonly Font SmallFont =
        new("Segoe UI", 8.5f);

    public static readonly Font HeadingFont =
        new("Segoe UI Semibold", 17f, FontStyle.Bold);

    public static readonly Font SectionFont =
        new("Segoe UI Semibold", 10f, FontStyle.Bold);

    // =========================
    // APPLY THEME
    // =========================

    public static void Apply(Form form)
    {
        ArgumentNullException.ThrowIfNull(form);

        // Không can thiệp giao diện tối được thiết kế riêng
        // như Movie Discovery hoặc Video Player.
        if (IsDarkColor(form.BackColor))
            return;

        form.BackColor = Background;

        if (form.ForeColor == SystemColors.ControlText)
            form.ForeColor = Text;

        if (form.Font == Control.DefaultFont)
            form.Font = BodyFont;

        if (form.StartPosition ==
            FormStartPosition.WindowsDefaultLocation)
        {
            form.StartPosition = FormStartPosition.CenterParent;
        }

        ApplyControls(form.Controls);
    }

    public static void Apply(Control control)
    {
        ArgumentNullException.ThrowIfNull(control);

        if (control is Form form)
        {
            Apply(form);
            return;
        }

        if (IsDarkColor(control.BackColor))
            return;

        if (control.BackColor == SystemColors.Control)
            control.BackColor = Background;

        if (control.ForeColor == SystemColors.ControlText)
            control.ForeColor = Text;

        if (control.Font == Control.DefaultFont)
            control.Font = BodyFont;

        ApplyControls(control.Controls);
    }

    public static void FitToWorkingArea(
        Form form,
        Size preferredSize,
        Size minimumSize)
    {
        ArgumentNullException.ThrowIfNull(form);
        form.MinimumSize = minimumSize;
        form.Load += (_, _) =>
        {
            if (form.IsDisposed)
                return;

            var workArea = Screen.FromControl(form).WorkingArea;
            var availableWidth = workArea.Width - 24;
            var availableHeight = workArea.Height - 24;
            if (availableWidth < minimumSize.Width ||
                availableHeight < minimumSize.Height)
            {
                return;
            }

            form.Size = new Size(
                Math.Min(preferredSize.Width, availableWidth),
                Math.Min(preferredSize.Height, availableHeight));
        };
    }

    public static void ApplyDefaultGridStyle(DataGridView grid)
    {
        ArgumentNullException.ThrowIfNull(grid);
        StyleGrid(grid);
    }

    public static void StyleActionButton(
        Button button,
        ActionButtonRole role = ActionButtonRole.Secondary)
    {
        ArgumentNullException.ThrowIfNull(button);

        var (background, foreground, hover) = role switch
        {
            ActionButtonRole.Primary =>
                (Accent, Primary, AccentHover),
            ActionButtonRole.Danger =>
                (Danger, Color.White, Color.FromArgb(190, 48, 62)),
            ActionButtonRole.Success =>
                (Success, Primary, Color.FromArgb(20, 128, 58)),
            _ =>
                (SurfaceAlt, Primary, Color.FromArgb(232, 237, 245))
        };

        button.FlatStyle = FlatStyle.Flat;
        button.UseVisualStyleBackColor = false;
        button.BackColor = background;
        button.ForeColor = foreground;
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = hover;
        button.FlatAppearance.MouseDownBackColor = hover;
        button.Cursor = Cursors.Hand;
        button.AccessibleRole = AccessibleRole.PushButton;
        if (button.Font == Control.DefaultFont)
            button.Font = BodyFont;
        button.GotFocus -= ButtonGotFocus;
        button.GotFocus += ButtonGotFocus;
        button.LostFocus -= ButtonLostFocus;
        button.LostFocus += ButtonLostFocus;
    }

    public enum ActionButtonRole
    {
        Primary,
        Secondary,
        Danger,
        Success
    }

    public static void StyleActionButton(Button button, Color intentColor)
    {
        var role = intentColor == Accent
            ? ActionButtonRole.Primary
            : intentColor == Danger || intentColor == Color.FromArgb(185, 65, 65)
                ? ActionButtonRole.Danger
                : intentColor == Success
                    ? ActionButtonRole.Success
                    : ActionButtonRole.Secondary;

        StyleActionButton(button, role);
    }


    public static Panel CreateCard(
        Control content,
        string? title = null)
    {
        ArgumentNullException.ThrowIfNull(content);

        var card = new Panel
        {
            BackColor = Surface,
            Padding = new Padding(16),
            Margin = new Padding(6),
            Dock = DockStyle.Fill
        };

        if (!string.IsNullOrWhiteSpace(title))
        {
            var heading = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 34,
                Font = SectionFont,
                ForeColor = Primary,
                TextAlign = ContentAlignment.MiddleLeft
            };

            card.Controls.Add(heading);
        }

        content.Dock = DockStyle.Fill;
        card.Controls.Add(content);

        card.Paint += (_, e) =>
        {
            using var pen = new Pen(Border);

            e.Graphics.DrawRectangle(
                pen,
                0,
                0,
                Math.Max(0, card.Width - 1),
                Math.Max(0, card.Height - 1));
        };

        return card;
    }

    public static Label CreateStatusBadge(string text, string? tone = null)
    {
        var (foreground, background) = tone?.Trim().ToLowerInvariant() switch
        {
            "success" or "paid" or "active" => (Success, SuccessSoft),
            "warning" or "pending" => (Warning, WarningSoft),
            "danger" or "refunded" or "cancelled" => (Danger, DangerSoft),
            "info" => (Info, InfoSoft),
            _ => (Muted, SurfaceAlt)
        };

        return new Label
        {
            Text = text,
            AutoSize = true,
            BackColor = background,
            ForeColor = foreground,
            Font = SmallFont,
            Padding = new Padding(9, 4, 9, 4),
            Margin = new Padding(2),
            TextAlign = ContentAlignment.MiddleCenter
        };
    }

    private static void ApplyControls(
        Control.ControlCollection controls)
    {
        foreach (Control control in controls)
        {
            switch (control)
            {
                case DataGridView grid:
                    StyleGrid(grid);
                    break;

                case Button button:
                    StyleButton(button);
                    break;

                case TextBox textBox:
                    StyleTextBox(textBox);
                    break;

                case RichTextBox richTextBox:
                    richTextBox.Font = BodyFont;
                    richTextBox.BackColor = Surface;
                    richTextBox.ForeColor = Text;
                    break;

                case ComboBox comboBox:
                    comboBox.Font = BodyFont;
                    comboBox.BackColor = Surface;
                    comboBox.ForeColor = Text;
                    comboBox.FlatStyle = FlatStyle.Flat;
                    comboBox.Enter -= ComboBoxEnter;
                    comboBox.Enter += ComboBoxEnter;
                    comboBox.Leave -= ComboBoxLeave;
                    comboBox.Leave += ComboBoxLeave;
                    break;

                case DateTimePicker picker:
                    picker.Font = BodyFont;
                    picker.CalendarMonthBackground = Surface;
                    picker.CalendarTitleBackColor = Primary;
                    picker.CalendarTitleForeColor = Color.White;
                    break;

                case NumericUpDown numeric:
                    numeric.Font = BodyFont;
                    numeric.BackColor = Surface;
                    numeric.ForeColor = Text;
                    break;

                case CheckBox check:
                    check.Font = BodyFont;
                    check.ForeColor = Text;
                    check.Cursor = Cursors.Hand;
                    break;

                case RadioButton radio:
                    radio.Font = BodyFont;
                    radio.ForeColor = Text;
                    break;

                case TabControl tabs:
                    tabs.Font = BodyFont;
                    tabs.Padding = new Point(14, 6);

                    foreach (TabPage page in tabs.TabPages)
                    {
                        page.BackColor = Surface;
                        page.ForeColor = Text;
                        page.Padding = new Padding(12);
                    }
                    break;

                case MenuStrip menu:
                    menu.BackColor = Surface;
                    menu.ForeColor = Text;
                    menu.Font = BodyFont;
                    menu.GripStyle = ToolStripGripStyle.Hidden;
                    menu.Padding = new Padding(8, 5, 8, 5);
                    break;

                case StatusStrip status:
                    status.BackColor = Surface;
                    status.ForeColor = Muted;
                    status.Font = SmallFont;
                    break;

                case ToolStrip toolStrip:
                    toolStrip.BackColor = Surface;
                    toolStrip.ForeColor = Text;
                    toolStrip.Font = BodyFont;
                    toolStrip.GripStyle = ToolStripGripStyle.Hidden;
                    break;

                case GroupBox group:
                    group.ForeColor = Primary;

                    if (group.Font == Control.DefaultFont)
                        group.Font = SectionFont;
                    break;

                case FlowLayoutPanel flow:
                    if (flow.BackColor == SystemColors.Control)
                        flow.BackColor = Background;
                    break;

                case TableLayoutPanel table:
                    if (table.BackColor == SystemColors.Control)
                        table.BackColor = Background;
                    break;

                case Panel panel:
                    if (panel.BackColor == SystemColors.Control)
                        panel.BackColor = Background;
                    break;

                case CheckedListBox checkedList:
                    checkedList.BackColor = Surface;
                    checkedList.ForeColor = Text;
                    checkedList.Font = BodyFont;
                    break;

                case ListBox listBox:
                    listBox.BackColor = Surface;
                    listBox.ForeColor = Text;
                    listBox.Font = BodyFont;
                    break;

                case ListView listView:
                    listView.BackColor = Surface;
                    listView.ForeColor = Text;
                    listView.Font = BodyFont;
                    listView.BorderStyle = BorderStyle.FixedSingle;
                    break;

                case TreeView tree:
                    tree.BackColor = Surface;
                    tree.ForeColor = Text;
                    tree.Font = BodyFont;
                    break;

                case LinkLabel link:
                    link.LinkColor = Accent;
                    link.ActiveLinkColor = AccentHover;
                    link.VisitedLinkColor = Accent;
                    break;
            }

            if (control is Label label)
            {
                if (label.ForeColor == SystemColors.ControlText)
                    label.ForeColor = Text;

                if (label.Font == Control.DefaultFont)
                    label.Font = BodyFont;
            }

            // Không áp dụng đệ quy vào form con có giao diện tối riêng.
            if (control is Form childForm &&
                IsDarkColor(childForm.BackColor))
            {
                continue;
            }

            if (control.HasChildren)
                ApplyControls(control.Controls);
        }
    }

    // =========================
    // TEXTBOX
    // =========================

    private static void StyleTextBox(TextBox textBox)
    {
        textBox.Font = BodyFont;
        textBox.BackColor = Surface;
        textBox.ForeColor = Text;
        textBox.BorderStyle = BorderStyle.FixedSingle;
        textBox.Enter -= TextBoxEnter;
        textBox.Enter += TextBoxEnter;
        textBox.Leave -= TextBoxLeave;
        textBox.Leave += TextBoxLeave;
    }

    private static void TextBoxEnter(object? sender, EventArgs e)
    {
        if (sender is TextBox textBox && textBox.Enabled && !textBox.ReadOnly)
            textBox.BackColor = SelectedSurface;
    }

    private static void TextBoxLeave(object? sender, EventArgs e)
    {
        if (sender is TextBox textBox && textBox.Enabled)
            textBox.BackColor = Surface;
    }

    private static void ComboBoxEnter(object? sender, EventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.Enabled)
            comboBox.BackColor = SelectedSurface;
    }

    private static void ComboBoxLeave(object? sender, EventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.Enabled)
            comboBox.BackColor = Surface;
    }

    // =========================
    // BUTTON
    // =========================

    private static void StyleButton(Button button)
    {
        // Không ghi đè nút điều hướng hoặc nút có giao diện riêng.
        if (button.Tag as string == "sidebar")
            return;

        // Chỉ tạo kiểu cho nút mặc định.
        // Nút ghế, nút trạng thái và nút custom có màu riêng
        // sẽ được giữ nguyên.
        if (button.BackColor != SystemColors.Control &&
            button.BackColor != Color.Empty)
        {
            return;
        }

        if (!button.UseVisualStyleBackColor)
            return;

        var caption = button.Text.Trim();

        Color background;
        Color foreground = Primary;
        Color hover;

        if (IsDangerAction(caption))
        {
            background = Danger;
            foreground = Color.White;
            hover = Color.FromArgb(190, 48, 62);
        }
        else if (IsPrimaryAction(caption))
        {
            background = Accent;
            foreground = Primary;
            hover = AccentHover;
        }
        else
        {
            background = SurfaceAlt;
            foreground = Primary;
            hover = Color.FromArgb(232, 237, 245);
        }

        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = hover;
        button.FlatAppearance.MouseDownBackColor = hover;

        button.UseVisualStyleBackColor = false;
        button.BackColor = background;
        button.ForeColor = foreground;

        button.Cursor = Cursors.Hand;
        button.Font = new Font(
            "Segoe UI Semibold",
            9f,
            FontStyle.Bold);

        button.Padding = new Padding(12, 5, 12, 5);
        button.GotFocus -= ButtonGotFocus;
        button.GotFocus += ButtonGotFocus;
        button.LostFocus -= ButtonLostFocus;
        button.LostFocus += ButtonLostFocus;
    }

    private static void ButtonGotFocus(object? sender, EventArgs e)
    {
        if (sender is Button button)
        {
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = Focus;
        }
    }

    private static void ButtonLostFocus(object? sender, EventArgs e)
    {
        if (sender is Button button)
            button.FlatAppearance.BorderSize = 0;
    }

    private static bool IsDangerAction(string caption)
    {
        string[] terms =
        [
            "Hủy vé",
            "Hủy đơn",
            "Hoàn tiền",
            "Xóa",
            "Xoá",
            "Khóa",
            "Ngừng",
            "Khôi phục",
            "Delete",
            "Remove",
            "Refund",
            "Restore",
            "Disable",
            "Lock"
        ];

        return ContainsAny(caption, terms);
    }

    private static bool IsPrimaryAction(string caption)
    {
        string[] terms =
        [
            "Thêm",
            "Tạo",
            "Lưu",
            "Xác nhận",
            "Thanh toán",
            "Đăng nhập",
            "Tiếp tục",
            "Áp dụng",
            "Đồng ý",
            "Hoàn tất",
            "Sao lưu",
            "Xem báo cáo",
            "Add",
            "Create",
            "Save",
            "Backup",
            "Confirm",
            "Pay",
            "Continue",
            "Apply",
            "Finish"
        ];

        return ContainsAny(caption, terms);
    }

    private static bool ContainsAny(
        string value,
        IEnumerable<string> terms)
    {
        return terms.Any(term =>
            value.Contains(
                term,
                StringComparison.OrdinalIgnoreCase));
    }

    // =========================
    // DATAGRIDVIEW
    // =========================

    private static void StyleGrid(DataGridView grid)
    {
        grid.SuspendLayout();
        grid.AccessibleRole = AccessibleRole.Table;
        grid.TabStop = true;
        if (string.IsNullOrWhiteSpace(grid.AccessibleName))
        {
            var formName = grid.FindForm()?.Text;
            grid.AccessibleName = string.IsNullOrWhiteSpace(formName)
                ? "Bảng dữ liệu"
                : $"Bảng dữ liệu - {formName}";
        }

        if (string.IsNullOrWhiteSpace(grid.AccessibleDescription))
        {
            grid.AccessibleDescription =
                "Dùng phím Tab để vào bảng và các phím mũi tên để duyệt hàng, cột.";
        }

        grid.ColumnAdded -= ApplyGridColumnMinimumWidth;
        grid.ColumnAdded += ApplyGridColumnMinimumWidth;
        foreach (DataGridViewColumn column in grid.Columns)
            ApplyGridColumnMinimumWidth(grid, new DataGridViewColumnEventArgs(column));

        grid.BackgroundColor = Surface;
        grid.BorderStyle = BorderStyle.None;
        grid.GridColor = Border;

        grid.CellBorderStyle =
            DataGridViewCellBorderStyle.SingleHorizontal;

        grid.ColumnHeadersBorderStyle =
            DataGridViewHeaderBorderStyle.None;

        grid.EnableHeadersVisualStyles = false;

        grid.ColumnHeadersDefaultCellStyle.BackColor = Structural;

        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Structural;
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
        grid.ColumnHeadersDefaultCellStyle.Font = SectionFont;

        grid.ColumnHeadersDefaultCellStyle.Padding =
            new Padding(10, 0, 10, 0);

        grid.ColumnHeadersDefaultCellStyle.WrapMode =
            DataGridViewTriState.True;

        if (grid.ColumnHeadersHeight < 44)
            grid.ColumnHeadersHeight = 44;

        grid.DefaultCellStyle.BackColor = Surface;
        grid.DefaultCellStyle.ForeColor = Text;
        grid.DefaultCellStyle.Font = BodyFont;

        grid.DefaultCellStyle.SelectionBackColor = SelectedSurface;
        grid.DefaultCellStyle.SelectionForeColor = PrimaryDark;

        grid.DefaultCellStyle.Padding =
            new Padding(8, 5, 8, 5);

        grid.DefaultCellStyle.WrapMode = DataGridViewTriState.False;

        grid.AlternatingRowsDefaultCellStyle.BackColor = SurfaceAlt;

        if (grid.RowTemplate.Height < 34)
            grid.RowTemplate.Height = 36;

        grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
        grid.ColumnHeadersHeightSizeMode =
            DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.ShowCellToolTips = true;
        grid.RowHeadersVisible = false;
        grid.AllowUserToResizeRows = false;
        if (grid.AutoSizeColumnsMode == DataGridViewAutoSizeColumnsMode.None)
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        grid.ResumeLayout();

        // Cố ý không thay đổi:
        // ReadOnly, MultiSelect, SelectionMode,
        // Explicit AutoSizeColumnsMode, AllowUserToAddRows,
        // AllowUserToResizeRows, EditMode.
        // Những thuộc tính này thuộc hành vi nghiệp vụ của từng màn hình.
    }

    private static void ApplyGridColumnMinimumWidth(
        object? sender,
        DataGridViewColumnEventArgs e)
    {
        const int minimumContentWidth = 80;
        if (e.Column.MinimumWidth < minimumContentWidth)
            e.Column.MinimumWidth = minimumContentWidth;
    }


    private static bool IsDarkColor(Color color)
    {
        var brightness =
            (color.R * 299 +
             color.G * 587 +
             color.B * 114) / 1000;

        return brightness < 75;
    }
}
