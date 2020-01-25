using System;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class DefaultGreenzoneSettings : Form
	{
		private TasStateManagerSettings _settings;

		public DefaultGreenzoneSettings()
		{
			InitializeComponent();

			_settings = new TasStateManagerSettings(Global.Config.DefaultTasStateManagerSettings);

			SettingsPropertyGrid.SelectedObject = _settings;
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			Global.Config.DefaultTasStateManagerSettings = _settings;
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
