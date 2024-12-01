using System.Windows.Forms;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Computers.AmstradCPC;

namespace BizHawk.Client.EmuHawk
{
	public partial class AmstradCpcAudioSettings : Form
	{
		private readonly ISettingsAdapter _settable;

		private readonly AmstradCPC.AmstradCPCSettings _settings;

		public AmstradCpcAudioSettings(ISettingsAdapter settable)
		{
			_settable = settable;
			_settings = (AmstradCPC.AmstradCPCSettings) _settable.GetSettings();

			InitializeComponent();
			Icon = Properties.Resources.GameControllerIcon;
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

				_settable.PutCoreSettings(_settings);
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
