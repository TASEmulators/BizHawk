using System;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Cores.Computers.AmstradCPC;
using System.Text;
using static BizHawk.Emulation.Cores.Computers.AmstradCPC.AmstradCPC;

namespace BizHawk.Client.EmuHawk
{
	public partial class AmstradCPCCoreEmulationSettings : Form
	{
		private AmstradCPC.AmstradCPCSyncSettings _syncSettings;

		public AmstradCPCCoreEmulationSettings()
		{
			InitializeComponent();
		}

		private void IntvControllerSettings_Load(object sender, EventArgs e)
		{
			_syncSettings = ((AmstradCPC)Global.Emulator).GetSyncSettings().Clone();

            // machine selection
            var machineTypes = Enum.GetNames(typeof(MachineType));
			foreach (var val in machineTypes)
			{
				MachineSelectionComboBox.Items.Add(val);
            }
            MachineSelectionComboBox.SelectedItem = _syncSettings.MachineType.ToString();
            UpdateMachineNotes((MachineType)Enum.Parse(typeof(MachineType), MachineSelectionComboBox.SelectedItem.ToString()));

            // border selecton
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

                GlobalWin.MainForm.PutCoreSyncSettings(_syncSettings);

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
			GlobalWin.OSD.AddMessage("Core emulator settings aborted");
			DialogResult = DialogResult.Cancel;
			Close();
		}

        private void MachineSelectionComboBox_SelectionChangeCommitted(object sender, EventArgs e)
        {
            ComboBox cb = sender as ComboBox;
            UpdateMachineNotes((MachineType)Enum.Parse(typeof(MachineType), cb.SelectedItem.ToString()));
        }

        private void UpdateMachineNotes(MachineType type)
        {
            textBoxMachineNotes.Text = CPCMachineMetaData.GetMetaString(type);
        }

        private void borderTypecomboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox cb = sender as ComboBox;
            UpdateBorderNotes((AmstradCPC.BorderType)Enum.Parse(typeof(AmstradCPC.BorderType), cb.SelectedItem.ToString()));
        }

        private void UpdateBorderNotes(AmstradCPC.BorderType type)
        {
            switch (type)
            {
                case AmstradCPC.BorderType.Uniform:
                    lblBorderInfo.Text = "Attempts to equalise the border areas";
                    break;
                case AmstradCPC.BorderType.Uncropped:
                    lblBorderInfo.Text = "Pretty much the signal the gate array is generating (looks pants)";
                    break;

                case AmstradCPC.BorderType.Widescreen:
                    lblBorderInfo.Text = "Top and bottom border removed so that the result is *almost* 16:9";
                    break;
            }
        }
    }
}
