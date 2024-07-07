using System.Drawing;
using System.Windows.Forms;

using BizHawk.Common;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.NES;

namespace BizHawk.Client.EmuHawk
{
	public partial class NESGraphicsConfig : Form, IDialogParent
	{
		// TODO:
		// Allow selection of palette file from archive
		// Hotkeys for BG & Sprite display toggle
		// NTSC filter settings? Hue, Tint (This should probably be a client thing, not a nes specific thing?)
		private readonly Config _config;

		private readonly ISettingsAdapter _settable;

		private NES.NESSettings _settings;
		//private Bitmap _bmp;

		public IDialogController DialogController { get; }

		public NESGraphicsConfig(
			Config config,
			IDialogController dialogController,
			ISettingsAdapter settable)
		{
			_config = config;
			_settable = settable;
			_settings = (NES.NESSettings) _settable.GetSettings();
			DialogController = dialogController;
			InitializeComponent();
		}

		private void NESGraphicsConfig_Load(object sender, EventArgs e)
		{
			LoadStuff();
		}

		private void LoadStuff()
		{
			NTSC_FirstLineNumeric.Value = _settings.NTSC_TopLine;
			NTSC_LastLineNumeric.Value = _settings.NTSC_BottomLine;
			PAL_FirstLineNumeric.Value = _settings.PAL_TopLine;
			PAL_LastLineNumeric.Value = _settings.PAL_BottomLine;
			AllowMoreSprites.Checked = _settings.AllowMoreThanEightSprites;
			ClipLeftAndRightCheckBox.Checked = _settings.ClipLeftAndRight;
			DispSprites.Checked = _settings.DispSprites;
			DispBackground.Checked = _settings.DispBackground;
			BGColorDialog.Color = Color.FromArgb(unchecked(_settings.BackgroundColor | (int)0xFF000000));
			checkUseBackdropColor.Checked = (_settings.BackgroundColor & 0xFF000000) != 0;
			SetColorBox();
			SetPaletteImage();
		}

		private void BrowsePalette_Click(object sender, EventArgs e)
		{
			var result = this.ShowFileOpenDialog(
				discardCWDChange: true,
				filter: FilesystemFilterSet.Palettes,
				initDir: _config.PathEntries.PalettesAbsolutePathFor(VSystemID.Raw.NES));
			if (result is null) return;
			PalettePath.Text = result;
			AutoLoadPalette.Checked = true;
			SetPaletteImage();
		}

		private void SetPaletteImage()
		{
			var pal = ResolvePalette();

			int w = pictureBoxPalette.Size.Width;
			int h = pictureBoxPalette.Size.Height;

			var bmp = new Bitmap(w, h);
			for (int j = 0; j < h; j++)
			{
				int cy = j * 4 / h;
				for (int i = 0; i < w; i++)
				{
					int cx = i * 16 / w;
					int cIndex = (cy * 16) + cx;
					Color col = Color.FromArgb(0xff, pal[cIndex, 0], pal[cIndex, 1], pal[cIndex, 2]);
					bmp.SetPixel(i, j, col);
				}
			}

			pictureBoxPalette.Image = bmp;
		}

		private byte[,] ResolvePalette(bool showMsg = false)
		{
			if (AutoLoadPalette.Checked) // checkbox checked: try to load palette from file
			{
				if (PalettePath.Text.Length > 0)
				{
					var palette = new HawkFile(PalettePath.Text);

					if (palette.Exists)
					{
						var data = Palettes.Load_FCEUX_Palette(palette.ReadAllBytes());
						if (showMsg)
						{
							DialogController.AddOnScreenMessage($"Palette file loaded: {palette.Name}");
						}

						return data;
					}

					return _settings.Palette;
				}
				
				// no filename: interpret this as "reset to default"
				if (showMsg)
				{
					DialogController.AddOnScreenMessage("Standard Palette set");
				}

				return (byte[,])Palettes.QuickNESPalette.Clone();
				
			}
			
			// checkbox unchecked: we're reusing whatever palette was set
			return _settings.Palette;
		}

		private void Ok_Click(object sender, EventArgs e)
		{
			_settings.Palette = ResolvePalette(true);

			_settings.NTSC_TopLine = (int)NTSC_FirstLineNumeric.Value;
			_settings.NTSC_BottomLine = (int)NTSC_LastLineNumeric.Value;
			_settings.PAL_TopLine = (int)PAL_FirstLineNumeric.Value;
			_settings.PAL_BottomLine = (int)PAL_LastLineNumeric.Value;
			_settings.AllowMoreThanEightSprites = AllowMoreSprites.Checked;
			_settings.ClipLeftAndRight = ClipLeftAndRightCheckBox.Checked;
			_settings.DispSprites = DispSprites.Checked;
			_settings.DispBackground = DispBackground.Checked;
			_settings.BackgroundColor = BGColorDialog.Color.ToArgb();
			if (!checkUseBackdropColor.Checked)
			{
				_settings.BackgroundColor &= 0x00FFFFFF;
			}

			_settable.PutCoreSettings(_settings);
			Close();
		}

		private void SetColorBox()
		{
			int color = BGColorDialog.Color.ToArgb();
			BackGroundColorNumber.Text = $"{color:X8}".Substring(2, 6);
			BackgroundColorPanel.BackColor = BGColorDialog.Color;
		}

		private void ChangeBGColor_Click(object sender, EventArgs e)
		{
			ChangeBG();
		}

		private void ChangeBG()
		{
			if (BGColorDialog.ShowDialog().IsOk())
			{
				SetColorBox();
			}
		}

		private void BtnAreaStandard_Click(object sender, EventArgs e)
		{
			NTSC_FirstLineNumeric.Value = 8;
			NTSC_LastLineNumeric.Value = 231;
		}

		private void BtnAreaFull_Click(object sender, EventArgs e)
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
			_settings = new NES.NESSettings();
			LoadStuff();
		}

		private void AutoLoadPalette_Click(object sender, EventArgs e)
		{
			SetPaletteImage();
		}
	}
}
