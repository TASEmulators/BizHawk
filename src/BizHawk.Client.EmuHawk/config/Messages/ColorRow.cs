using System.Drawing;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	public partial class ColorRow : UserControl
	{
		private int _selectedColor;
		public int SelectedColor
		{
			get => _selectedColor;
			set
			{
				_selectedColor = value;
				SetColor();
			}
		}

		public string DisplayName
		{
			get => DisplayNameLabel.Text;
			set => DisplayNameLabel.Text = value;
		}

		public ColorRow()
		{
			InitializeComponent();
		}

		private void SetColor()
		{
			ColorPanel.BackColor = Color.FromArgb(_selectedColor);
			ColorText.Text = $"{SelectedColor:X8}";
		}

		private void ColorPanel_Click(object sender, EventArgs e)
		{
			using var colorPicker = new ColorDialog
			{
				FullOpen = true, Color = Color.FromArgb(_selectedColor)
			};

			if (colorPicker.ShowDialog().IsOk())
			{
				_selectedColor = colorPicker.Color.ToArgb();
				SetColor();
			}
		}
	}
}
