using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Cores.Nintendo.NES;

namespace BizHawk.Client.EmuHawk
{
	public partial class NesControllerSettings : Form
	{
		NES.NESSyncSettings SyncSettings;

		public NesControllerSettings()
		{
			InitializeComponent();
			SyncSettings = ((NES)Global.Emulator).GetSyncSettings();

			// TODO: use combobox extension and add descriptions to enum values
			comboBoxFamicom.Items.AddRange(NESControlSettings.GetFamicomExpansionValues().ToArray());
			comboBoxNESL.Items.AddRange(NESControlSettings.GetNesPortValues().ToArray());
			comboBoxNESR.Items.AddRange(NESControlSettings.GetNesPortValues().ToArray());

			comboBoxFamicom.SelectedItem = SyncSettings.Controls.FamicomExpPort;
			comboBoxNESL.SelectedItem = SyncSettings.Controls.NesLeftPort;
			comboBoxNESR.SelectedItem = SyncSettings.Controls.NesRightPort;
			checkBoxFamicom.Checked = SyncSettings.Controls.Famicom;
		}

		private void NesControllerSettings_Load(object sender, EventArgs e)
		{

		}

		private void checkBoxFamicom_CheckedChanged(object sender, EventArgs e)
		{
			comboBoxFamicom.Enabled = checkBoxFamicom.Checked;
			comboBoxNESL.Enabled = !checkBoxFamicom.Checked;
			comboBoxNESR.Enabled = !checkBoxFamicom.Checked;
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			var ctrls = new NESControlSettings
			{
				Famicom = checkBoxFamicom.Checked,
				FamicomExpPort = (string)comboBoxFamicom.SelectedItem,
				NesLeftPort = (string)comboBoxNESL.SelectedItem,
				NesRightPort = (string)comboBoxNESR.SelectedItem
			};

			bool changed = NESControlSettings.NeedsReboot(ctrls, SyncSettings.Controls);

			SyncSettings.Controls = ctrls;

			if (changed)
			{
				GlobalWin.MainForm.PutCoreSyncSettings(SyncSettings);
				// redundant -- MainForm.PutCoreSyncSettings() flags reboot when it is needed
				// GlobalWin.MainForm.FlagNeedsReboot();
				// GlobalWin.OSD.AddMessage("Controller settings saved but a core reboot is required");
			}

			DialogResult = DialogResult.OK;
			Close();
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			GlobalWin.OSD.AddMessage("Controller settings aborted");
			DialogResult = DialogResult.Cancel;
			Close();
		}
	}
}
