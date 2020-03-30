using System;
using System.IO;
using System.Windows.Forms;
using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.Filters;
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

			rbFilterNone.Checked = _config.TargetDisplayFilter == 0;
			rbFilterHq2x.Checked = _config.TargetDisplayFilter == 1;
			rbFilterScanline.Checked = _config.TargetDisplayFilter == 2;
			rbFilterUser.Checked = _config.TargetDisplayFilter == 3;

			_pathSelection = _config.DispUserFilterPath ?? "";
			RefreshState();

			rbFinalFilterNone.Checked = _config.DispFinalFilter == 0;
			rbFinalFilterBilinear.Checked = _config.DispFinalFilter == 1;
			rbFinalFilterBicubic.Checked = _config.DispFinalFilter == 2;

			tbFilterScanlineAlpha.Value = _config.TargetScanlineFilterIntensity;
			cbLetterbox.Checked = _config.DispFixAspectRatio;
			cbScaleByInteger.Checked = _config.DispFixScaleInteger;
			cbFSWinHacks.Checked = _config.DispFullscreenHacks;
			cbAutoPrescale.Checked = _config.DispAutoPrescale;

			cbD3DAltVSync.Checked = _config.DispAlternateVsync;

			if (_config.DispSpeedupFeatures == 2) rbDispFeaturesFull.Checked = true;
			if (_config.DispSpeedupFeatures == 1) rbDispFeaturesMinimal.Checked = true;
			if (_config.DispSpeedupFeatures == 0) rbDispFeaturesNothing.Checked = true;

			rbDispMethodOpenGL.Checked = _config.DispMethod == EDispMethod.OpenGL;
			rbDispMethodGDIPlus.Checked = _config.DispMethod == EDispMethod.GdiPlus;
			rbDispMethodD3D.Checked = _config.DispMethod == EDispMethod.SlimDX9;

			cbWindowedStatusBar.Checked = _config.DispChromeStatusBarWindowed;
			cbWindowedCaption.Checked = _config.DispChromeCaptionWindowed;
			cbWindowedMenu.Checked = _config.DispChromeMenuWindowed;
			cbFSStatusBar.Checked = _config.DispChromeStatusBarFullscreen;
			cbFSMenu.Checked = _config.DispChromeMenuFullscreen;
			tbWindowedFrameType.Value = _config.DispChromeFrameWindowed;
			cbFSAutohideMouse.Checked = _config.DispChromeFullscreenAutohideMouse;
			SyncTrackBar();

			cbDoubleClickFS.Checked = _config.DispChromeAllowDoubleClickFullscreen;

			nudUserPrescale.Value = _config.DispPrescale;

			if (_config.DispManagerAR == EDispManagerAR.None)
				rbARSquare.Checked = true;
			else if (_config.DispManagerAR == EDispManagerAR.System)
				rbARBySystem.Checked = true;
			else if (_config.DispManagerAR == EDispManagerAR.Custom)
				rbARCustomSize.Checked = true;
			else if (_config.DispManagerAR == EDispManagerAR.CustomRatio)
				rbARCustomRatio.Checked = true;

			if(_config.DispCustomUserARWidth != -1)
				txtARCustomWidth.Text = _config.DispCustomUserARWidth.ToString();
			if (_config.DispCustomUserARHeight != -1)
				txtARCustomHeight.Text = _config.DispCustomUserARHeight.ToString();
			if (_config.DispCustomUserArx != -1)
				txtARCustomRatioH.Text = _config.DispCustomUserArx.ToString();
			if (_config.DispCustomUserAry != -1)
				txtARCustomRatioV.Text = _config.DispCustomUserAry.ToString();

			txtCropLeft.Text = _config.DispCropLeft.ToString();
			txtCropTop.Text = _config.DispCropTop.ToString();
			txtCropRight.Text = _config.DispCropRight.ToString();
			txtCropBottom.Text = _config.DispCropBottom.ToString();

			RefreshAspectRatioOptions();

			if (OSTailoredCode.IsUnixHost) flpD3DSection.Enabled = false; // Disable SlimDX on Unix
		}

		private void btnDialogOK_Click(object sender, EventArgs e)
		{
			if (rbFilterNone.Checked)
				_config.TargetDisplayFilter = 0;
			if (rbFilterHq2x.Checked)
				_config.TargetDisplayFilter = 1;
			if (rbFilterScanline.Checked)
				_config.TargetDisplayFilter = 2;
			if (rbFilterUser.Checked)
				_config.TargetDisplayFilter = 3;

			if (rbFinalFilterNone.Checked)
				_config.DispFinalFilter = 0;
			if (rbFinalFilterBilinear.Checked)
				_config.DispFinalFilter = 1;
			if (rbFinalFilterBicubic.Checked)
				_config.DispFinalFilter = 2;

			_config.DispPrescale = (int)nudUserPrescale.Value;

			_config.TargetScanlineFilterIntensity = tbFilterScanlineAlpha.Value;
			_config.DispFixAspectRatio = cbLetterbox.Checked;
			_config.DispFixScaleInteger = cbScaleByInteger.Checked;
			_config.DispFullscreenHacks = cbFSWinHacks.Checked;
			_config.DispAutoPrescale = cbAutoPrescale.Checked;
			
			_config.DispAlternateVsync = cbD3DAltVSync.Checked;

			_config.DispChromeStatusBarWindowed = cbWindowedStatusBar.Checked;
			_config.DispChromeCaptionWindowed = cbWindowedCaption.Checked;
			_config.DispChromeMenuWindowed = cbWindowedMenu.Checked;
			_config.DispChromeStatusBarFullscreen = cbFSStatusBar.Checked;
			_config.DispChromeMenuFullscreen = cbFSMenu.Checked;
			_config.DispChromeFrameWindowed = tbWindowedFrameType.Value;
			_config.DispChromeFullscreenAutohideMouse = cbFSAutohideMouse.Checked;
			_config.DispChromeAllowDoubleClickFullscreen = cbDoubleClickFS.Checked;

			if (rbDispFeaturesFull.Checked) _config.DispSpeedupFeatures = 2;
			if (rbDispFeaturesMinimal.Checked) _config.DispSpeedupFeatures = 1;
			if (rbDispFeaturesNothing.Checked) _config.DispSpeedupFeatures = 0;

			_config.DispManagerAR = grpAspectRatio.Tracker.GetSelectionTagAs<EDispManagerAR>() ?? throw new InvalidOperationException();

			if (string.IsNullOrWhiteSpace(txtARCustomWidth.Text))
			{
				if (int.TryParse(txtARCustomWidth.Text, out int dispCustomUserARWidth))
				{
					_config.DispCustomUserARWidth = dispCustomUserARWidth;
				}
			}
			else
			{
				_config.DispCustomUserARWidth = -1;
			}

			if (string.IsNullOrWhiteSpace(txtARCustomHeight.Text))
			{
				if (int.TryParse(txtARCustomHeight.Text, out int dispCustomUserARHeight))
				{
					_config.DispCustomUserARHeight = dispCustomUserARHeight;
				}
			}
			else
			{
				_config.DispCustomUserARHeight = -1;
			}

			if (string.IsNullOrWhiteSpace(txtARCustomRatioH.Text))
			{
				if (float.TryParse(txtARCustomRatioH.Text, out float dispCustomUserArx))
				{
					_config.DispCustomUserArx = dispCustomUserArx;
				}
			}
			else
			{
				_config.DispCustomUserArx = -1;
			}

			if (string.IsNullOrWhiteSpace(txtARCustomRatioV.Text))
			{
				if (float.TryParse(txtARCustomRatioV.Text, out float dispCustomUserAry))
				{
					_config.DispCustomUserAry = dispCustomUserAry;
				}
			}
			else
			{
				_config.DispCustomUserAry = -1;
			}

			var oldDisplayMethod = _config.DispMethod;
			_config.DispMethod = grpDispMethod.Tracker.GetSelectionTagAs<EDispMethod>() ?? throw new InvalidOperationException();

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
			lblFilterUser.Text = Path.GetFileNameWithoutExtension(_pathSelection);
		}

		private void btnFilterUser_Click(object sender, EventArgs e)
		{
			using var ofd = new OpenFileDialog
			{
				Filter = new FilesystemFilter(".CGP Files", new[] { "cgp" }).ToString(),
				FileName = _pathSelection
			};
			if (ofd.ShowDialog() == DialogResult.OK)
			{
				rbFilterUser.Checked = true;
				var choice = Path.GetFullPath(ofd.FileName);
				
				//test the preset
				using (var stream = File.OpenRead(choice))
				{
					var cgp = new RetroShaderPreset(stream);
					if (cgp.ContainsGlsl)
					{
						MessageBox.Show("Specified CGP contains references to .glsl files. This is illegal. Use .cg");
						return;
					}

					// try compiling it
					bool ok = false;
					string errors = "";
					try 
					{
						var filter = new RetroShaderChain(GlobalWin.GL, cgp, Path.GetDirectoryName(choice));
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

		private void cbLetterbox_CheckedChanged(object sender, EventArgs e)
		{
			RefreshAspectRatioOptions();
		}

		private void cbScaleByInteger_CheckedChanged(object sender, EventArgs e)
		{
			RefreshAspectRatioOptions();
		}

		private void rbARSquare_CheckedChanged(object sender, EventArgs e)
		{
			RefreshAspectRatioOptions();
		}

		private void rbARBySystem_CheckedChanged(object sender, EventArgs e)
		{
			RefreshAspectRatioOptions();
		}

		void RefreshAspectRatioOptions()
		{
			grpAspectRatio.Enabled = cbLetterbox.Checked;
			cbScaleByInteger.Enabled = cbLetterbox.Checked;
		}

		public void tbFilterScanlineAlpha_Scroll(object sender, EventArgs e)
		{
			_config.TargetScanlineFilterIntensity = tbFilterScanlineAlpha.Value;
			int scanlines = _config.TargetScanlineFilterIntensity;
			float percentage = (float) scanlines / 256 * 100;
			lblFilterScanlineAlpha.Text = $"{percentage:F2}%";
		}

		private void tbWidowedFrameType_ValueChanged(object sender, EventArgs e)
		{
			SyncTrackBar();
		}

		private void SyncTrackBar()
		{
			if (tbWindowedFrameType.Value == 0)
			{
				lblWindowedFrameTypeReadout.Text = "None";
			}

			if (tbWindowedFrameType.Value == 1)
			{
				lblWindowedFrameTypeReadout.Text = "Thin";
			}

			if (tbWindowedFrameType.Value == 2)
			{
				lblWindowedFrameTypeReadout.Text = "Thick";
			}
		}

		private void lnkDocs_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			System.Diagnostics.Process.Start("http://tasvideos.org/Bizhawk/DisplayConfig.html");
		}

		private void lblD3DAltVSync_Click(object sender, EventArgs e)
		{
			cbD3DAltVSync.Checked ^= true;
		}

		private void btnDefaults_Click(object sender, EventArgs e)
		{
			nudUserPrescale.Value = 1;
			rbFilterNone.Checked = true;
			cbAutoPrescale.Checked = true;
			rbFinalFilterBilinear.Checked = true;
			cbLetterbox.Checked = true;
			rbARBySystem.Checked = true;
		}
	}
}
