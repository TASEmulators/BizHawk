using System.Windows.Forms;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Computers.AmstradCPC;

namespace BizHawk.Client.EmuHawk
{
	public partial class AmstradCpcCoreEmulationSettings : Form
	{
		private readonly ISettingsAdapter _settable;

		private readonly AmstradCPC.AmstradCPCSyncSettings _syncSettings;

		public AmstradCpcCoreEmulationSettings(ISettingsAdapter settable)
		{
			_settable = settable;
			_syncSettings = (AmstradCPC.AmstradCPCSyncSettings) _settable.GetSyncSettings();
			InitializeComponent();
		}

		private void IntvControllerSettings_Load(object sender, EventArgs e)
		{
			// machine selection
			var machineTypes = Enum.GetNames(typeof(MachineType));
			foreach (var val in machineTypes)
			{
				MachineSelectionComboBox.Items.Add(val);
			}
			MachineSelectionComboBox.SelectedItem = _syncSettings.MachineType.ToString();
			UpdateMachineNotes((MachineType)Enum.Parse(typeof(MachineType), MachineSelectionComboBox.SelectedItem.ToString()));

			// border selection
			var borderTypes = Enum.GetNames(typeof(AmstradCPC.BorderType));
			foreach (var val in borderTypes)
			{
				borderTypecomboBox1.Items.Add(val);
			}
			borderTypecomboBox1.SelectedItem = _syncSettings.BorderType.ToString();
			UpdateBorderNotes((AmstradCPC.BorderType)Enum.Parse(typeof(AmstradCPC.BorderType), borderTypecomboBox1.SelectedItem.ToString()));

			// deterministic emulation
			determEmucheckBox1.Checked = _syncSettings.DeterministicEmulation;

			// autoload tape
			autoLoadcheckBox1.Checked = _syncSettings.AutoStartStopTape;
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			bool changed =
				_syncSettings.MachineType.ToString() != MachineSelectionComboBox.SelectedItem.ToString()
				|| _syncSettings.BorderType.ToString() != borderTypecomboBox1.SelectedItem.ToString()
				|| _syncSettings.DeterministicEmulation != determEmucheckBox1.Checked
				|| _syncSettings.AutoStartStopTape != autoLoadcheckBox1.Checked;

			if (changed)
			{
				_syncSettings.MachineType = (MachineType)Enum.Parse(typeof(MachineType), MachineSelectionComboBox.SelectedItem.ToString());
				_syncSettings.BorderType = (AmstradCPC.BorderType)Enum.Parse(typeof(AmstradCPC.BorderType), borderTypecomboBox1.SelectedItem.ToString());
				_syncSettings.DeterministicEmulation = determEmucheckBox1.Checked;
				_syncSettings.AutoStartStopTape = autoLoadcheckBox1.Checked;

				_settable.PutCoreSyncSettings(_syncSettings);

				DialogResult = DialogResult.OK;
				Close();
			}
			else
			{
				DialogResult = DialogResult.OK;
				Close();
			}
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void MachineSelectionComboBox_SelectionChangeCommitted(object sender, EventArgs e)
		{
			var cb = (ComboBox)sender;
			UpdateMachineNotes((MachineType)Enum.Parse(typeof(MachineType), cb.SelectedItem.ToString()));
		}

		private void UpdateMachineNotes(MachineType type)
		{
			textBoxMachineNotes.Text = AmstradCPC.CPCMachineMetaData.GetMetaString(type);
		}

		private void BorderTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			var cb = (ComboBox)sender;
			UpdateBorderNotes((AmstradCPC.BorderType)Enum.Parse(typeof(AmstradCPC.BorderType), cb.SelectedItem.ToString()));
		}

		private void UpdateBorderNotes(AmstradCPC.BorderType type)
		{
			lblBorderInfo.Text = type switch
			{
				AmstradCPC.BorderType.Visible => "Approximates what you see on a CPC monitor",
				AmstradCPC.BorderType.Uncropped => "The full display area",
				_ => lblBorderInfo.Text
			};
		}
	}
}
