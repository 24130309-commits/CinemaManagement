
﻿namespace CinemaManagement.UI.Helpers;

/// <summary>
/// Bộ giao diện Light dùng chung cho các màn hình.
/// Chỉ thay đổi thuộc tính trình bày, không can thiệp dữ liệu hay nghiệp vụ.
/// </summary>
public static class ThemeHelper
{
    public static readonly Color Primary = Color.FromArgb(27, 54, 93);
    public static readonly Color PrimaryDark = Color.FromArgb(20, 39, 68);
    public static readonly Color Accent = Color.FromArgb(37, 99, 235);
    public static readonly Color AccentSoft = Color.FromArgb(239, 246, 255);
    public static readonly Color Surface = Color.White;
    public static readonly Color Background = Color.FromArgb(245, 247, 251);
    public static readonly Color Text = Color.FromArgb(31, 41, 55);
    public static readonly Color Muted = Color.FromArgb(100, 116, 139);
    public static readonly Color Border = Color.FromArgb(226, 232, 240);
    public static readonly Color Success = Color.FromArgb(22, 163, 74);
    public static readonly Color Danger = Color.FromArgb(220, 38, 38);

    public static readonly Font BodyFont = new("Segoe UI", 9.5f);
    public static readonly Font SmallFont = new("Segoe UI", 8.5f);
    public static readonly Font HeadingFont = new("Segoe UI", 17f, FontStyle.Bold);
    public static readonly Font SectionFont = new("Segoe UI", 10.5f, FontStyle.Bold);

    public static void Apply(Form form)
    {
        form.BackColor = Background;
        form.ForeColor = Text;
        form.Font = BodyFont;

        form.StartPosition =
            form.StartPosition == FormStartPosition.WindowsDefaultLocation
                ? FormStartPosition.CenterParent
                : form.StartPosition;

        ApplyControls(form.Controls);
    }

    private static void ApplyControls(Control.ControlCollection controls)
    {
        foreach (Control control in controls)
        {
            switch (control)
            {
                case Button button:
                    StyleButton(button);
                    break;

                case DataGridView grid:
                    StyleGrid(grid);
                    break;

                case TextBox textBox:
                    textBox.Font = BodyFont;
                    textBox.BackColor = Surface;
                    textBox.ForeColor = Text;
                    textBox.BorderStyle = BorderStyle.FixedSingle;
                    break;

                case ComboBox comboBox:
                    comboBox.Font = BodyFont;
                    comboBox.BackColor = Surface;
                    comboBox.ForeColor = Text;
                    comboBox.FlatStyle = FlatStyle.Flat;
                    break;

                case DateTimePicker datePicker:
                    datePicker.Font = BodyFont;
                    datePicker.CalendarMonthBackground = Surface;
                    datePicker.CalendarTitleBackColor = Primary;
                    datePicker.CalendarTitleForeColor = Color.White;
                    break;

                case NumericUpDown numeric:
                    numeric.Font = BodyFont;
                    numeric.BackColor = Surface;
                    numeric.ForeColor = Text;
                    numeric.BorderStyle = BorderStyle.FixedSingle;
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
                    menu.Padding = new Padding(8, 4, 8, 4);
                    break;

                case FlowLayoutPanel flow:
                    if (flow.BackColor == SystemColors.Control)
                        flow.BackColor = Surface;
                    break;

                case Panel panel:
                    if (panel.BackColor == SystemColors.Control)
                        panel.BackColor = Surface;
                    break;

                case GroupBox group:
                    group.ForeColor = Primary;
                    group.Font = SectionFont;
                    break;
            }

            if (control is Label label &&
                label.ForeColor == SystemColors.ControlText)
            {
                label.ForeColor = Text;
            }

            if (control.HasChildren)
                ApplyControls(control.Controls);
        }
    }

    private static void StyleButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.BorderColor = Border;
        button.Cursor = Cursors.Hand;
        button.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        button.Padding = new Padding(12, 5, 12, 5);
        button.UseVisualStyleBackColor = false;

        if (string.Equals(
                button.Tag as string,
                "sidebar",
                StringComparison.Ordinal))
        {
            button.BackColor = PrimaryDark;
            button.ForeColor = Color.FromArgb(226, 232, 240);
            button.TextAlign = ContentAlignment.MiddleLeft;
            button.FlatAppearance.MouseOverBackColor =
                Color.FromArgb(39, 64, 99);
            button.FlatAppearance.MouseDownBackColor = Primary;
            return;
        }

        var caption = button.Text.Trim();

        var isNeutral =
            caption.Contains("Hủy", StringComparison.OrdinalIgnoreCase)
            || caption.Contains("Đóng", StringComparison.OrdinalIgnoreCase)
            || caption.Contains("Quay lại", StringComparison.OrdinalIgnoreCase);

        var isDanger =
            caption.Contains("Xóa", StringComparison.OrdinalIgnoreCase)
            || caption.Contains("Khóa", StringComparison.OrdinalIgnoreCase);

        button.BackColor = isNeutral
            ? Color.FromArgb(241, 245, 249)
            : isDanger ? Danger : Accent;

        button.ForeColor = isNeutral ? Text : Color.White;

        button.FlatAppearance.MouseOverBackColor = isNeutral
            ? Color.FromArgb(226, 232, 240)
            : isDanger
                ? Color.FromArgb(185, 28, 28)
                : Color.FromArgb(29, 78, 216);

        button.FlatAppearance.MouseDownBackColor = isNeutral
            ? Border
            : isDanger
                ? Color.FromArgb(153, 27, 27)
                : Primary;
    }

    private static void StyleGrid(DataGridView grid)
    {
        grid.BackgroundColor = Surface;
        grid.BorderStyle = BorderStyle.None;
        grid.GridColor = Border;

        grid.CellBorderStyle =
            DataGridViewCellBorderStyle.SingleHorizontal;

        grid.ColumnHeadersBorderStyle =
            DataGridViewHeaderBorderStyle.None;

        grid.EnableHeadersVisualStyles = false;

        grid.ColumnHeadersDefaultCellStyle.BackColor = Primary;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        grid.ColumnHeadersDefaultCellStyle.Font =
            new Font("Segoe UI", 9f, FontStyle.Bold);

        grid.ColumnHeadersDefaultCellStyle.Padding =
            new Padding(8, 0, 8, 0);

        grid.ColumnHeadersHeight = 42;

        grid.ColumnHeadersHeightSizeMode =
            DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

        grid.DefaultCellStyle.BackColor = Surface;
        grid.DefaultCellStyle.ForeColor = Text;
        grid.DefaultCellStyle.Font = BodyFont;
        grid.DefaultCellStyle.SelectionBackColor = AccentSoft;
        grid.DefaultCellStyle.SelectionForeColor = Text;
        grid.DefaultCellStyle.Padding = new Padding(7, 3, 7, 3);

        grid.AlternatingRowsDefaultCellStyle.BackColor =
            Color.FromArgb(248, 250, 252);

        grid.RowTemplate.Height = 38;
        grid.RowHeadersVisible = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToResizeRows = false;
        grid.ReadOnly = true;
    }
}
