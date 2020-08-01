using System;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class DefaultGreenzoneSettings : Form
	{
		private readonly Action<TasStateManagerSettings> _saveSettings;
		private TasStateManagerSettings _settings;

		public DefaultGreenzoneSettings(TasStateManagerSettings settings, Action<TasStateManagerSettings> saveSettings)
		{
			InitializeComponent();
			Icon = Properties.Resources.TAStudioIcon;

			_saveSettings = saveSettings;
			_settings = settings;
			SettingsPropertyGrid.SelectedObject = _settings;
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			_saveSettings(_settings);
			Close();
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void DefaultsButton_Click(object sender, EventArgs e)
		{
			_settings = new TasStateManagerSettings();
			SettingsPropertyGrid.SelectedObject = _settings;
		}
	}
}
