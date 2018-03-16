using System;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Cores.Computers.SinclairSpectrum;
using System.Text;

namespace BizHawk.Client.EmuHawk
{
	public partial class ZXSpectrumNonSyncSettings : Form
	{
		private ZXSpectrum.ZXSpectrumSettings _settings;

		public ZXSpectrumNonSyncSettings()
		{
			InitializeComponent();
		}

		private void IntvControllerSettings_Load(object sender, EventArgs e)
		{
			_settings = ((ZXSpectrum)Global.Emulator).GetSettings().Clone();

            // autoload tape
            autoLoadcheckBox1.Checked = _settings.AutoLoadTape;

            // AY panning config
            var panTypes = Enum.GetNames(typeof(AYChip.AYPanConfig));
			foreach (var val in panTypes)
			{
				panTypecomboBox1.Items.Add(val);
            }
            panTypecomboBox1.SelectedItem = _settings.AYPanConfig.ToString();            
        }

		private void OkBtn_Click(object sender, EventArgs e)
		{
            bool changed =
                _settings.AutoLoadTape != autoLoadcheckBox1.Checked
                || _settings.AYPanConfig.ToString() != panTypecomboBox1.SelectedItem.ToString();

            if (changed)
			{
                _settings.AutoLoadTape = autoLoadcheckBox1.Checked;
                _settings.AYPanConfig = (AYChip.AYPanConfig)Enum.Parse(typeof(AYChip.AYPanConfig), panTypecomboBox1.SelectedItem.ToString());

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
