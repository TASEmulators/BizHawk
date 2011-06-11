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
		//Add restriction on for load event for nes
		//Add restriction on Main form menu item for nes
		//Add palette config in NES path config
		//Hook up allow > 8 scan lines
		//Hook up Clip L+R Sides
		//Hook up Disp Background
		//Hook up Disp Sprites
		//Hook up BG color
		//Allow selection of palette file from archive
		//Hotkeys for BG & Sprite display toggle
		//allow null in box
		//select all on enter event for palette config
		//NTSC fileter settings? Hue, Tint (This should probably be a multiclient thing, not a nes specific thing?)
		//Color panel isn't loading color on load

		HawkFile palette = null;
		NES nes;

		public NESGraphicsConfig()
		{
			InitializeComponent();
		}

		private void NESGraphicsConfig_Load(object sender, EventArgs e)
		{
			nes = Global.Emulator as NES;
			AllowMoreSprites.Checked = Global.Config.NESAllowMoreThanEightSprites;
			ClipLeftAndRightCheckBox.Checked = Global.Config.NESClipLeftAndRight;
			AutoLoadPalette.Checked = Global.Config.NESAutoLoadPalette;
			PalettePath.Text = Global.Config.NESPaletteFile;
			DispSprites.Checked = Global.Config.NESDispSprites;
			DispBackground.Checked = Global.Config.NESDispBackground;
			BGColorDialog.Color = Color.FromArgb(Global.Config.NESBackgroundColor);
			SetColorBox();
		}

		private void BrowsePalette_Click(object sender, EventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog();
			ofd.InitialDirectory = PathManager.GetPlatformBase("NES");
			ofd.Filter = "Palette Files (.pal)|*.PAL|All Files (*.*)|*.*";
			ofd.RestoreDirectory = true;

			var result = ofd.ShowDialog();
			if (result != DialogResult.OK)
				return;

			PalettePath.Text = ofd.FileName;
		}

		private void OK_Click(object sender, EventArgs e)
		{
			string path = PathManager.MakeAbsolutePath(PalettePath.Text, "NES");
			palette = new HawkFile(PalettePath.Text);

			if (palette != null && palette.Exists)
			{
				if (Global.Config.NESPaletteFile != palette.Name)
				{
					Global.Config.NESPaletteFile = palette.Name;
					nes.SetPalette(NES.Palettes.Load_FCEUX_Palette(HawkFile.ReadAllBytes(palette.Name)));
					Global.RenderPanel.AddMessage("Palette file loaded: " + palette.Name);
				}
				Global.Config.NESAllowMoreThanEightSprites = AllowMoreSprites.Checked;
				Global.Config.NESClipLeftAndRight = ClipLeftAndRightCheckBox.Checked;
				Global.Config.NESAutoLoadPalette = AutoLoadPalette.Checked;
				 Global.Config.NESDispSprites = DispSprites.Checked;
				Global.Config.NESDispBackground = DispBackground.Checked;
			}

			this.Close();
		}

		private void SetColorBox()
		{
			int color = BGColorDialog.Color.ToArgb();
			BackGroundColorNumber.Text = String.Format("{0:X8}", color);
			BackgroundColorPanel.BackColor = BGColorDialog.Color;
		}

		private void ChangeBGColor_Click(object sender, EventArgs e)
		{
			if (BGColorDialog.ShowDialog() == DialogResult.OK)
				SetColorBox();
		}
	}
}
