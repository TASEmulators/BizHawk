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

		private void RefreshAllBackdrops()
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

		Color betweencolor(Color left, Color right, double pos)
		{
			int R = (int)(right.R * pos + left.R * (1.0 - pos) + 0.5);
			int G = (int)(right.G * pos + left.G * (1.0 - pos) + 0.5);
			int B = (int)(right.B * pos + left.B * (1.0 - pos) + 0.5);
			int A = (int)(right.A * pos + left.A * (1.0 - pos) + 0.5);

			return Color.FromArgb(A, R, G, B);
		}

		void interpolate_colors(int firstindex, int lastindex)
		{
			for (int i = firstindex + 1; i < lastindex; i++)
			{
				double pos = (double)(i - firstindex) / (double)(lastindex - firstindex);
				colors[i] = betweencolor(colors[firstindex], colors[lastindex], pos);
			}
			RefreshAllBackdrops();
		}

		private void button3_Click(object sender, EventArgs e)
		{
			interpolate_colors(0, 3);
		}

		private void button4_Click(object sender, EventArgs e)
		{
			interpolate_colors(4, 7);
		}

		private void button5_Click(object sender, EventArgs e)
		{
			interpolate_colors(8, 11);
		}

		private void panel12_DoubleClick(object _sender, EventArgs e)
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
				return; // i = -1;

			using (var dlg = new ColorDialog())
			{
				dlg.AllowFullOpen = true;
				dlg.AnyColor = true;
				dlg.Color = colors[i];

				// custom colors are ints, not Color structs?
				// and they don't work right unless the alpha bits are set to 0
				int[] customs = new int[12];
				for (int j = 0; j < customs.Length; j++)
					customs[j] = colors[j].ToArgb() & 0xffffff;

				dlg.CustomColors = customs;
				dlg.FullOpen = true;

				var result = dlg.ShowDialog(this);

				if (result == System.Windows.Forms.DialogResult.OK)
				{
					colors[i] = dlg.Color;
					sender.BackColor = colors[i];
				}
			}
		}

		public static bool DoColorChooserFormDialog(int[] colors)
		{
			using (var dlg = new ColorChooserForm())
			{
				for (int i = 0; i < dlg.colors.Length; i++)
					dlg.colors[i] = Color.FromArgb(255, Color.FromArgb(colors[i]));
				dlg.RefreshAllBackdrops();

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
