using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES;
using BizHawk.Client.Common;
using BizHawk.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class QuickNesConfig : Form
	{
		private QuickNES.QuickNESSettings Settings;

		public QuickNesConfig()
		{
			InitializeComponent();
		}

		private void QuickNesConfig_Load(object sender, EventArgs e)
		{
			Settings = ((QuickNES)Global.Emulator).GetSettings();
			propertyGrid1.SelectedObject = Settings;
			SetPaletteImage();
		}

		void SetPaletteImage()
		{
			int w = pictureBox1.Size.Width;
			int h = pictureBox1.Size.Height;
			var bmp = new Bitmap(w, h);
			var pal = Settings.Palette;

			for (int j = 0; j < h; j++)
			{
				int iy = j * 12 / h;
				int cy = iy / 3;
				for (int i = 0; i < w; i++)
				{
					int ix = i * 112 / w;
					int cx = ix / 7;
					if (iy % 3 == 2)
					{
						cx += 64 * (ix % 7 + 1);
					}
					int cindex = cy * 16 + cx;
					Color col = Color.FromArgb(0xff, pal[cindex * 3], pal[cindex * 3 + 1], pal[cindex * 3 + 2]);
					bmp.SetPixel(i, j, col);
				}
			}

			if (pictureBox1.Image != null) pictureBox1.Image.Dispose();
			pictureBox1.Image = bmp;
		}

		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);
			
			if (pictureBox1.Image != null)
			{
				pictureBox1.Image.Dispose();
				pictureBox1.Image = null;
			}

		}

		private void buttonPal_Click(object sender, EventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog
			{
				InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.PathEntries["NES", "Palettes"].Path, "NES"),
				Filter = "Palette Files (.pal)|*.PAL|All Files (*.*)|*.*",
				RestoreDirectory = true
			};

			var result = ofd.ShowDialog();
			if (result != DialogResult.OK)
			{
				return;
			}
			HawkFile palette = new HawkFile(ofd.FileName);

			if (palette != null && palette.Exists)
			{
				var data = Emulation.Cores.Nintendo.NES.Palettes.Load_FCEUX_Palette(HawkFile.ReadAllBytes(palette.Name));
				Settings.SetNesHawkPalette(data);
				SetPaletteImage();
			}
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			GlobalWin.MainForm.PutCoreSettings(Settings);
			DialogResult = DialogResult.OK;
			Close();
		}

		private void buttonPalReset_Click(object sender, EventArgs e)
		{
			Settings.SetDefaultColors();
			SetPaletteImage();
		}
	}
}
