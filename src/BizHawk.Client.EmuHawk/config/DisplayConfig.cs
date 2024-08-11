using System.Globalization;
using System.IO;
using System.Windows.Forms;

using BizHawk.Bizware.Graphics;
using BizHawk.Client.Common;
using BizHawk.Client.Common.Filters;
using BizHawk.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class DisplayConfig : Form, IDialogParent
	{
		private static readonly FilesystemFilterSet CgShaderPresetsFSFilterSet = new(new FilesystemFilter(".CGP Files", new[] { "cgp" }))
		{
			AppendAllFilesEntry = false,
		};

		private readonly Config _config;

		private readonly IGL _gl;

		private string _pathSelection;

		public IDialogController DialogController { get; }

		public bool NeedReset { get; set; }

		public DisplayConfig(Config config, IDialogController dialogController, IGL gl)
		{
			_config = config;
			_gl = gl;
			DialogController = dialogController;

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

			cbAllowTearing.Checked = _config.DispAllowTearing;

			if (_config.DispSpeedupFeatures == 2) rbDisplayFull.Checked = true;
			if (_config.DispSpeedupFeatures == 1) rbDisplayMinimal.Checked = true;
			if (_config.DispSpeedupFeatures == 0) rbDisplayAbsoluteZero.Checked = true;

			cbStaticWindowTitles.Checked = _config.UseStaticWindowTitles;

			rbOpenGL.Checked = _config.DispMethod == EDispMethod.OpenGL;
			rbGDIPlus.Checked = _config.DispMethod == EDispMethod.GdiPlus;
			rbD3D11.Checked = _config.DispMethod == EDispMethod.D3D11;

			cbStatusBarWindowed.Checked = _config.DispChromeStatusBarWindowed;
			cbCaptionWindowed.Checked = _config.DispChromeCaptionWindowed;
			cbMenuWindowed.Checked = _config.DispChromeMenuWindowed;
			cbMainFormSaveWindowPosition.Checked = _config.SaveWindowPosition;
			cbMainFormStayOnTop.Checked = _config.MainFormStayOnTop;
			if (OSTailoredCode.IsUnixHost)
			{
				cbMainFormStayOnTop.Enabled = false;
				cbMainFormStayOnTop.Visible = false;
			}
			cbStatusBarFullscreen.Checked = _config.DispChromeStatusBarFullscreen;
			cbMenuFullscreen.Checked = _config.DispChromeMenuFullscreen;
			trackbarFrameSizeWindowed.Value = _config.DispChromeFrameWindowed;
			cbFSAutohideMouse.Checked = _config.DispChromeFullscreenAutohideMouse;
			SyncTrackBar();

			cbAllowDoubleclickFullscreen.Checked = _config.DispChromeAllowDoubleClickFullscreen;

			nudPrescale.Value = _config.DispPrescale;

			if (_config.DispManagerAR == EDispManagerAR.None)
				rbUseRaw.Checked = true;
			else if (_config.DispManagerAR == EDispManagerAR.System)
				rbUseSystem.Checked = true;
			else if (_config.DispManagerAR == EDispManagerAR.CustomSize)
				rbUseCustom.Checked = true;
			else if (_config.DispManagerAR == EDispManagerAR.CustomRatio)
				rbUseCustomRatio.Checked = true;

			if(_config.DispCustomUserARWidth != -1)
				txtCustomARWidth.Text = _config.DispCustomUserARWidth.ToString();
			if (_config.DispCustomUserARHeight != -1)
				txtCustomARHeight.Text = _config.DispCustomUserARHeight.ToString();
			if (_config.DispCustomUserArx != -1)
				txtCustomARX.Text = _config.DispCustomUserArx.ToString(NumberFormatInfo.InvariantInfo);
			if (_config.DispCustomUserAry != -1)
				txtCustomARY.Text = _config.DispCustomUserAry.ToString(NumberFormatInfo.InvariantInfo);

			txtCropLeft.Text = _config.DispCropLeft.ToString();
			txtCropTop.Text = _config.DispCropTop.ToString();
			txtCropRight.Text = _config.DispCropRight.ToString();
			txtCropBottom.Text = _config.DispCropBottom.ToString();

			RefreshAspectRatioOptions();

			if (!HostCapabilityDetector.HasD3D11)
			{
				rbD3D11.Enabled = false;
				rbD3D11.AutoCheck = false;
				cbAllowTearing.Enabled = false;
				label13.Enabled = false;
				label8.Enabled = false;
			}
		}

		private void BtnOk_Click(object sender, EventArgs e)
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
			
			_config.DispAllowTearing = cbAllowTearing.Checked;

			_config.DispChromeStatusBarWindowed = cbStatusBarWindowed.Checked;
			_config.DispChromeCaptionWindowed = cbCaptionWindowed.Checked;
			_config.DispChromeMenuWindowed = cbMenuWindowed.Checked;
			_config.SaveWindowPosition = cbMainFormSaveWindowPosition.Checked;
			_config.MainFormStayOnTop = cbMainFormStayOnTop.Checked;
			Owner.TopMost = _config.MainFormStayOnTop;
			_config.DispChromeStatusBarFullscreen = cbStatusBarFullscreen.Checked;
			_config.DispChromeMenuFullscreen = cbMenuFullscreen.Checked;
			_config.DispChromeFrameWindowed = trackbarFrameSizeWindowed.Value;
			_config.DispChromeFullscreenAutohideMouse = cbFSAutohideMouse.Checked;
			_config.DispChromeAllowDoubleClickFullscreen = cbAllowDoubleclickFullscreen.Checked;

			if (rbDisplayFull.Checked) _config.DispSpeedupFeatures = 2;
			if (rbDisplayMinimal.Checked) _config.DispSpeedupFeatures = 1;
			if (rbDisplayAbsoluteZero.Checked) _config.DispSpeedupFeatures = 0;

			_config.UseStaticWindowTitles = cbStaticWindowTitles.Checked;

			if (rbUseRaw.Checked)
				_config.DispManagerAR = EDispManagerAR.None;
			else if (rbUseSystem.Checked)
				_config.DispManagerAR = EDispManagerAR.System;
			else if (rbUseCustom.Checked)
				_config.DispManagerAR = EDispManagerAR.CustomSize;
			else if (rbUseCustomRatio.Checked)
				_config.DispManagerAR = EDispManagerAR.CustomRatio;

			if (!string.IsNullOrWhiteSpace(txtCustomARWidth.Text))
			{
				if (int.TryParse(txtCustomARWidth.Text, out int dispCustomUserARWidth))
				{
					_config.DispCustomUserARWidth = dispCustomUserARWidth;
				}
			}
			else
			{
				_config.DispCustomUserARWidth = -1;
			}

			if (!string.IsNullOrWhiteSpace(txtCustomARHeight.Text))
			{
				if (int.TryParse(txtCustomARHeight.Text, out int dispCustomUserARHeight))
				{
					_config.DispCustomUserARHeight = dispCustomUserARHeight;
				}
			}
			else
			{
				_config.DispCustomUserARHeight = -1;
			}

			if (!string.IsNullOrWhiteSpace(txtCustomARX.Text))
			{
				if (float.TryParse(txtCustomARX.Text, out float dispCustomUserArx))
				{
					_config.DispCustomUserArx = dispCustomUserArx;
				}
			}
			else
			{
				_config.DispCustomUserArx = -1;
			}

			if (!string.IsNullOrWhiteSpace(txtCustomARY.Text))
			{
				if (float.TryParse(txtCustomARY.Text, out float dispCustomUserAry))
				{
					_config.DispCustomUserAry = dispCustomUserAry;
				}
			}
			else
			{
				_config.DispCustomUserAry = -1;
			}

			var oldDisplayMethod = _config.DispMethod;
			if(rbOpenGL.Checked)
				_config.DispMethod = EDispMethod.OpenGL;
			if(rbGDIPlus.Checked)
				_config.DispMethod = EDispMethod.GdiPlus;
			if(rbD3D11.Checked)
				_config.DispMethod = EDispMethod.D3D11;

			if (int.TryParse(txtCropLeft.Text, out int dispCropLeft))
			{
				_config.DispCropLeft = dispCropLeft;
			}

			if (int.TryParse(txtCropTop.Text, out int dispCropTop))
			{
				_config.DispCropTop = dispCropTop;
			}

			if (int.TryParse(txtCropRight.Text, out int dispCropRight))
			{
				_config.DispCropRight = dispCropRight;
			}

			if (int.TryParse(txtCropBottom.Text, out int dispCropBottom))
			{
				_config.DispCropBottom = dispCropBottom;
			}

			if (oldDisplayMethod != _config.DispMethod)
			{
				NeedReset = true;
			}

			_config.DispUserFilterPath = _pathSelection;

			DialogResult = DialogResult.OK;
			Close();
		}

		private void RefreshState()
		{
			lblUserFilterName.Text = Path.GetFileNameWithoutExtension(_pathSelection);
		}

		private void BtnSelectUserFilter_Click(object sender, EventArgs e)
		{
			var result = this.ShowFileOpenDialog(
				filter: CgShaderPresetsFSFilterSet,
				initDir: string.IsNullOrWhiteSpace(_pathSelection)
				? string.Empty : Path.GetDirectoryName(_pathSelection)!,
				initFileName: _pathSelection);
			if (result is null) return;

			rbUser.Checked = true;
			var choice = Path.GetFullPath(result);
				
			//test the preset
			using (var stream = File.OpenRead(choice))
			{
				var cgp = new RetroShaderPreset(stream);

				// try compiling it
				bool ok = false;
				string errors = "";
				try
				{
					var filter = new RetroShaderChain(_gl, cgp, Path.GetDirectoryName(choice));
					ok = filter.Available;
					errors = filter.Errors;
				}
				catch {}
				if (!ok)
				{
					using var errorForm = new ExceptionBox(errors);
					this.ShowDialogAsChild(errorForm);
					return;
				}
			}

			_pathSelection = choice;
			RefreshState();
		}

		private void CheckLetterbox_CheckedChanged(object sender, EventArgs e)
		{
			RefreshAspectRatioOptions();
		}

		private void CheckPadInteger_CheckedChanged(object sender, EventArgs e)
		{
			RefreshAspectRatioOptions();
		}

		private void RbUseRaw_CheckedChanged(object sender, EventArgs e)
		{
			RefreshAspectRatioOptions();
		}

		private void RbUseSystem_CheckedChanged(object sender, EventArgs e)
		{
			RefreshAspectRatioOptions();
		}

		private void RefreshAspectRatioOptions()
		{
			grpARSelection.Enabled = checkLetterbox.Checked;
			checkPadInteger.Enabled = checkLetterbox.Checked;
		}

		public void TbScanlineIntensity_Scroll(object sender, EventArgs e)
		{
			_config.TargetScanlineFilterIntensity = tbScanlineIntensity.Value;
			lblScanlines.Text = $"{_config.TargetScanlineFilterIntensity / 256.0:P2}";
		}

		private void TrackBarFrameSizeWindowed_ValueChanged(object sender, EventArgs e)
		{
			SyncTrackBar();
		}

		private void SyncTrackBar()
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

		private void LinkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			System.Diagnostics.Process.Start("https://tasvideos.org/Bizhawk/DisplayConfig");
		}

		private void Label13_Click(object sender, EventArgs e)
			=> cbAllowTearing.Checked = !cbAllowTearing.Checked;

		private void BtnDefaults_Click(object sender, EventArgs e)
		{
			nudPrescale.Value = 1;
			rbNone.Checked = true;
			cbAutoPrescale.Checked = true;
			rbFinalFilterBilinear.Checked = true;
			checkLetterbox.Checked = true;
			rbUseSystem.Checked = true;
			txtCropLeft.Text = "0";
			txtCropTop.Text = "0";
			txtCropRight.Text = "0";
			txtCropBottom.Text = "0";
		}
	}
}
