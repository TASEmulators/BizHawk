using System;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Cores.Computers.SinclairSpectrum;
using System.Text;

namespace BizHawk.Client.EmuHawk
{
	public partial class ZXSpectrumCoreEmulationSettings : Form
	{
		private ZXSpectrum.ZXSpectrumSyncSettings _syncSettings;

		public ZXSpectrumCoreEmulationSettings()
		{
			InitializeComponent();
		}

		private void IntvControllerSettings_Load(object sender, EventArgs e)
		{
			_syncSettings = ((ZXSpectrum)Global.Emulator).GetSyncSettings().Clone();

            // machine selection
            var machineTypes = Enum.GetNames(typeof(MachineType));
			foreach (var val in machineTypes)
			{
				MachineSelectionComboBox.Items.Add(val);
            }
            MachineSelectionComboBox.SelectedItem = _syncSettings.MachineType.ToString();
            UpdateMachineNotes((MachineType)Enum.Parse(typeof(MachineType), MachineSelectionComboBox.SelectedItem.ToString()));

            // border selecton
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
            textBoxCoreDetails.Text = ZXMachineMetaData.GetMetaString(type);
        }

        private void borderTypecomboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox cb = sender as ComboBox;
            UpdateBorderNotes((ZXSpectrum.BorderType)Enum.Parse(typeof(ZXSpectrum.BorderType), cb.SelectedItem.ToString()));
        }

        private void UpdateBorderNotes(ZXSpectrum.BorderType type)
        {
            switch (type)
            {
                case ZXSpectrum.BorderType.Full:
                    lblBorderInfo.Text = "Original border sizes";
                    break;
                case ZXSpectrum.BorderType.Medium:
                    lblBorderInfo.Text = "All borders 24px";
                    break;
                case ZXSpectrum.BorderType.None:
                    lblBorderInfo.Text = "No border at all";
                    break;
                case ZXSpectrum.BorderType.Small:
                    lblBorderInfo.Text = "All borders 10px";
                    break;
                case ZXSpectrum.BorderType.Widescreen:
                    lblBorderInfo.Text = "No top and bottom border (almost 16:9)";
                    break;
            }
        }
    }
}
