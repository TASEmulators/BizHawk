using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Emulation.Cores.Sony.PSX;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class PSXOptions : Form
	{
		public PSXOptions(Octoshock.Settings settings)
		{
			InitializeComponent();
			_settings = settings;

			rbPixelPro.Checked = _settings.ResolutionMode == Octoshock.eResolutionMode.PixelPro;
			rbDebugMode.Checked = _settings.ResolutionMode == Octoshock.eResolutionMode.Debug;
			rbMednafenMode.Checked = _settings.ResolutionMode == Octoshock.eResolutionMode.Mednafen;
			rbTweakedMednafenMode.Checked = _settings.ResolutionMode == Octoshock.eResolutionMode.TweakedMednafen;
			checkClipHorizontal.Checked = _settings.ClipHorizontalOverscan;

			NTSC_FirstLineNumeric.Value = _settings.ScanlineStart_NTSC;
			NTSC_LastLineNumeric.Value = _settings.ScanlineEnd_NTSC;
			PAL_FirstLineNumeric.Value = _settings.ScanlineStart_PAL;
			PAL_LastLineNumeric.Value = _settings.ScanlineEnd_PAL;
		}

		Octoshock.Settings _settings;
		bool _dispSettingsSet = false;

		private void btnNiceDisplayConfig_Click(object sender, EventArgs e)
		{
			_dispSettingsSet = true;
			MessageBox.Show("Finetuned Display Options will take effect if you OK from PSX Options");
		}

		public static DialogResult DoSettingsDialog(IWin32Window owner)
		{
			var s = ((Octoshock)Global.Emulator).GetSettings();
			var ss = ((Octoshock)Global.Emulator).GetSyncSettings();
			var dlg = new PSXOptions(s);

			var result = dlg.ShowDialog(owner);
			return result;
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			if (_dispSettingsSet)
			{
				Global.Config.DispManagerAR = Config.EDispManagerAR.System;
				Global.Config.DispFixAspectRatio = true;
				Global.Config.DispFixScaleInteger = false;
				Global.Config.DispFinalFilter = 1; //bilinear, I hope
			}

			if(rbPixelPro.Checked) _settings.ResolutionMode = Octoshock.eResolutionMode.PixelPro;
			if(rbDebugMode.Checked) _settings.ResolutionMode = Octoshock.eResolutionMode.Debug;
			if(rbMednafenMode.Checked)_settings.ResolutionMode = Octoshock.eResolutionMode.Mednafen;
			if(rbTweakedMednafenMode.Checked)_settings.ResolutionMode = Octoshock.eResolutionMode.TweakedMednafen;

			_settings.ClipHorizontalOverscan = checkClipHorizontal.Checked;

			_settings.ScanlineStart_NTSC = (int)NTSC_FirstLineNumeric.Value;
			_settings.ScanlineEnd_NTSC = (int)NTSC_LastLineNumeric.Value;
			_settings.ScanlineStart_PAL = (int)PAL_FirstLineNumeric.Value;
			_settings.ScanlineEnd_PAL = (int)PAL_LastLineNumeric.Value;

			_settings.Validate();

			GlobalWin.MainForm.PutCoreSettings(_settings);

			DialogResult = DialogResult.OK;
			Close();
		}

		private void btnAreaFull_Click(object sender, EventArgs e)
		{
			NTSC_FirstLineNumeric.Value = 0;
			NTSC_LastLineNumeric.Value = 239;
			PAL_FirstLineNumeric.Value = 0;
			PAL_LastLineNumeric.Value = 287;
		}

	}
}
