using BizHawk.Client.Common;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	// this is a little messy right now because of remnants of the old config system
	public partial class ColorRow : UserControl
	{
		private int _color;

		public ColorRow()
		{
			InitializeComponent();
		}

		public void Bind(string displayName, ref int color)
		{
			DisplayNameLabel.Text = displayName;
			_color = color;
			SetColor();
		}

		private void SetColor()
		{
			ColorPanel.BackColor = Color.FromArgb(_color);
			ColorText.Text = $"{_color:X8}";
		}

		private void ColorPanel_Click(object sender, EventArgs e)
		{
			using var colorPicker = new ColorDialog { FullOpen = true };
			if (colorPicker.ShowDialog().IsOk())
			{
				_color = colorPicker.Color.ToArgb();
				SetColor();
			}
		}
	}
}
