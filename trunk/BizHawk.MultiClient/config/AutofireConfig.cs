using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	public partial class AutofireConfig : Form
	{
		public AutofireConfig()
		{
			InitializeComponent();
		}

		private void AutofireConfig_Load(object sender, EventArgs e)
		{
			if (Global.Config.AutofireOn < OnNumeric.Minimum)
				OnNumeric.Value = OnNumeric.Minimum;
			else if (Global.Config.AutofireOn > OnNumeric.Maximum)
				OnNumeric.Value = OnNumeric.Maximum;
			else
				OnNumeric.Value = Global.Config.AutofireOn;

			if (Global.Config.AutofireOff < OffNumeric.Minimum)
				OffNumeric.Value = OffNumeric.Minimum;
			else if (Global.Config.AutofireOff > OffNumeric.Maximum)
				OffNumeric.Value = OffNumeric.Maximum;
			else
				OffNumeric.Value = Global.Config.AutofireOff;

			 LagFrameCheck.Checked = Global.Config.AutofireLagFrames;
		}

		private void Ok_Click(object sender, EventArgs e)
		{
			Global.AutoFireController.On = Global.Config.AutofireOn = (int)OnNumeric.Value;
			Global.AutoFireController.Off = Global.Config.AutofireOff = (int)OffNumeric.Value;
			Global.Config.AutofireLagFrames = LagFrameCheck.Checked;
			Global.AutofireStickyXORAdapter.SetOnOffPatternFromConfig();

			Global.OSD.AddMessage("Autofire settings saved");
			this.Close();
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			this.Close();
		}
	}
}
