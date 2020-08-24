using System;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class GreenzoneSettings : Form
	{
		private readonly Action<ZwinderStateManagerSettings, bool> _saveSettings;
		private ZwinderStateManagerSettings _settings;
		private readonly bool _isDefault;

		public GreenzoneSettings(ZwinderStateManagerSettings settings, Action<ZwinderStateManagerSettings, bool> saveSettings, bool isDefault)
		{
			InitializeComponent();
			Icon = Properties.Resources.TAStudioIcon;

			_saveSettings = saveSettings;
			_settings = settings;
			_isDefault = isDefault;
			if (!isDefault)
				Text = "Savestate History Settings";
			SettingsPropertyGrid.SelectedObject = _settings;
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			bool keep = false;
			if (!_isDefault)
				keep = (MessageBox.Show("Attempt to keep old states?", "Keep old states?", MessageBoxButtons.YesNo) == DialogResult.Yes);
			_saveSettings(_settings, keep);
			Close();
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void DefaultsButton_Click(object sender, EventArgs e)
		{
			_settings = new ZwinderStateManagerSettings();
			SettingsPropertyGrid.SelectedObject = _settings;
		}
	}
}
