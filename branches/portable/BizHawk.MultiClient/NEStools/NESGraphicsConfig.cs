using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using BizHawk.Emulation.Consoles.Nintendo;

namespace BizHawk.MultiClient
{
	public partial class NESGraphicsConfig : Form
	{
		//TODO:
		
		//Allow selection of palette file from archive
		//Hotkeys for BG & Sprite display toggle
		//NTSC filter settings? Hue, Tint (This should probably be a multiclient thing, not a nes specific thing?)

		HawkFile palette = null;
		NES nes;

		public NESGraphicsConfig()
		{
			InitializeComponent();
		}

		private void NESGraphicsConfig_Load(object sender, EventArgs e)
		{
			nes = Global.Emulator as NES;
			FirstLineNumeric.Value = Global.Config.NESTopLine;
			LastLineNumeric.Value = Global.Config.NESBottomLine;
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
			var ofd = HawkUIFactory.CreateOpenFileDialog();
			ofd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.PathNESPalette, "NES");
			ofd.Filter = "Palette Files (.pal)|*.PAL|All Files (*.*)|*.*";
			ofd.RestoreDirectory = true;

			var result = ofd.ShowDialog();
			if (result != DialogResult.OK)
				return;

			PalettePath.Text = ofd.FileName;
		}

		private void OK_Click(object sender, EventArgs e)
		{
			if (PalettePath.Text.Length > 0)
			{
				string path = PathManager.MakeAbsolutePath(PalettePath.Text, "NES");
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

			Global.Config.NESTopLine = (int)FirstLineNumeric.Value;
			Global.Config.NESBottomLine = (int)LastLineNumeric.Value;
			nes.FirstDrawLine = (int)FirstLineNumeric.Value;
			nes.LastDrawLine = (int)LastLineNumeric.Value;
			Global.Config.NESAllowMoreThanEightSprites = AllowMoreSprites.Checked;
			Global.Config.NESClipLeftAndRight = ClipLeftAndRightCheckBox.Checked;
			nes.SetClipLeftAndRight(ClipLeftAndRightCheckBox.Checked);
			Global.Config.NESAutoLoadPalette = AutoLoadPalette.Checked;
			Global.Config.NESDispSprites = DispSprites.Checked;
			Global.Config.NESDispBackground = DispBackground.Checked;
			Global.Config.NESBackgroundColor = BGColorDialog.Color.ToArgb();
			if (!checkUseBackdropColor.Checked)
				Global.Config.NESBackgroundColor &= 0x00FFFFFF;

			this.Close();
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
			FirstLineNumeric.Value = 8;
			LastLineNumeric.Value = 231;
		}

		private void btnAreaFull_Click(object sender, EventArgs e)
		{
			FirstLineNumeric.Value = 0;
			LastLineNumeric.Value = 239;
		}

		private void BackgroundColorPanel_DoubleClick(object sender, EventArgs e)
		{
			ChangeBG();
		}
	}
}
