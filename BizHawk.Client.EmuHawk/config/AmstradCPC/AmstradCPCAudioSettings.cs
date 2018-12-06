using System;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Cores.Computers.AmstradCPC;
using System.Text;

namespace BizHawk.Client.EmuHawk
{
	public partial class AmstradCPCAudioSettings : Form
	{
		private AmstradCPC.AmstradCPCSettings _settings;

		public AmstradCPCAudioSettings()
		{
			InitializeComponent();
		}

		private void IntvControllerSettings_Load(object sender, EventArgs e)
		{
			_settings = ((AmstradCPC)Global.Emulator).GetSettings().Clone();

            // AY panning config
            var panTypes = Enum.GetNames(typeof(AY38912.AYPanConfig));
			foreach (var val in panTypes)
			{
				panTypecomboBox1.Items.Add(val);
            }
            panTypecomboBox1.SelectedItem = _settings.AYPanConfig.ToString();

            // tape volume
            tapeVolumetrackBar.Value = _settings.TapeVolume;

            // ay volume
            ayVolumetrackBar.Value = _settings.AYVolume;

            
        }

		private void OkBtn_Click(object sender, EventArgs e)
		{
            bool changed =
                _settings.AYPanConfig.ToString() != panTypecomboBox1.SelectedItem.ToString()
                || _settings.TapeVolume != tapeVolumetrackBar.Value
                || _settings.AYVolume != ayVolumetrackBar.Value;

            if (changed)
			{
                _settings.AYPanConfig = (AY38912.AYPanConfig)Enum.Parse(typeof(AY38912.AYPanConfig), panTypecomboBox1.SelectedItem.ToString());

                _settings.TapeVolume = tapeVolumetrackBar.Value;
                _settings.AYVolume = ayVolumetrackBar.Value;

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
    }
}
