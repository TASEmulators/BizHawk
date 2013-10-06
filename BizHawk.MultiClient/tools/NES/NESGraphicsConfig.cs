using System;
using System.Drawing;
using System.Windows.Forms;
using BizHawk.Emulation.Consoles.Nintendo;

namespace BizHawk.MultiClient
{
	public partial class NESGraphicsConfig : Form
	{
		//TODO:
		//Allow selection of palette file from archive
		//Hotkeys for BG & Sprite display toggle
		//NTSC filter settings? Hue, Tint (This should probably be a multiclient thing, not a nes specific thing?)

		private HawkFile palette;
		private NES nes;

		public NESGraphicsConfig()
		{
			InitializeComponent();
		}

		private void NESGraphicsConfig_Load(object sender, EventArgs e)
		{
			nes = Global.Emulator as NES;
			LoadStuff();
		}

		private void LoadStuff()
		{
			NTSC_FirstLineNumeric.Value = Global.Config.NTSC_NESTopLine;
			NTSC_LastLineNumeric.Value = Global.Config.NTSC_NESBottomLine;
			PAL_FirstLineNumeric.Value = Global.Config.PAL_NESTopLine;
			PAL_LastLineNumeric.Value = Global.Config.PAL_NESBottomLine;
			AllowMoreSprites.Checked = Global.Config.NESAllowMoreThanEightSprites;
			ClipLeftAndRightCheckBox.Checked = Global.Config.NESClipLeftAndRight;
			AutoLoadPalette.Checked = Global.Config.NESAutoLoadPalette;
			PalettePath.Text = Global.Config.NESPaletteFile;
			DispSprites.Checked = Global.Config.NESDispSprites;
			DispBackground.Checked = Global.Config.NESDispBackground;
			BGColorDialog.Color = Color.FromArgb(unchecked(Global.Config.NESBackgroundColor | (int)0xFF000000));
			checkUseBackdropColor.Checked = (Global.Config.NESBackgroundColor & 0xFF000000) != 0;
			SetColorBox();
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
		}

		private void OK_Click(object sender, EventArgs e)
		{
			if (PalettePath.Text.Length > 0)
			{
				palette = new HawkFile(PalettePath.Text);

				if (palette != null && palette.Exists)
				{
					if (Global.Config.NESPaletteFile != palette.Name)
					{
						Global.Config.NESPaletteFile = palette.Name;
						nes.SetPalette(NES.Palettes.Load_FCEUX_Palette(HawkFile.ReadAllBytes(palette.Name)));
						Global.OSD.AddMessage("Palette file loaded: " + palette.Name);
					}
				}
			}
			else
			{
				Global.Config.NESPaletteFile = "";
				nes.SetPalette(NES.Palettes.FCEUX_Standard);
				Global.OSD.AddMessage("Standard Palette set");
			}

			Global.Config.NTSC_NESTopLine = (int)NTSC_FirstLineNumeric.Value;
			nes.NTSC_FirstDrawLine = (int)NTSC_FirstLineNumeric.Value;

			Global.Config.NTSC_NESBottomLine = (int)NTSC_LastLineNumeric.Value;
			nes.NTSC_LastDrawLine = (int)NTSC_LastLineNumeric.Value;

			Global.Config.PAL_NESTopLine = (int)PAL_FirstLineNumeric.Value;
			nes.PAL_FirstDrawLine = (int)PAL_FirstLineNumeric.Value;

			Global.Config.PAL_NESBottomLine = (int)PAL_LastLineNumeric.Value;
			nes.PAL_LastDrawLine = (int)PAL_LastLineNumeric.Value;

			Global.Config.NESAllowMoreThanEightSprites = AllowMoreSprites.Checked;
			Global.Config.NESClipLeftAndRight = ClipLeftAndRightCheckBox.Checked;
			nes.SetClipLeftAndRight(ClipLeftAndRightCheckBox.Checked);
			Global.Config.NESAutoLoadPalette = AutoLoadPalette.Checked;
			Global.Config.NESDispSprites = DispSprites.Checked;
			Global.Config.NESDispBackground = DispBackground.Checked;
			Global.Config.NESBackgroundColor = BGColorDialog.Color.ToArgb();
			if (!checkUseBackdropColor.Checked)
			{
				Global.Config.NESBackgroundColor &= 0x00FFFFFF;
			}
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
			NTSC_FirstLineNumeric.Value = 8;
			NTSC_LastLineNumeric.Value = 231;
			PAL_FirstLineNumeric.Value = 0;
			PAL_LastLineNumeric.Value = 239;
			AllowMoreSprites.Checked = false;
			ClipLeftAndRightCheckBox.Checked = false;
			AutoLoadPalette.Checked = true;
			PalettePath.Text = "";
			DispSprites.Checked = true;
			DispBackground.Checked = true;
			BGColorDialog.Color = Color.FromArgb(unchecked(0 | (int)0xFF000000));
			checkUseBackdropColor.Checked = false;
			SetColorBox();
		}
	}
}
