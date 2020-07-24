using System;
using System.Windows.Forms;

using BizHawk.Emulation.Cores.Computers.SinclairSpectrum;

using EnumsNET;

namespace BizHawk.Client.EmuHawk
{
	public partial class ZxSpectrumCoreEmulationSettings : Form
	{
		private readonly IMainFormForConfig _mainForm;
		private readonly ZXSpectrum.ZXSpectrumSyncSettings _syncSettings;

		public ZxSpectrumCoreEmulationSettings(
			IMainFormForConfig mainForm,
			ZXSpectrum.ZXSpectrumSyncSettings syncSettings)
		{
			_mainForm = mainForm;
			_syncSettings = syncSettings;
			InitializeComponent();
			Icon = Properties.Resources.GameControllerIcon;
		}

		private void IntvControllerSettings_Load(object sender, EventArgs e)
		{
			// machine selection
			var machineTypes = Enums.GetNames<MachineType>();
			foreach (var val in machineTypes)
			{
				MachineSelectionComboBox.Items.Add(val);
			}
			MachineSelectionComboBox.SelectedItem = _syncSettings.MachineType.ToString();
			UpdateMachineNotes(Enums.Parse<MachineType>(MachineSelectionComboBox.SelectedItem.ToString()));

			// border selection
			var borderTypes = Enums.GetNames<ZXSpectrum.BorderType>();
			foreach (var val in borderTypes)
			{
				borderTypecomboBox1.Items.Add(val);
			}
			borderTypecomboBox1.SelectedItem = _syncSettings.BorderType.ToString();
			UpdateBorderNotes(Enums.Parse<ZXSpectrum.BorderType>(borderTypecomboBox1.SelectedItem.ToString()));

			// deterministic emulation
			determEmucheckBox1.Checked = _syncSettings.DeterministicEmulation;

			// autoload tape
			autoLoadcheckBox1.Checked = _syncSettings.AutoLoadTape;
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			bool changed =
				_syncSettings.MachineType.ToString() != MachineSelectionComboBox.SelectedItem.ToString()
				|| _syncSettings.BorderType.ToString() != borderTypecomboBox1.SelectedItem.ToString()
				|| _syncSettings.DeterministicEmulation != determEmucheckBox1.Checked
				|| _syncSettings.AutoLoadTape != autoLoadcheckBox1.Checked;

			if (changed)
			{
				_syncSettings.MachineType = Enums.Parse<MachineType>(MachineSelectionComboBox.SelectedItem.ToString());
				_syncSettings.BorderType = Enums.Parse<ZXSpectrum.BorderType>(borderTypecomboBox1.SelectedItem.ToString());
				_syncSettings.DeterministicEmulation = determEmucheckBox1.Checked;
				_syncSettings.AutoLoadTape = autoLoadcheckBox1.Checked;

				_mainForm.PutCoreSyncSettings(_syncSettings);
			}
			DialogResult = DialogResult.OK;
			Close();
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			_mainForm.AddOnScreenMessage("Core emulator settings aborted");
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void MachineSelectionComboBox_SelectionChangeCommitted(object sender, EventArgs e)
		{
			var cb = (ComboBox)sender;
			UpdateMachineNotes(Enums.Parse<MachineType>(cb.SelectedItem.ToString()));
		}

		private void UpdateMachineNotes(MachineType type)
		{
			textBoxCoreDetails.Text = ZXMachineMetaData.GetMetaString(type);
		}

		private void BorderTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			var cb = (ComboBox)sender;
			UpdateBorderNotes(Enums.Parse<ZXSpectrum.BorderType>(cb.SelectedItem.ToString()));
		}

		private void UpdateBorderNotes(ZXSpectrum.BorderType type)
		{
			lblBorderInfo.Text = type switch
			{
				ZXSpectrum.BorderType.Full => "Original border sizes",
				ZXSpectrum.BorderType.Medium => "All borders 24px",
				ZXSpectrum.BorderType.None => "No border at all",
				ZXSpectrum.BorderType.Small => "All borders 10px",
				ZXSpectrum.BorderType.Widescreen => "No top and bottom border (almost 16:9)",
				_ => lblBorderInfo.Text
			};
		}
	}
}
