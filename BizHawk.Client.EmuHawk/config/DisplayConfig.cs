using System;
using System.IO;
using System.Windows.Forms;
using BizHawk.Client.Common;
using BizHawk.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class DisplayConfig : Form
	{
		private readonly Config _config;

		private string _pathSelection;

		public bool NeedReset { get; set; }

		public DisplayConfig(Config config)
		{
			_config = config;
			InitializeComponent();

			rbNone.Checked = _config.TargetDisplayFilter == 0;
			rbHq2x.Checked = _config.TargetDisplayFilter == 1;
			rbScanlines.Checked = _config.TargetDisplayFilter == 2;
			rbUser.Checked = _config.TargetDisplayFilter == 3;

			_pathSelection = _config.DispUserFilterPath ?? "";
			RefreshState();

			rbFinalFilterNone.Checked = _config.DispFinalFilter == 0;
			rbFinalFilterBilinear.Checked = _config.DispFinalFilter == 1;
			rbFinalFilterBicubic.Checked = _config.DispFinalFilter == 2;

			tbScanlineIntensity.Value = _config.TargetScanlineFilterIntensity;
			checkLetterbox.Checked = _config.DispFixAspectRatio;
			checkPadInteger.Checked = _config.DispFixScaleInteger;
			cbFullscreenHacks.Checked = _config.DispFullscreenHacks;
			cbAutoPrescale.Checked = _config.DispAutoPrescale;

			cbAlternateVsync.Checked = _config.DispAlternateVsync;

			if (_config.DispSpeedupFeatures == 2) rbDisplayFull.Checked = true;
			if (_config.DispSpeedupFeatures == 1) rbDisplayMinimal.Checked = true;
			if (_config.DispSpeedupFeatures == 0) rbDisplayAbsoluteZero.Checked = true;

			rbOpenGL.Checked = _config.DispMethod == EDispMethod.OpenGL;
			rbGDIPlus.Checked = _config.DispMethod == EDispMethod.GdiPlus;
			rbD3D9.Checked = _config.DispMethod == EDispMethod.SlimDX9;

			cbStatusBarWindowed.Checked = _config.DispChrome_StatusBarWindowed;
			cbCaptionWindowed.Checked = _config.DispChrome_CaptionWindowed;
			cbMenuWindowed.Checked = _config.DispChrome_MenuWindowed;
			cbStatusBarFullscreen.Checked = _config.DispChrome_StatusBarFullscreen;
			cbMenuFullscreen.Checked = _config.DispChrome_MenuFullscreen;
			trackbarFrameSizeWindowed.Value = _config.DispChrome_FrameWindowed;
			cbFSAutohideMouse.Checked = _config.DispChrome_Fullscreen_AutohideMouse;
			SyncTrackBar();

			cbAllowDoubleclickFullscreen.Checked = _config.DispChrome_AllowDoubleClickFullscreen;

			nudPrescale.Value = _config.DispPrescale;

			if (_config.DispManagerAR == EDispManagerAR.None)
				rbUseRaw.Checked = true;
			else if (_config.DispManagerAR == EDispManagerAR.System)
				rbUseSystem.Checked = true;
			else if (_config.DispManagerAR == EDispManagerAR.Custom)
				rbUseCustom.Checked = true;
			else if (_config.DispManagerAR == EDispManagerAR.CustomRatio)
				rbUseCustomRatio.Checked = true;

			if(_config.DispCustomUserARWidth != -1)
				txtCustomARWidth.Text = _config.DispCustomUserARWidth.ToString();
			if (_config.DispCustomUserARHeight != -1)
				txtCustomARHeight.Text = _config.DispCustomUserARHeight.ToString();
			if (_config.DispCustomUserARX != -1)
				txtCustomARX.Text = _config.DispCustomUserARX.ToString();
			if (_config.DispCustomUserARY != -1)
				txtCustomARY.Text = _config.DispCustomUserARY.ToString();

			txtCropLeft.Text = _config.DispCropLeft.ToString();
			txtCropTop.Text = _config.DispCropTop.ToString();
			txtCropRight.Text = _config.DispCropRight.ToString();
			txtCropBottom.Text = _config.DispCropBottom.ToString();

			RefreshAspectRatioOptions();

			if (OSTailoredCode.IsUnixHost)
			{
				// Disable SlimDX on Unix
				rbD3D9.Enabled = false;
				rbD3D9.AutoCheck = false;
				cbAlternateVsync.Enabled = false;
				label13.Enabled = false;
				label8.Enabled = false;
			}
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			if (rbNone.Checked)
				_config.TargetDisplayFilter = 0;
			if (rbHq2x.Checked)
				_config.TargetDisplayFilter = 1;
			if (rbScanlines.Checked)
				_config.TargetDisplayFilter = 2;
			if (rbUser.Checked)
				_config.TargetDisplayFilter = 3;

			if (rbFinalFilterNone.Checked)
				_config.DispFinalFilter = 0;
			if (rbFinalFilterBilinear.Checked)
				_config.DispFinalFilter = 1;
			if (rbFinalFilterBicubic.Checked)
				_config.DispFinalFilter = 2;

			_config.DispPrescale = (int)nudPrescale.Value;

			_config.TargetScanlineFilterIntensity = tbScanlineIntensity.Value;
			_config.DispFixAspectRatio = checkLetterbox.Checked;
			_config.DispFixScaleInteger = checkPadInteger.Checked;
			_config.DispFullscreenHacks = cbFullscreenHacks.Checked;
			_config.DispAutoPrescale = cbAutoPrescale.Checked;
			
			_config.DispAlternateVsync = cbAlternateVsync.Checked;

			_config.DispChrome_StatusBarWindowed = cbStatusBarWindowed.Checked;
			_config.DispChrome_CaptionWindowed = cbCaptionWindowed.Checked;
			_config.DispChrome_MenuWindowed = cbMenuWindowed.Checked;
			_config.DispChrome_StatusBarFullscreen = cbStatusBarFullscreen.Checked;
			_config.DispChrome_MenuFullscreen = cbMenuFullscreen.Checked;
			_config.DispChrome_FrameWindowed = trackbarFrameSizeWindowed.Value;
			_config.DispChrome_Fullscreen_AutohideMouse = cbFSAutohideMouse.Checked;
			_config.DispChrome_AllowDoubleClickFullscreen = cbAllowDoubleclickFullscreen.Checked;

			if (rbDisplayFull.Checked) _config.DispSpeedupFeatures = 2;
			if (rbDisplayMinimal.Checked) _config.DispSpeedupFeatures = 1;
			if (rbDisplayAbsoluteZero.Checked) _config.DispSpeedupFeatures = 0;

			if (rbUseRaw.Checked)
				_config.DispManagerAR = EDispManagerAR.None;
			else if (rbUseSystem.Checked)
				_config.DispManagerAR = EDispManagerAR.System;
			else if (rbUseCustom.Checked)
				_config.DispManagerAR = EDispManagerAR.Custom;
			else if (rbUseCustomRatio.Checked)
				_config.DispManagerAR = EDispManagerAR.CustomRatio;

			if (txtCustomARWidth.Text != "")
				int.TryParse(txtCustomARWidth.Text, out _config.DispCustomUserARWidth);
			else _config.DispCustomUserARWidth = -1;
			if (txtCustomARHeight.Text != "")
				int.TryParse(txtCustomARHeight.Text, out _config.DispCustomUserARHeight);
			else _config.DispCustomUserARHeight = -1;
			if (txtCustomARX.Text != "")
				float.TryParse(txtCustomARX.Text, out _config.DispCustomUserARX);
			else _config.DispCustomUserARX = -1;
			if (txtCustomARY.Text != "")
				float.TryParse(txtCustomARY.Text, out _config.DispCustomUserARY);
			else _config.DispCustomUserARY = -1;

			var oldDisplayMethod = _config.DispMethod;
			if(rbOpenGL.Checked)
				_config.DispMethod = EDispMethod.OpenGL;
			if(rbGDIPlus.Checked)
				_config.DispMethod = EDispMethod.GdiPlus;
			if(rbD3D9.Checked)
				_config.DispMethod = EDispMethod.SlimDX9;

			int.TryParse(txtCropLeft.Text, out _config.DispCropLeft);
			int.TryParse(txtCropTop.Text, out _config.DispCropTop);
			int.TryParse(txtCropRight.Text, out _config.DispCropRight);
			int.TryParse(txtCropBottom.Text, out _config.DispCropBottom);

			if (oldDisplayMethod != _config.DispMethod)
				NeedReset = true;

			_config.DispUserFilterPath = _pathSelection;

			DialogResult = DialogResult.OK;
			Close();
		}

		void RefreshState()
		{
			lblUserFilterName.Text = Path.GetFileNameWithoutExtension(_pathSelection);
		}

		private void btnSelectUserFilter_Click(object sender, EventArgs e)
		{
			using var ofd = new OpenFileDialog
			{
				Filter = ".CGP (*.cgp)|*.cgp",
				FileName = _pathSelection
			};
			if (ofd.ShowDialog() == DialogResult.OK)
			{
				rbUser.Checked = true;
				var choice = Path.GetFullPath(ofd.FileName);
				
				//test the preset
				using (var stream = File.OpenRead(choice))
				{
					var cgp = new BizHawk.Client.EmuHawk.Filters.RetroShaderPreset(stream);
					if (cgp.ContainsGlsl)
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
						using var errorForm = new ExceptionBox(errors);
						errorForm.ShowDialog();
						return;
					}
				}

				_pathSelection = choice;
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
			_config.TargetScanlineFilterIntensity = tbScanlineIntensity.Value;
			int scanlines = _config.TargetScanlineFilterIntensity;
			float percentage = (float) scanlines / 256 * 100;
			lblScanlines.Text = $"{percentage:F2}%";
		}

		private void TrackBarFrameSizeWindowed_ValueChanged(object sender, EventArgs e)
		{
			SyncTrackBar();
		}

		void SyncTrackBar()
		{
			if (trackbarFrameSizeWindowed.Value == 0)
			{
				lblFrameTypeWindowed.Text = "None";
			}

			if (trackbarFrameSizeWindowed.Value == 1)
			{
				lblFrameTypeWindowed.Text = "Thin";
			}

			if (trackbarFrameSizeWindowed.Value == 2)
			{
				lblFrameTypeWindowed.Text = "Thick";
			}
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
