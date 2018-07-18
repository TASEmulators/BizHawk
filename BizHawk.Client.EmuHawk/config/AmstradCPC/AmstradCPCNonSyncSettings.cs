using System;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Cores.Computers.AmstradCPC;
using System.Text;

namespace BizHawk.Client.EmuHawk
{
	public partial class AmstradCPCNonSyncSettings : Form
	{
		private AmstradCPC.AmstradCPCSettings _settings;

		public AmstradCPCNonSyncSettings()
		{
			InitializeComponent();
		}

		private void IntvControllerSettings_Load(object sender, EventArgs e)
		{
			_settings = ((AmstradCPC)Global.Emulator).GetSettings().Clone();

            

            // OSD Message Verbosity
            var osdTypes = Enum.GetNames(typeof(AmstradCPC.OSDVerbosity));     
            foreach (var val in osdTypes)
            {
                osdMessageVerbositycomboBox1.Items.Add(val);
            }
            osdMessageVerbositycomboBox1.SelectedItem = _settings.OSDMessageVerbosity.ToString();
            UpdateOSDNotes((AmstradCPC.OSDVerbosity)Enum.Parse(typeof(AmstradCPC.OSDVerbosity), osdMessageVerbositycomboBox1.SelectedItem.ToString()));
        }

		private void OkBtn_Click(object sender, EventArgs e)
		{
            bool changed =                
                _settings.OSDMessageVerbosity.ToString() != osdMessageVerbositycomboBox1.SelectedItem.ToString();

            if (changed)
			{
                _settings.OSDMessageVerbosity = (AmstradCPC.OSDVerbosity)Enum.Parse(typeof(AmstradCPC.OSDVerbosity), osdMessageVerbositycomboBox1.SelectedItem.ToString());

                GlobalWin.MainForm.PutCoreSettings(_settings);

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
			GlobalWin.OSD.AddMessage("Misc settings aborted");
			DialogResult = DialogResult.Cancel;
			Close();
		}

        private void UpdateOSDNotes(AmstradCPC.OSDVerbosity type)
        {
            switch (type)
            {
                case AmstradCPC.OSDVerbosity.Full:
                    lblOSDVerbinfo.Text = "Show all OSD messages";
                    break;
                case AmstradCPC.OSDVerbosity.Medium:
                    lblOSDVerbinfo.Text = "Only show machine/device generated messages";
                    break;
                case AmstradCPC.OSDVerbosity.None:
                    lblOSDVerbinfo.Text = "No core-driven OSD messages";
                    break;
            }
        }

        private void OSDComboBox_SelectionChangeCommitted(object sender, EventArgs e)
        {
            ComboBox cb = sender as ComboBox;
            UpdateOSDNotes((AmstradCPC.OSDVerbosity)Enum.Parse(typeof(AmstradCPC.OSDVerbosity), cb.SelectedItem.ToString()));
        }
    }
}
