using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using BizHawk.Emulation.Consoles.GB;

namespace BizHawk.MultiClient.GBtools
{
	public partial class CGBColorChooserForm : Form
	{
		CGBColorChooserForm()
		{
			InitializeComponent();
			bmpView1.ChangeBitmapSize(bmpView1.Size);
			type = Global.Config.CGBColors;
			switch (type)
			{
				case GBColors.ColorType.gambatte: radioButton1.Checked = true; break;
				case GBColors.ColorType.vivid: radioButton2.Checked = true; break;
				case GBColors.ColorType.vbavivid: radioButton3.Checked = true; break;
				case GBColors.ColorType.vbagbnew: radioButton4.Checked = true; break;
				case GBColors.ColorType.vbabgbold: radioButton5.Checked = true; break;
				case GBColors.ColorType.gba: radioButton6.Checked = true; break;
			}
		}
		GBColors.ColorType type;

		unsafe void RefreshType()
		{
			var lockdata = bmpView1.bmp.LockBits(new Rectangle(0, 0, 256, 128), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

			int[] lut = GBColors.GetLut(type);

			int* dest = (int*)lockdata.Scan0;

			for (int j = 0; j < 128; j++)
			{
				for (int i = 0; i < 256; i++)
				{
					int r = i % 32;
					int g = j % 32;
					int b = i / 32 * 4 + j / 32;
					int color = lut[r | g << 5 | b << 10];
					*dest++ = color;
				}
				dest -= 256;
				dest += lockdata.Stride / sizeof(int);
			}

			bmpView1.bmp.UnlockBits(lockdata);
			bmpView1.Refresh();
		}

		private void radioButton1_CheckedChanged(object sender, EventArgs e)
		{
			if (sender == radioButton1)
				type = GBColors.ColorType.gambatte;
			if (sender == radioButton2)
				type = GBColors.ColorType.vivid;
			if (sender == radioButton3)
				type = GBColors.ColorType.vbavivid;
			if (sender == radioButton4)
				type = GBColors.ColorType.vbagbnew;
			if (sender == radioButton5)
				type = GBColors.ColorType.vbabgbold;
			if (sender == radioButton6)
				type = GBColors.ColorType.gba;
			if ((sender as RadioButton).Checked)
				RefreshType();
		}

		public static bool DoCGBColorChooserFormDialog(IWin32Window parent)
		{
			using (var dlg = new CGBColorChooserForm())
			{
				var result = dlg.ShowDialog(parent);
				if (result == DialogResult.OK)
				{
					Global.Config.CGBColors = dlg.type;
					return true;
				}
				else
					return false;
			}
		}

	}
}
