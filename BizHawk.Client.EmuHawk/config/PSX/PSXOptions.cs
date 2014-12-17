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
		//backups of the labels for string replacing
		string lblPixelPro_text, lblMednafen_text, lblTweakedMednafen_text;
		
		public PSXOptions(Octoshock.Settings settings, OctoshockDll.eVidStandard vidStandard, Size currentVideoSize)
		{
			InitializeComponent();
			_settings = settings;
			_previewVideoStandard = vidStandard;
			_previewVideoSize = currentVideoSize;

			if (_previewVideoStandard == OctoshockDll.eVidStandard.NTSC)
				lblNTSC.Font = new System.Drawing.Font(lblNTSC.Font, FontStyle.Bold);
			else lblPAL.Font = new System.Drawing.Font(lblPAL.Font, FontStyle.Bold);

			lblPixelPro_text = lblPixelPro.Text;
			lblMednafen_text = lblMednafen.Text;
			lblTweakedMednafen_text = lblTweakedMednafen.Text;

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

		Size _previewVideoSize;
		OctoshockDll.eVidStandard _previewVideoStandard;
		Octoshock.Settings _settings;
		bool _dispSettingsSet = false;

		private void btnNiceDisplayConfig_Click(object sender, EventArgs e)
		{
			_dispSettingsSet = true;
			MessageBox.Show("Finetuned Display Options will take effect if you OK from PSX Options");
		}

		public static DialogResult DoSettingsDialog(IWin32Window owner)
		{
			var psx = ((Octoshock)Global.Emulator);
			var s = psx.GetSettings();
			var ss = psx.GetSyncSettings();
			var vid = psx.SystemVidStandard;
			var size = psx.CurrentVideoSize; 
			var dlg = new PSXOptions(s,vid,size);

			var result = dlg.ShowDialog(owner);
			return result;
		}

		void SyncGuiToTheseSettings(Octoshock.Settings settings)
		{
			if (rbPixelPro.Checked) settings.ResolutionMode = Octoshock.eResolutionMode.PixelPro;
			if (rbDebugMode.Checked) settings.ResolutionMode = Octoshock.eResolutionMode.Debug;
			if (rbMednafenMode.Checked) settings.ResolutionMode = Octoshock.eResolutionMode.Mednafen;
			if (rbTweakedMednafenMode.Checked) settings.ResolutionMode = Octoshock.eResolutionMode.TweakedMednafen;

			settings.ClipHorizontalOverscan = checkClipHorizontal.Checked;

			settings.ScanlineStart_NTSC = (int)NTSC_FirstLineNumeric.Value;
			settings.ScanlineEnd_NTSC = (int)NTSC_LastLineNumeric.Value;
			settings.ScanlineStart_PAL = (int)PAL_FirstLineNumeric.Value;
			settings.ScanlineEnd_PAL = (int)PAL_LastLineNumeric.Value;
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

			SyncGuiToTheseSettings(_settings);
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
			SyncLabels();
		}

		void SyncLabels()
		{
			var temp = _settings.Clone();
			SyncGuiToTheseSettings(temp);
			_settings.Validate();

			//actually, I think this is irrelevant. But it's nice in case we want to do some kind of a more detailed simulation later
			int w = _previewVideoSize.Width;
			int h = _previewVideoSize.Height;

			temp.ResolutionMode = Octoshock.eResolutionMode.PixelPro;
			var size = Octoshock.CalculateResolution(_previewVideoStandard, temp, w, h);
			lblPixelPro.Text = lblPixelPro_text.Replace("800x480", string.Format("{0}x{1}", size.Width, size.Height)); ;

			temp.ResolutionMode = Octoshock.eResolutionMode.Mednafen;
			size = Octoshock.CalculateResolution(_previewVideoStandard, temp, w, h);
			lblMednafen.Text = lblMednafen_text.Replace("320x240", string.Format("{0}x{1}", size.Width, size.Height));

			temp.ResolutionMode = Octoshock.eResolutionMode.TweakedMednafen;
			size = Octoshock.CalculateResolution(_previewVideoStandard, temp, w, h);
			lblTweakedMednafen.Text = lblTweakedMednafen_text.Replace("400x300", string.Format("{0}x{1}", size.Width, size.Height));
		}

		private void DrawingArea_ValueChanged(object sender, EventArgs e)
		{
			SyncLabels();
		}

		private void checkClipHorizontal_CheckedChanged(object sender, EventArgs e)
		{
			SyncLabels();
		}
	}
}
