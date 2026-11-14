
namespace CinemaManagement.UI.Helpers;

public static class ThemeHelper
{
    public static readonly Color Primary = Color.FromArgb(25, 43, 75);
    public static readonly Color Accent = Color.FromArgb(230, 75, 65);
    public static readonly Color Surface = Color.White;
    public static readonly Color Background = Color.FromArgb(244, 246, 250);
    public static readonly Color Text = Color.FromArgb(40, 48, 62);
    public static readonly Font BodyFont = new("Segoe UI", 9.5f);
    public static readonly Font HeadingFont = new("Segoe UI", 16f, FontStyle.Bold);

    public static void Apply(Form form)
    {
        form.BackColor = Background;
        form.Font = BodyFont;
        form.ForeColor = Text;
        ApplyControls(form.Controls);
    }

    private static void ApplyControls(Control.ControlCollection controls)
    {
        foreach (Control control in controls)
        {
            if (control is Button button)
            {
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderSize = 0;
                button.BackColor = Primary;
                button.ForeColor = Color.White;
                button.Cursor = Cursors.Hand;
                button.Padding = new Padding(8, 4, 8, 4);
            }
            else if (control is DataGridView grid)
            {
                grid.BackgroundColor = Surface;
                grid.BorderStyle = BorderStyle.None;
                grid.EnableHeadersVisualStyles = false;
                grid.ColumnHeadersDefaultCellStyle.BackColor = Primary;
                grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
                grid.RowHeadersVisible = false;
                grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                grid.MultiSelect = false;
                grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                grid.AllowUserToAddRows = false;
            }
            else if (control is TextBox or ComboBox or DateTimePicker)
            {
                control.BackColor = Surface;
            }

            if (control.HasChildren)
                ApplyControls(control.Controls);
        }
    }
}
