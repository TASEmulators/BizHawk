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
		public bool NeedReset;

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
			cbFullscreenHacks.Checked = Global.Config.DispFullscreenHacks;
			cbAutoPrescale.Checked = Global.Config.DispAutoPrescale;

			cbAlternateVsync.Checked = Global.Config.DispAlternateVsync;

			if (Global.Config.DispSpeedupFeatures == 2) rbDisplayFull.Checked = true;
			if (Global.Config.DispSpeedupFeatures == 1) rbDisplayMinimal.Checked = true;
			if (Global.Config.DispSpeedupFeatures == 0) rbDisplayAbsoluteZero.Checked = true;

			rbOpenGL.Checked = Global.Config.DispMethod == Config.EDispMethod.OpenGL;
			rbGDIPlus.Checked = Global.Config.DispMethod == Config.EDispMethod.GdiPlus;
			rbD3D9.Checked = Global.Config.DispMethod == Config.EDispMethod.SlimDX9;

			cbStatusBarWindowed.Checked = Global.Config.DispChrome_StatusBarWindowed;
			cbCaptionWindowed.Checked = Global.Config.DispChrome_CaptionWindowed;
			cbMenuWindowed.Checked = Global.Config.DispChrome_MenuWindowed;
			cbStatusBarFullscreen.Checked = Global.Config.DispChrome_StatusBarFullscreen;
			cbMenuFullscreen.Checked = Global.Config.DispChrome_MenuFullscreen;
			trackbarFrameSizeWindowed.Value = Global.Config.DispChrome_FrameWindowed;
			cbFSAutohideMouse.Checked = Global.Config.DispChrome_Fullscreen_AutohideMouse;
			SyncTrackbar();

			cbAllowDoubleclickFullscreen.Checked = Global.Config.DispChrome_AllowDoubleClickFullscreen;

			nudPrescale.Value = Global.Config.DispPrescale;

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
			else if (Global.Config.DispManagerAR == Config.EDispManagerAR.CustomRatio)
				rbUseCustomRatio.Checked = true;

			if(Global.Config.DispCustomUserARWidth != -1)
				txtCustomARWidth.Text = Global.Config.DispCustomUserARWidth.ToString();
			if (Global.Config.DispCustomUserARHeight != -1)
				txtCustomARHeight.Text = Global.Config.DispCustomUserARHeight.ToString();
			if (Global.Config.DispCustomUserARX != -1)
				txtCustomARX.Text = Global.Config.DispCustomUserARX.ToString();
			if (Global.Config.DispCustomUserARY != -1)
				txtCustomARY.Text = Global.Config.DispCustomUserARY.ToString();

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

			Global.Config.DispPrescale = (int)nudPrescale.Value;

			Global.Config.TargetScanlineFilterIntensity = tbScanlineIntensity.Value;
			Global.Config.DispFixAspectRatio = checkLetterbox.Checked;
			Global.Config.DispFixScaleInteger = checkPadInteger.Checked;
			Global.Config.DispFullscreenHacks = cbFullscreenHacks.Checked;
			Global.Config.DispAutoPrescale = cbAutoPrescale.Checked;
			
			Global.Config.DispAlternateVsync = cbAlternateVsync.Checked;

			Global.Config.DispChrome_StatusBarWindowed = cbStatusBarWindowed.Checked;
			Global.Config.DispChrome_CaptionWindowed = cbCaptionWindowed.Checked;
			Global.Config.DispChrome_MenuWindowed = cbMenuWindowed.Checked;
			Global.Config.DispChrome_StatusBarFullscreen = cbStatusBarFullscreen.Checked;
			Global.Config.DispChrome_MenuFullscreen = cbMenuFullscreen.Checked;
			Global.Config.DispChrome_FrameWindowed = trackbarFrameSizeWindowed.Value;
			Global.Config.DispChrome_Fullscreen_AutohideMouse = cbFSAutohideMouse.Checked;
			Global.Config.DispChrome_AllowDoubleClickFullscreen = cbAllowDoubleclickFullscreen.Checked;

			if (rbDisplayFull.Checked) Global.Config.DispSpeedupFeatures = 2;
			if (rbDisplayMinimal.Checked) Global.Config.DispSpeedupFeatures = 1;
			if (rbDisplayAbsoluteZero.Checked) Global.Config.DispSpeedupFeatures = 0;

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
			else if (rbUseCustomRatio.Checked)
				Global.Config.DispManagerAR = Config.EDispManagerAR.CustomRatio;

			if (txtCustomARWidth.Text != "")
				int.TryParse(txtCustomARWidth.Text, out Global.Config.DispCustomUserARWidth);
			else Global.Config.DispCustomUserARWidth = -1;
			if (txtCustomARHeight.Text != "")
				int.TryParse(txtCustomARHeight.Text, out Global.Config.DispCustomUserARHeight);
			else Global.Config.DispCustomUserARHeight = -1;
			if (txtCustomARX.Text != "")
				float.TryParse(txtCustomARX.Text, out Global.Config.DispCustomUserARX);
			else Global.Config.DispCustomUserARX = -1;
			if (txtCustomARY.Text != "")
				float.TryParse(txtCustomARY.Text, out Global.Config.DispCustomUserARY);
			else Global.Config.DispCustomUserARY = -1;

			var oldDisplayMethod = Global.Config.DispMethod;
			if(rbOpenGL.Checked)
				Global.Config.DispMethod = Config.EDispMethod.OpenGL;
			if(rbGDIPlus.Checked)
				Global.Config.DispMethod = Config.EDispMethod.GdiPlus;
			if(rbD3D9.Checked)
				Global.Config.DispMethod = Config.EDispMethod.SlimDX9;

			if (oldDisplayMethod != Global.Config.DispMethod)
				NeedReset = true;

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
				var choice = Path.GetFullPath(ofd.FileName);
				
				//test the preset
				using (var stream = File.OpenRead(choice))
				{
					var cgp = new BizHawk.Client.EmuHawk.Filters.RetroShaderPreset(stream);
					if (cgp.ContainsGLSL)
					{
						MessageBox.Show("Specified CGP contains references to .glsl files. This is illegal. Use .cg");
						return;
					}

					//try compiling it
					bool ok = false;
					string errors = "";
					try 
					{
						var filter = new BizHawk.Client.EmuHawk.Filters.RetroShaderChain(GlobalWin.GL, cgp, Path.GetDirectoryName(choice));
						ok = filter.Available;
						errors = filter.Errors;
					}
					catch {}
					if (!ok)
					{
						new ExceptionBox(errors).ShowDialog();
						return;
					}
				}

				PathSelection = choice;
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
			float percentage = (float) scanlines / 256 * 100;
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

		private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			System.Diagnostics.Process.Start("http://tasvideos.org/Bizhawk/DisplayConfig.html");
		}

		private void label13_Click(object sender, EventArgs e)
		{
			cbAlternateVsync.Checked ^= true;
		}

		private void btnDefaults_Click(object sender, EventArgs e)
		{
			nudPrescale.Value = 1;
			rbNone.Checked = true;
			cbAutoPrescale.Checked = true;
			rbFinalFilterBilinear.Checked = true;
			checkLetterbox.Checked = true;
			rbUseSystem.Checked = true;
		}

	}
}
