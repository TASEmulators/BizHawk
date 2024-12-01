using System.Windows.Forms;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Computers.SinclairSpectrum;

namespace BizHawk.Client.EmuHawk
{
	public partial class ZxSpectrumCoreEmulationSettings : Form
	{
		private readonly ISettingsAdapter _settable;

		private readonly ZXSpectrum.ZXSpectrumSyncSettings _syncSettings;

		public ZxSpectrumCoreEmulationSettings(ISettingsAdapter settable)
		{
			_settable = settable;
			_syncSettings = (ZXSpectrum.ZXSpectrumSyncSettings) _settable.GetSyncSettings();
			InitializeComponent();
			Icon = Properties.Resources.GameControllerIcon;
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
			var borderTypes = Enum.GetNames(typeof(ZXSpectrum.BorderType));
			foreach (var val in borderTypes)
			{
				borderTypecomboBox1.Items.Add(val);
			}
			borderTypecomboBox1.SelectedItem = _syncSettings.BorderType.ToString();
			UpdateBorderNotes((ZXSpectrum.BorderType)Enum.Parse(typeof(ZXSpectrum.BorderType), borderTypecomboBox1.SelectedItem.ToString()));

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
				_syncSettings.MachineType = (MachineType)Enum.Parse(typeof(MachineType), MachineSelectionComboBox.SelectedItem.ToString());
				_syncSettings.BorderType = (ZXSpectrum.BorderType)Enum.Parse(typeof(ZXSpectrum.BorderType), borderTypecomboBox1.SelectedItem.ToString());
				_syncSettings.DeterministicEmulation = determEmucheckBox1.Checked;
				_syncSettings.AutoLoadTape = autoLoadcheckBox1.Checked;

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

		private void MachineSelectionComboBox_SelectionChangeCommitted(object sender, EventArgs e)
		{
			var cb = (ComboBox)sender;
			UpdateMachineNotes((MachineType)Enum.Parse(typeof(MachineType), cb.SelectedItem.ToString()));
		}

		private void UpdateMachineNotes(MachineType type)
		{
			textBoxCoreDetails.Text = ZXMachineMetaData.GetMetaString(type);
		}

		private void BorderTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			var cb = (ComboBox)sender;
			UpdateBorderNotes((ZXSpectrum.BorderType)Enum.Parse(typeof(ZXSpectrum.BorderType), cb.SelectedItem.ToString()));
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
