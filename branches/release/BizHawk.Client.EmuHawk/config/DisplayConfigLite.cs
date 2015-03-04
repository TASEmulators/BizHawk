using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using BizHawk.Emulation.Common;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk.config
{
	public partial class DisplayConfigLite : Form
	{
		string PathSelection;
		public DisplayConfigLite()
		{
			InitializeComponent();

			rbNone.Checked = Global.Config.TargetDisplayFilter == 0;
			rbHq2x.Checked = Global.Config.TargetDisplayFilter == 1;
			rbScanlines.Checked = Global.Config.TargetDisplayFilter == 2;
			rbUser.Checked = Global.Config.TargetDisplayFilter == 3;

			PathSelection = Global.Config.DispUserFilterPath ?? "";
			RefreshState();

			rbFinalFilterNone.Checked = Global.Config.DispFinalFilter == 0;
			rbFinalFilterBilinear.Checked = Global.Config.DispFinalFilter == 1;
			rbFinalFilterBicubic.Checked = Global.Config.DispFinalFilter == 2;

			tbScanlineIntensity.Value = Global.Config.TargetScanlineFilterIntensity;
			checkLetterbox.Checked = Global.Config.DispFixAspectRatio;
			checkPadInteger.Checked = Global.Config.DispFixScaleInteger;
			checkFullscreenHacks.Checked = Global.Config.DispFullscreenHacks;

			rbOpenGL.Checked = Global.Config.DispMethod == Config.EDispMethod.OpenGL;
			rbGDIPlus.Checked = Global.Config.DispMethod == Config.EDispMethod.GdiPlus;
			rbD3D9.Checked = Global.Config.DispMethod == Config.EDispMethod.SlimDX9;

			cbStatusBarWindowed.Checked = Global.Config.DispChrome_StatusBarWindowed;
			cbCaptionWindowed.Checked = Global.Config.DispChrome_CaptionWindowed;
			cbMenuWindowed.Checked = Global.Config.DispChrome_MenuWindowed;
			cbStatusBarFullscreen.Checked = Global.Config.DispChrome_StatusBarFullscreen;
			cbMenuFullscreen.Checked = Global.Config.DispChrome_MenuFullscreen;
			trackbarFrameSizeWindowed.Value = Global.Config.DispChrome_FrameWindowed;
			SyncTrackbar();

			// null emulator config hack
			{
				NullEmulator.NullEmulatorSettings s;
				if (Global.Emulator is NullEmulator)
					s = (Global.Emulator as dynamic).GetSettings();
				else
					s = (NullEmulator.NullEmulatorSettings)Global.Config.GetCoreSettings<NullEmulator>();
				checkSnowyNullEmulator.Checked = s.SnowyDisplay;
			}

			if (Global.Config.DispManagerAR == Config.EDispManagerAR.None)
				rbUseRaw.Checked = true;
			else if (Global.Config.DispManagerAR == Config.EDispManagerAR.System)
				rbUseSystem.Checked = true;
			else if (Global.Config.DispManagerAR == Config.EDispManagerAR.Custom)
				rbUseCustom.Checked = true;

			txtCustomARWidth.Text = Global.Config.DispCustomUserARWidth.ToString();
			txtCustomARHeight.Text = Global.Config.DispCustomUserARHeight.ToString();

			RefreshAspectRatioOptions();
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			if (rbNone.Checked)
				Global.Config.TargetDisplayFilter = 0;
			if (rbHq2x.Checked)
				Global.Config.TargetDisplayFilter = 1;
			if (rbScanlines.Checked)
				Global.Config.TargetDisplayFilter = 2;
			if (rbUser.Checked)
				Global.Config.TargetDisplayFilter = 3;

			if (rbFinalFilterNone.Checked)
				Global.Config.DispFinalFilter = 0;
			if (rbFinalFilterBilinear.Checked)
				Global.Config.DispFinalFilter = 1;
			if (rbFinalFilterBicubic.Checked)
				Global.Config.DispFinalFilter = 2;

			Global.Config.TargetScanlineFilterIntensity = tbScanlineIntensity.Value;
			Global.Config.DispFixAspectRatio = checkLetterbox.Checked;
			Global.Config.DispFixScaleInteger = checkPadInteger.Checked;
			Global.Config.DispFullscreenHacks = checkFullscreenHacks.Checked;

			Global.Config.DispChrome_StatusBarWindowed = cbStatusBarWindowed.Checked;
			Global.Config.DispChrome_CaptionWindowed = cbCaptionWindowed.Checked;
			Global.Config.DispChrome_MenuWindowed = cbMenuWindowed.Checked;
			Global.Config.DispChrome_StatusBarFullscreen = cbStatusBarFullscreen.Checked;
			Global.Config.DispChrome_MenuFullscreen = cbMenuFullscreen.Checked;
			Global.Config.DispChrome_FrameWindowed = trackbarFrameSizeWindowed.Value;

			// HACK:: null emulator's settings don't persist to config normally
			{
				NullEmulator.NullEmulatorSettings s;
				if (Global.Emulator is NullEmulator)
					s = (Global.Emulator as dynamic).GetSettings();
				else
					s = (NullEmulator.NullEmulatorSettings)Global.Config.GetCoreSettings<NullEmulator>();
				s.SnowyDisplay = checkSnowyNullEmulator.Checked;

				Global.Config.PutCoreSettings<NullEmulator>(s);
				if (Global.Emulator is NullEmulator)
					(Global.Emulator as dynamic).PutSettings(s);
			}

			if (rbUseRaw.Checked)
				Global.Config.DispManagerAR = Config.EDispManagerAR.None;
			else if (rbUseSystem.Checked)
				Global.Config.DispManagerAR = Config.EDispManagerAR.System;
			else if (rbUseCustom.Checked)
				Global.Config.DispManagerAR = Config.EDispManagerAR.Custom;

			int.TryParse(txtCustomARWidth.Text, out Global.Config.DispCustomUserARWidth);
			int.TryParse(txtCustomARHeight.Text, out Global.Config.DispCustomUserARHeight);

			if(rbOpenGL.Checked)
				Global.Config.DispMethod = Config.EDispMethod.OpenGL;
			if(rbGDIPlus.Checked)
				Global.Config.DispMethod = Config.EDispMethod.GdiPlus;
			if(rbD3D9.Checked)
				Global.Config.DispMethod = Config.EDispMethod.SlimDX9;

			Global.Config.DispUserFilterPath = PathSelection;
			GlobalWin.DisplayManager.RefreshUserShader();

			DialogResult = DialogResult.OK;
			Close();
		}

		void RefreshState()
		{
			lblUserFilterName.Text = Path.GetFileNameWithoutExtension(PathSelection);
		}

		private void btnSelectUserFilter_Click(object sender, EventArgs e)
		{
			var ofd = new OpenFileDialog();
			ofd.Filter = ".CGP (*.cgp)|*.cgp";
			ofd.FileName = PathSelection;
			if (ofd.ShowDialog() == DialogResult.OK)
			{
				rbUser.Checked = true;
				PathSelection = Path.GetFullPath(ofd.FileName);
				RefreshState();
			}
		}

		private void checkLetterbox_CheckedChanged(object sender, EventArgs e)
		{
			RefreshAspectRatioOptions();
		}
		private void checkPadInteger_CheckedChanged(object sender, EventArgs e)
		{
			RefreshAspectRatioOptions();
		}

		private void rbUseRaw_CheckedChanged(object sender, EventArgs e)
		{
			RefreshAspectRatioOptions();
		}

		private void rbUseSystem_CheckedChanged(object sender, EventArgs e)
		{
			RefreshAspectRatioOptions();
		}

		void RefreshAspectRatioOptions()
		{
			grpARSelection.Enabled = checkLetterbox.Checked;
			checkPadInteger.Enabled = checkLetterbox.Checked;
		}

		public void tbScanlineIntensity_Scroll(object sender, EventArgs e)
		{
			Global.Config.TargetScanlineFilterIntensity = tbScanlineIntensity.Value;
			int scanlines = Global.Config.TargetScanlineFilterIntensity;
			float percentage = (float) scanlines / 255 * 100;
			if (percentage > 100) percentage = 100;
			lblScanlines.Text = String.Format("{0:F2}", percentage) + "%";
		}

		private void trackbarFrameSizeWindowed_ValueChanged(object sender, EventArgs e)
		{
			SyncTrackbar();
		}

		void SyncTrackbar()
		{
			if (trackbarFrameSizeWindowed.Value == 0)
				lblFrameTypeWindowed.Text = "None";
			if (trackbarFrameSizeWindowed.Value == 1)
				lblFrameTypeWindowed.Text = "Thin";
			if (trackbarFrameSizeWindowed.Value == 2)
				lblFrameTypeWindowed.Text = "Thick";
		}

	}
}
