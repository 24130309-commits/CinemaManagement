using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;

namespace CinemaManagement.UI.Controls
{
    public class StatCard : Panel
    {
        private readonly Label _lblTitle;
        private readonly Label _lblValue;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Title { get => _lblTitle.Text; set => _lblTitle.Text = value; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Value { get => _lblValue.Text; set => _lblValue.Text = value; }

        public StatCard()
        {
            Size = new Size(220, 100);
            BackColor = Color.FromArgb(41, 128, 185);
            Padding = new Padding(12);

            _lblTitle = new Label
            {
                Text = "TIÊU ĐỀ",
                ForeColor = Color.WhiteSmoke,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 25
            };

            _lblValue = new Label
            {
                Text = "0",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            Controls.Add(_lblValue);
            Controls.Add(_lblTitle);
        }
    }
}