using System.Drawing;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Cores.Nintendo.Gameboy;

namespace BizHawk.Client.EmuHawk
{
	public partial class CGBColorChooserForm : Form
	{
		private CGBColorChooserForm()
		{
			InitializeComponent();
			Icon = Properties.Resources.GambatteIcon;
			bmpView1.ChangeBitmapSize(new Size(256, 128));
		}

		private GBColors.ColorType _type;

		private void LoadType(Gameboy.GambatteSettings s)
		{
			_type = s.CGBColors;
			switch (_type)
			{
				case GBColors.ColorType.sameboy:
					radioButton0.Checked = true;
					break;
				case GBColors.ColorType.gambatte:
					radioButton1.Checked = true;
					break;
				case GBColors.ColorType.vivid:
					radioButton2.Checked = true;
					break;
				case GBColors.ColorType.vbavivid:
					radioButton3.Checked = true;
					break;
				case GBColors.ColorType.vbagbnew:
					radioButton4.Checked = true;
					break;
				case GBColors.ColorType.vbabgbold:
					radioButton5.Checked = true;
					break;
				case GBColors.ColorType.gba:
					radioButton6.Checked = true;
					break;
				case GBColors.ColorType.libretrogbc:
					radioButton7.Checked = true;
					break;
			}
		}

		private unsafe void RefreshType()
		{
			var lockBits = bmpView1.Bmp.LockBits(new Rectangle(0, 0, 256, 128), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

			int[] lut = GBColors.GetLut(_type);

			int* dest = (int*)lockBits.Scan0;

			for (int j = 0; j < 128; j++)
			{
				for (int i = 0; i < 256; i++)
				{
					int r = i % 32;
					int g = j % 32;
					int b = (i / 32 * 4) + (j / 32);
					int color = lut[r | g << 5 | b << 10];
					*dest++ = color;
				}

				dest -= 256;
				dest += lockBits.Stride / sizeof(int);
			}

			bmpView1.Bmp.UnlockBits(lockBits);
			bmpView1.Refresh();
		}

		private void RadioButton1_CheckedChanged(object sender, EventArgs e)
		{
			if (sender == radioButton0)
			{
				_type = GBColors.ColorType.sameboy;
			}

			if (sender == radioButton1)
			{
				_type = GBColors.ColorType.gambatte;
			}

			if (sender == radioButton2)
			{
				_type = GBColors.ColorType.vivid;
			}

			if (sender == radioButton3)
			{
				_type = GBColors.ColorType.vbavivid;
			}

			if (sender == radioButton4)
			{
				_type = GBColors.ColorType.vbagbnew;
			}

			if (sender == radioButton5)
			{
				_type = GBColors.ColorType.vbabgbold;
			}

			if (sender == radioButton6)
			{
				_type = GBColors.ColorType.gba;
			}

			if (sender == radioButton7)
			{
				_type = GBColors.ColorType.libretrogbc;
			}

			if (sender is RadioButton radioButton && radioButton.Checked)
			{
				RefreshType();
			}
		}

		public static void DoCGBColorChooserFormDialog(IDialogParent parent, Gameboy.GambatteSettings s)
		{
			using var dlg = new CGBColorChooserForm();
			dlg.LoadType(s);
			if (!parent.ShowDialogAsChild(dlg).IsOk()) return;
			s.CGBColors = dlg._type;
		}
	}
}
