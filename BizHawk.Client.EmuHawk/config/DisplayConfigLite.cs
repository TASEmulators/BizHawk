using System;
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
		public DisplayConfigLite()
		{
			InitializeComponent();

			rbNone.Checked = Global.Config.TargetDisplayFilter == 0;
			rbHq2x.Checked  = Global.Config.TargetDisplayFilter == 1;
			rbScanlines.Checked = Global.Config.TargetDisplayFilter == 2;
			checkBilinearFilter.Checked = Global.Config.DispBlurry;
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			if(rbNone.Checked)
				Global.Config.TargetDisplayFilter = 0;
			if (rbHq2x.Checked)
				Global.Config.TargetDisplayFilter = 1;
			if (rbScanlines.Checked)
				Global.Config.TargetDisplayFilter = 2;

			Global.Config.DispBlurry = checkBilinearFilter.Checked;

			DialogResult = System.Windows.Forms.DialogResult.OK;
			Close();
		}
	}
}
