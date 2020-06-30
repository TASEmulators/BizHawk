using System;
using System.Drawing;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	// this is a little messy right now because of remnants of the old config system
	public partial class ColorRow : UserControl
	{
		public int SelectedColor { get; private set; }

		public ColorRow()
		{
			InitializeComponent();
		}

		public void Bind(string displayName, int color)
		{
			DisplayNameLabel.Text = displayName;
			SelectedColor = color;
			SetColor();
		}

		private void SetColor()
		{
			ColorPanel.BackColor = Color.FromArgb(SelectedColor);
			ColorText.Text = $"{SelectedColor:X8}";
		}

		private void ColorPanel_Click(object sender, EventArgs e)
		{
			using var colorPicker = new ColorDialog 
			{ 
				FullOpen = true, Color = Color.FromArgb(SelectedColor)
			};

			if (colorPicker.ShowDialog().IsOk())
			{
				SelectedColor = colorPicker.Color.ToArgb();
				SetColor();
			}
		}
	}
}
