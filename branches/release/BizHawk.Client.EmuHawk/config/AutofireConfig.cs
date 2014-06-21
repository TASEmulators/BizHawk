using System;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
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
			{
				OnNumeric.Value = OnNumeric.Minimum;
			}
			else if (Global.Config.AutofireOn > OnNumeric.Maximum)
			{
				OnNumeric.Value = OnNumeric.Maximum;
			}
			else
			{
				OnNumeric.Value = Global.Config.AutofireOn;
			}

			if (Global.Config.AutofireOff < OffNumeric.Minimum)
			{
				OffNumeric.Value = OffNumeric.Minimum;
			}
			else if (Global.Config.AutofireOff > OffNumeric.Maximum)
			{
				OffNumeric.Value = OffNumeric.Maximum;
			}
			else
			{
				OffNumeric.Value = Global.Config.AutofireOff;
			}

			LagFrameCheck.Checked = Global.Config.AutofireLagFrames;
		}

		private void Ok_Click(object sender, EventArgs e)
		{
			Global.AutoFireController.On = Global.Config.AutofireOn = (int)OnNumeric.Value;
			Global.AutoFireController.Off = Global.Config.AutofireOff = (int)OffNumeric.Value;
			Global.Config.AutofireLagFrames = LagFrameCheck.Checked;
			Global.AutofireStickyXORAdapter.SetOnOffPatternFromConfig();

			GlobalWin.OSD.AddMessage("Autofire settings saved");
			Close();
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			GlobalWin.OSD.AddMessage("Autofire config aborted");
			Close();
		}
	}
}
