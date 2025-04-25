using System.Windows.Forms;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.NES;

namespace BizHawk.Client.EmuHawk
{
	public partial class NesControllerSettings : Form
	{
		private readonly ISettingsAdapter _settable;

		private readonly NES.NESSyncSettings _syncSettings;

		public NesControllerSettings(ISettingsAdapter settable)
		{
			_settable = settable;
			_syncSettings = (NES.NESSyncSettings) _settable.GetSyncSettings();
			InitializeComponent();
			Icon = Properties.Resources.GameControllerIcon;

			// TODO: use combobox extension and add descriptions to enum values
			comboBoxFamicom.ReplaceItems(items: NESControlSettings.GetFamicomExpansionValues());
			comboBoxNESL.ReplaceItems(items: NESControlSettings.GetNesPortValues());
			comboBoxNESR.ReplaceItems(items: NESControlSettings.GetNesPortValues());

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
			var controls = new NESControlSettings
			{
				Famicom = checkBoxFamicom.Checked,
				FamicomExpPort = (string)comboBoxFamicom.SelectedItem,
				NesLeftPort = (string)comboBoxNESL.SelectedItem,
				NesRightPort = (string)comboBoxNESR.SelectedItem,
			};

			bool changed = NESControlSettings.NeedsReboot(controls, _syncSettings.Controls);

			_syncSettings.Controls = controls;

			if (changed)
			{
				_settable.PutCoreSyncSettings(_syncSettings);
			}

			DialogResult = DialogResult.OK;
			Close();
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}
	}
}
