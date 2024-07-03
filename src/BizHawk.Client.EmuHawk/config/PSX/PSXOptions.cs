using System.Drawing;
using System.Windows.Forms;

using BizHawk.Emulation.Cores.Sony.PSX;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class PSXOptions : Form, IDialogParent
	{
		// backups of the labels for string replacing
		private readonly string _lblPixelProText, _lblMednafenText, _lblTweakedMednafenText;

		private readonly ISettingsAdapter _settable;

		public IDialogController DialogController { get; }

		private PSXOptions(
			Config config,
			IDialogController dialogController,
			ISettingsAdapter settable,
			Octoshock.Settings settings,
			Octoshock.SyncSettings syncSettings,
			OctoshockDll.eVidStandard vidStandard,
			Size currentVideoSize)
		{
			InitializeComponent();
			_config = config;
			_settable = settable;
			_settings = settings;
			_syncSettings = syncSettings;
			_previewVideoStandard = vidStandard;
			_previewVideoSize = currentVideoSize;
			DialogController = dialogController;

			if (_previewVideoStandard == OctoshockDll.eVidStandard.NTSC)
			{
				lblNTSC.Font = new Font(lblNTSC.Font, FontStyle.Bold);
			}
			else
			{
				lblPAL.Font = new Font(lblPAL.Font, FontStyle.Bold);
			}

			_lblPixelProText = lblPixelPro.Text;
			_lblMednafenText = lblMednafen.Text;
			_lblTweakedMednafenText = lblTweakedMednafen.Text;

			rbPixelPro.Checked = _settings.ResolutionMode == Octoshock.eResolutionMode.PixelPro;
			rbDebugMode.Checked = _settings.ResolutionMode == Octoshock.eResolutionMode.Debug;
			rbMednafenMode.Checked = _settings.ResolutionMode == Octoshock.eResolutionMode.Mednafen;
			rbTweakedMednafenMode.Checked = _settings.ResolutionMode == Octoshock.eResolutionMode.TweakedMednafen;
			rbClipNone.Checked = _settings.HorizontalClipping == Octoshock.eHorizontalClipping.None;
			rbClipBasic.Checked = _settings.HorizontalClipping == Octoshock.eHorizontalClipping.Basic;
			rbClipToFramebuffer.Checked = _settings.HorizontalClipping == Octoshock.eHorizontalClipping.Framebuffer;

			cbLEC.Checked = _syncSettings.EnableLEC;
			cbGpuLag.Checked = _settings.GPULag;

			rbWeave.Checked = _settings.DeinterlaceMode == Octoshock.eDeinterlaceMode.Weave;
			rbBob.Checked = _settings.DeinterlaceMode == Octoshock.eDeinterlaceMode.Bob;
			rbBobOffset.Checked = _settings.DeinterlaceMode == Octoshock.eDeinterlaceMode.BobOffset;

			NTSC_FirstLineNumeric.Value = _settings.ScanlineStart_NTSC;
			NTSC_LastLineNumeric.Value = _settings.ScanlineEnd_NTSC;
			PAL_FirstLineNumeric.Value = _settings.ScanlineStart_PAL;
			PAL_LastLineNumeric.Value = _settings.ScanlineEnd_PAL;
		}

		private Size _previewVideoSize;
		private readonly Config _config;
		private readonly OctoshockDll.eVidStandard _previewVideoStandard;
		private readonly Octoshock.Settings _settings;
		private readonly Octoshock.SyncSettings _syncSettings;
		private bool _dispSettingsSet;

		private void BtnNiceDisplayConfig_Click(object sender, EventArgs e)
		{
			_dispSettingsSet = true;
			DialogController.ShowMessageBox("Finetuned Display Options will take effect if you OK from PSX Options");
		}

		public static DialogResult DoSettingsDialog(
			Config config,
			IDialogParent dialogParent,
			ISettingsAdapter settable,
			OctoshockDll.eVidStandard vidStandard,
			Size vidSize)
		{
			using PSXOptions dlg = new(
				config,
				dialogParent.DialogController,
				settable,
				(Octoshock.Settings) settable.GetSettings(),
				(Octoshock.SyncSettings) settable.GetSyncSettings(),
				vidStandard,
				vidSize);
			return dialogParent.ShowDialogAsChild(dlg);
		}

		private void SyncSettingsFromGui(Octoshock.Settings settings, Octoshock.SyncSettings syncSettings)
		{
			if (rbPixelPro.Checked) settings.ResolutionMode = Octoshock.eResolutionMode.PixelPro;
			if (rbDebugMode.Checked) settings.ResolutionMode = Octoshock.eResolutionMode.Debug;
			if (rbMednafenMode.Checked) settings.ResolutionMode = Octoshock.eResolutionMode.Mednafen;
			if (rbTweakedMednafenMode.Checked) settings.ResolutionMode = Octoshock.eResolutionMode.TweakedMednafen;

			if (rbClipNone.Checked) settings.HorizontalClipping = Octoshock.eHorizontalClipping.None;
			if (rbClipBasic.Checked) settings.HorizontalClipping = Octoshock.eHorizontalClipping.Basic;
			if (rbClipToFramebuffer.Checked) settings.HorizontalClipping = Octoshock.eHorizontalClipping.Framebuffer;

			if (rbWeave.Checked) _settings.DeinterlaceMode = Octoshock.eDeinterlaceMode.Weave;
			if (rbBob.Checked) _settings.DeinterlaceMode = Octoshock.eDeinterlaceMode.Bob;
			if (rbBobOffset.Checked) _settings.DeinterlaceMode = Octoshock.eDeinterlaceMode.BobOffset;

			settings.ScanlineStart_NTSC = (int)NTSC_FirstLineNumeric.Value;
			settings.ScanlineEnd_NTSC = (int)NTSC_LastLineNumeric.Value;
			settings.ScanlineStart_PAL = (int)PAL_FirstLineNumeric.Value;
			settings.ScanlineEnd_PAL = (int)PAL_LastLineNumeric.Value;

			settings.GPULag = cbGpuLag.Checked;

			syncSettings.EnableLEC = cbLEC.Checked;
		}

		private void BtnOk_Click(object sender, EventArgs e)
		{
			if (_dispSettingsSet)
			{
				_config.DispManagerAR = EDispManagerAR.System;
				_config.DispFixAspectRatio = true;
				_config.DispFixScaleInteger = false;
				_config.DispFinalFilter = 1; // bilinear, I hope
			}

			SyncSettingsFromGui(_settings, _syncSettings);
			_settings.Validate();
			_settable.PutCoreSettings(_settings);
			_settable.PutCoreSyncSettings(_syncSettings);

			DialogResult = DialogResult.OK;
			Close();
		}

		private void BtnAreaFull_Click(object sender, EventArgs e)
		{
			NTSC_FirstLineNumeric.Value = 0;
			NTSC_LastLineNumeric.Value = 239;
			PAL_FirstLineNumeric.Value = 0;
			PAL_LastLineNumeric.Value = 287;
			SyncLabels();
		}

		private void SyncLabels()
		{
			var temp = _settings.Clone();
			var syncTemp = _syncSettings.Clone();
			SyncSettingsFromGui(temp, syncTemp);
			_settings.Validate();

			// actually, I think this is irrelevant. But it's nice in case we want to do some kind of a more detailed simulation later
			int w = _previewVideoSize.Width;
			int h = _previewVideoSize.Height;

			temp.ResolutionMode = Octoshock.eResolutionMode.PixelPro;
			var ri = Octoshock.CalculateResolution(_previewVideoStandard, temp, w, h);
			lblPixelPro.Text = _lblPixelProText.Replace("800x480", $"{ri.Resolution.Width}x{ri.Resolution.Height}");

			temp.ResolutionMode = Octoshock.eResolutionMode.Mednafen;
			ri = Octoshock.CalculateResolution(_previewVideoStandard, temp, w, h);
			lblMednafen.Text = _lblMednafenText.Replace("320x240", $"{ri.Resolution.Width}x{ri.Resolution.Height}");

			temp.ResolutionMode = Octoshock.eResolutionMode.TweakedMednafen;
			ri = Octoshock.CalculateResolution(_previewVideoStandard, temp, w, h);
			lblTweakedMednafen.Text = _lblTweakedMednafenText.Replace("400x300", $"{ri.Resolution.Width}x{ri.Resolution.Height}");
		}

		private void DrawingArea_ValueChanged(object sender, EventArgs e)
		{
			SyncLabels();
		}

		private void RbClipHorizontal_CheckedChanged(object sender, EventArgs e)
		{
			SyncLabels();
		}

		private void RbClipToFramebuffer_CheckedChanged(object sender, EventArgs e)
		{
			SyncLabels();
		}

		private void RbClipNone_CheckedChanged(object sender, EventArgs e)
		{
			SyncLabels();
		}

		private void LinkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			DialogController.ShowMessageBox($@"These options control BizHawk's Display Options to make it act quite a lot like Mednafen:

{nameof(_config.DispManagerAR)} = System (Use emulator-recommended AR)
{nameof(_config.DispFixAspectRatio)} = true (Maintain aspect ratio [letterbox main window as needed])
{nameof(_config.DispFinalFilter)} = bilinear (Like Mednafen)
{nameof(_config.DispFixScaleInteger)} = false (Generally unwanted with bilinear filtering)

This is a good place to write that Mednafen's default behaviour is fantastic for gaming!
But: 1. we think we improved on it a tiny bit with the tweaked mode
And: 2. It's not suitable for detailed scrutinizing of graphics
");
		}
	}
}
