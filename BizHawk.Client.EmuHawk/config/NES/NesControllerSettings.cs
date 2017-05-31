using System;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Cores.Nintendo.NES;

namespace BizHawk.Client.EmuHawk
{
	public partial class NesControllerSettings : Form
	{
		private readonly NES.NESSyncSettings _syncSettings;

		public NesControllerSettings()
		{
			InitializeComponent();
			_syncSettings = ((NES)Global.Emulator).GetSyncSettings();

			// TODO: use combobox extension and add descriptions to enum values
			comboBoxFamicom.Items.AddRange(NESControlSettings.GetFamicomExpansionValues().ToArray());
			comboBoxNESL.Items.AddRange(NESControlSettings.GetNesPortValues().ToArray());
			comboBoxNESR.Items.AddRange(NESControlSettings.GetNesPortValues().ToArray());

			comboBoxFamicom.SelectedItem = _syncSettings.Controls.FamicomExpPort;
			comboBoxNESL.SelectedItem = _syncSettings.Controls.NesLeftPort;
			comboBoxNESR.SelectedItem = _syncSettings.Controls.NesRightPort;
			checkBoxFamicom.Checked = _syncSettings.Controls.Famicom;
		}

		private void CheckBoxFamicom_CheckedChanged(object sender, EventArgs e)
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

			bool changed = NESControlSettings.NeedsReboot(ctrls, _syncSettings.Controls);

			_syncSettings.Controls = ctrls;

			if (changed)
			{
				GlobalWin.MainForm.PutCoreSyncSettings(_syncSettings);
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
