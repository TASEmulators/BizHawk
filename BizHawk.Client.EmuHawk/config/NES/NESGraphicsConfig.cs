using System;
using System.Drawing;
using System.Windows.Forms;

using BizHawk.Common;
using BizHawk.Client.Common;
using BizHawk.Emulation.Cores.Nintendo.NES;

namespace BizHawk.Client.EmuHawk
{
	public partial class NESGraphicsConfig : Form
	{
		// TODO:
		// Allow selection of palette file from archive
		// Hotkeys for BG & Sprite display toggle
		// NTSC filter settings? Hue, Tint (This should probably be a client thing, not a nes specific thing?)

		private NES nes;
		private NES.NESSettings settings;
		Bitmap bmp;

		public NESGraphicsConfig()
		{
			InitializeComponent();
		}

		private void NESGraphicsConfig_Load(object sender, EventArgs e)
		{
			nes = Global.Emulator as NES;
			settings = (NES.NESSettings)nes.GetSettings();
			LoadStuff();
		}

		private void LoadStuff()
		{
			NTSC_FirstLineNumeric.Value = settings.NTSC_TopLine;
			NTSC_LastLineNumeric.Value = settings.NTSC_BottomLine;
			PAL_FirstLineNumeric.Value = settings.PAL_TopLine;
			PAL_LastLineNumeric.Value = settings.PAL_BottomLine;
			AllowMoreSprites.Checked = settings.AllowMoreThanEightSprites;
			ClipLeftAndRightCheckBox.Checked = settings.ClipLeftAndRight;
			DispSprites.Checked = settings.DispSprites;
			DispBackground.Checked = settings.DispBackground;
			BGColorDialog.Color = Color.FromArgb(unchecked(settings.BackgroundColor | (int)0xFF000000));
			checkUseBackdropColor.Checked = (settings.BackgroundColor & 0xFF000000) != 0;
			SetColorBox();
			SetPaletteImage();
		}

		private void BrowsePalette_Click(object sender, EventArgs e)
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

			PalettePath.Text = ofd.FileName;
			AutoLoadPalette.Checked = true;
			SetPaletteImage();
		}

		private void SetPaletteImage()
		{
			var pal = ResolvePalette(false);

			int w = pictureBoxPalette.Size.Width;
			int h = pictureBoxPalette.Size.Height;

			bmp = new Bitmap(w, h);
			for (int j = 0; j < h; j++)
			{
				int cy = j * 4 / h;
				for (int i = 0; i < w; i++)
				{
					int cx = i * 16 / w;
					int cindex = cy * 16 + cx;
					Color col = Color.FromArgb(0xff, pal[cindex, 0], pal[cindex, 1], pal[cindex, 2]);
					bmp.SetPixel(i, j, col);
				}
			}
			pictureBoxPalette.Image = bmp;
		}

		private byte[,] ResolvePalette(bool showmsg = false)
		{
			if (AutoLoadPalette.Checked) // checkbox checked: try to load palette from file
			{
				if (PalettePath.Text.Length > 0)
				{
					HawkFile palette = new HawkFile(PalettePath.Text);

					if (palette != null && palette.Exists)
					{
						var data = Palettes.Load_FCEUX_Palette(HawkFile.ReadAllBytes(palette.Name));
						if (showmsg) GlobalWin.OSD.AddMessage("Palette file loaded: " + palette.Name);
						return data;
					}
					else
					{
						return settings.Palette;
					}
				}
				else // no filename: interpret this as "reset to default"
				{
					if (showmsg) GlobalWin.OSD.AddMessage("Standard Palette set");
					return (byte[,])Palettes.QuickNESPalette.Clone();
				}
			}
			else // checkbox unchecked: we're reusing whatever palette was set
			{
				return settings.Palette;
			}
		}


		private void OK_Click(object sender, EventArgs e)
		{
			settings.Palette = ResolvePalette(true);

			settings.NTSC_TopLine = (int)NTSC_FirstLineNumeric.Value;
			settings.NTSC_BottomLine = (int)NTSC_LastLineNumeric.Value;
			settings.PAL_TopLine = (int)PAL_FirstLineNumeric.Value;
			settings.PAL_BottomLine = (int)PAL_LastLineNumeric.Value;
			settings.AllowMoreThanEightSprites = AllowMoreSprites.Checked;
			settings.ClipLeftAndRight = ClipLeftAndRightCheckBox.Checked;
			settings.DispSprites = DispSprites.Checked;
			settings.DispBackground = DispBackground.Checked;
			settings.BackgroundColor = BGColorDialog.Color.ToArgb();
			if (!checkUseBackdropColor.Checked)
				settings.BackgroundColor &= 0x00FFFFFF;

			nes.PutSettings(settings);
			Close();
		}

		private void SetColorBox()
		{
			int color = BGColorDialog.Color.ToArgb();
			BackGroundColorNumber.Text = String.Format("{0:X8}", color).Substring(2,6);
			BackgroundColorPanel.BackColor = BGColorDialog.Color;
		}

		private void ChangeBGColor_Click(object sender, EventArgs e)
		{
			ChangeBG();
		}

		private void ChangeBG()
		{
			if (BGColorDialog.ShowDialog() == DialogResult.OK)
			{
				SetColorBox();
			}
		}

		private void btnAreaStandard_Click(object sender, EventArgs e)
		{
			NTSC_FirstLineNumeric.Value = 8;
			NTSC_LastLineNumeric.Value = 231;
		}

		private void btnAreaFull_Click(object sender, EventArgs e)
		{
			NTSC_FirstLineNumeric.Value = 0;
			NTSC_LastLineNumeric.Value = 239;
		}

		private void BackgroundColorPanel_DoubleClick(object sender, EventArgs e)
		{
			ChangeBG();
		}

		private void RestoreDefaultsButton_Click(object sender, EventArgs e)
		{
			settings = new NES.NESSettings();
			LoadStuff();
		}

		private void AutoLoadPalette_Click(object sender, EventArgs e)
		{
			SetPaletteImage();
		}
	}
}
