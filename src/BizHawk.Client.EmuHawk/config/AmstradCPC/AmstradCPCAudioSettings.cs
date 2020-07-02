using System;
using System.Windows.Forms;
using BizHawk.Emulation.Cores.Computers.AmstradCPC;

namespace BizHawk.Client.EmuHawk
{
	public partial class AmstradCpcAudioSettings : Form
	{
		private readonly IMainFormForConfig _mainForm;
		private readonly AmstradCPC.AmstradCPCSettings _settings;

		public AmstradCpcAudioSettings(
			IMainFormForConfig mainForm,
			AmstradCPC.AmstradCPCSettings settings)
		{
			_mainForm = mainForm;
			_settings = settings;

			InitializeComponent();
			Icon = Properties.Resources.GameController_MultiSize;
		}

		private void IntvControllerSettings_Load(object sender, EventArgs e)
		{
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

				_mainForm.PutCoreSettings(_settings);

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
			_mainForm.AddOnScreenMessage("Misc settings aborted");
			DialogResult = DialogResult.Cancel;
			Close();
		}
	}
}
