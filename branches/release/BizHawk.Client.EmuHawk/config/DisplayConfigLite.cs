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
			rbHq2x.Checked  = Global.Config.TargetDisplayFilter == 1;
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
			rbUseSystem.Checked = Global.Config.DispObeyAR;
			rbUseRaw.Checked = !Global.Config.DispObeyAR;
			checkFullscreenHacks.Checked = Global.Config.DispFullscreenHacks;
			checkSnowyNullEmulator.Checked = Global.Config.DispSnowyNullEmulator;

			RefreshAspectRatioOptions();
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			if(rbNone.Checked)
				Global.Config.TargetDisplayFilter = 0;
			if (rbHq2x.Checked)
				Global.Config.TargetDisplayFilter = 1;
			if (rbScanlines.Checked)
				Global.Config.TargetDisplayFilter = 2;
			if (rbUser.Checked)
				Global.Config.TargetDisplayFilter = 3;

			if(rbFinalFilterNone.Checked) 
				Global.Config.DispFinalFilter = 0;
			if(rbFinalFilterBilinear.Checked) 
				Global.Config.DispFinalFilter = 1;
			if(rbFinalFilterBicubic.Checked) 
				Global.Config.DispFinalFilter = 2;

			Global.Config.TargetScanlineFilterIntensity = tbScanlineIntensity.Value;
			Global.Config.DispFixAspectRatio = checkLetterbox.Checked;
			Global.Config.DispFixScaleInteger = checkPadInteger.Checked;
			Global.Config.DispObeyAR = rbUseSystem.Checked;
			Global.Config.DispFullscreenHacks = checkFullscreenHacks.Checked;
			Global.Config.DispSnowyNullEmulator = checkSnowyNullEmulator.Checked;

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

	}
}
