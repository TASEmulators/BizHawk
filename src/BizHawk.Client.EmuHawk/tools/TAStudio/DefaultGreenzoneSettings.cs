using System;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class DefaultGreenzoneSettings : Form
	{
		private readonly MovieConfig _movieSettings;
		private TasStateManagerSettings _settings;

		public DefaultGreenzoneSettings(MovieConfig movieSettings)
		{
			InitializeComponent();
			Icon = Properties.Resources.TAStudio_MultiSize;
			_movieSettings = movieSettings;
			_settings = new TasStateManagerSettings(movieSettings.DefaultTasStateManagerSettings);
			SettingsPropertyGrid.SelectedObject = _settings;
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			_movieSettings.DefaultTasStateManagerSettings = _settings;
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
