using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.Emulation.Consoles.Nintendo.Gameboy
{
	public partial class ColorChooserForm : Form
	{
		public ColorChooserForm()
		{
			InitializeComponent();
		}

		Color[] colors = new Color[12];
		int selectedcolor = -1;

		Panel currentpanel = null;

		bool refreshingcolors = false;

		private void RefreshColors(bool changenumeric)
		{
			refreshingcolors = true;
			if (selectedcolor == -1)
			{
				panel13.BackColor = DefaultBackColor;
			}
			else
			{
				panel13.BackColor = colors[selectedcolor];
				if (changenumeric)
				{
					numericUpDown1.Value = colors[selectedcolor].R;
					numericUpDown2.Value = colors[selectedcolor].G;
					numericUpDown3.Value = colors[selectedcolor].B;
				}
				if (currentpanel != null)
					currentpanel.BackColor = colors[selectedcolor];
			}
			refreshingcolors = false;
		}

		private void panel12_Click(object _sender, EventArgs e)
		{
			Panel sender = (Panel)_sender;

			int i;
			if (sender == panel1)
				i = 0;
			else if (sender == panel2)
				i = 1;
			else if (sender == panel3)
				i = 2;
			else if (sender == panel4)
				i = 3;
			else if (sender == panel5)
				i = 4;
			else if (sender == panel6)
				i = 5;
			else if (sender == panel7)
				i = 6;
			else if (sender == panel8)
				i = 7;
			else if (sender == panel9)
				i = 8;
			else if (sender == panel10)
				i = 9;
			else if (sender == panel11)
				i = 10;
			else if (sender == panel12)
				i = 11;
			else
				i = -1;

			selectedcolor = i;
			currentpanel = sender;

			RefreshColors(true);
		}

		private void numericUpDown1_ValueChanged(object sender, EventArgs e)
		{
			if (refreshingcolors)
				return;
			if (selectedcolor != -1)
			{
				colors[selectedcolor] = Color.FromArgb(
					(int)numericUpDown1.Value,
					(int)numericUpDown2.Value,
					(int)numericUpDown3.Value
				);

				RefreshColors(false);
			}

		}

		private void SetColorsOnce()
		{
			panel1.BackColor = colors[0];
			panel2.BackColor = colors[1];
			panel3.BackColor = colors[2];
			panel4.BackColor = colors[3];
			panel5.BackColor = colors[4];
			panel6.BackColor = colors[5];
			panel7.BackColor = colors[6];
			panel8.BackColor = colors[7];
			panel9.BackColor = colors[8];
			panel10.BackColor = colors[9];
			panel11.BackColor = colors[10];
			panel12.BackColor = colors[11];
		}

		public static bool DoColorChooserFormDialog(int[] colors)
		{
			using (var dlg = new ColorChooserForm())
			{
				for (int i = 0; i < dlg.colors.Length; i++)
					dlg.colors[i] = Color.FromArgb(255, Color.FromArgb(colors[i]));
				dlg.SetColorsOnce();

				var result = dlg.ShowDialog();
				if (result != DialogResult.OK)
				{
					return false;
				}
				else
				{
					for (int i = 0; i < dlg.colors.Length; i++)
						colors[i] = dlg.colors[i].ToArgb();
					return true;
				}
			}
		}
	}
}
