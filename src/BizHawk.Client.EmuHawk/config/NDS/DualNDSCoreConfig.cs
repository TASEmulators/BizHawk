using System;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Consoles.Nintendo.NDS;

namespace BizHawk.Client.EmuHawk
{
	public partial class DualNDSCoreConfig : Form
	{
		private readonly IMainFormForConfig _mainForm;
		private DualNDS.DualNDSSettings _settings;
		private DualNDS.DualNDSSyncSettings _syncSettings;
		private bool _settingsChanged, _syncSettingsChanged;

		private DualNDSCoreConfig(IMainFormForConfig mainForm, DualNDS.DualNDSSettings settings, DualNDS.DualNDSSyncSettings syncSettings)
		{
			InitializeComponent();
			_mainForm = mainForm;

			_settings = settings;
			_syncSettings = syncSettings;

			checkBoxLeftAccurateAudioBitrate.Checked = _settings.L.AccurateAudioBitrate;
			checkBoxRightAccurateAudioBitrate.Checked = _settings.R.AccurateAudioBitrate;

			propertyGrid1.SelectedObject = _syncSettings.L;
			propertyGrid1.AdjustDescriptionHeightToFit();

			propertyGrid2.SelectedObject = _syncSettings.R;
			propertyGrid2.AdjustDescriptionHeightToFit();

			if (_mainForm.MovieSession.Movie.IsActive())
			{
				propertyGrid2.Enabled = false; // disable changes to sync setting when movie, so as not to confuse user
			}
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			if (_settingsChanged)
			{
				_settings.L.AccurateAudioBitrate = checkBoxLeftAccurateAudioBitrate.Checked;
				_settings.R.AccurateAudioBitrate = checkBoxRightAccurateAudioBitrate.Checked;
				_mainForm.PutCoreSettings(_settings);
			}

			if (_syncSettingsChanged)
			{
				_mainForm.PutCoreSyncSettings(_syncSettings);
			}

			DialogResult = DialogResult.OK;
			Close();
		}

		public static void DoDialog(IMainFormForConfig owner, DualNDS.DualNDSSettings settings, DualNDS.DualNDSSyncSettings syncSettings)
		{
			using var dlg = new DualNDSCoreConfig(owner, settings, syncSettings) { Text = "Dual NDS Settings" };
			owner.ShowDialogAsChild(dlg);
		}

		private void PropertyGrid1_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
		{
			_syncSettingsChanged = true;
		}

		private void PropertyGrid2_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
		{
			_syncSettingsChanged = true;
		}

		private void DefaultsBtn_Click(object sender, EventArgs e)
		{
			_settings = new DualNDS.DualNDSSettings();
			_syncSettings = new DualNDS.DualNDSSyncSettings();

			checkBoxLeftAccurateAudioBitrate.Checked = _settings.L.AccurateAudioBitrate;
			checkBoxRightAccurateAudioBitrate.Checked = _settings.R.AccurateAudioBitrate;

			propertyGrid1.SelectedObject = _syncSettings.L;
			propertyGrid2.SelectedObject = _syncSettings.R;

			_settingsChanged = true;
			_syncSettingsChanged = true;
		}

		private void CheckBoxLeftAccurateAudioBitrate_CheckedChanged(object sender, EventArgs e)
		{
			_settingsChanged = true;
		}

		private void CheckBoxRightAccurateAudioBitrate_CheckedChanged(object sender, EventArgs e)
		{
			_settingsChanged = true;
		}
	}
}
