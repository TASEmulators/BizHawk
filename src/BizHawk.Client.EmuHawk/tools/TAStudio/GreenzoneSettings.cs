using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class GreenzoneSettings : Form, IDialogParent
	{
		private readonly Action<PagedStateManager.PagedSettings, bool> _saveSettings;
		private PagedStateManager.PagedSettings _settings;
		private readonly bool _isDefault;

		public IDialogController DialogController { get; }

		public GreenzoneSettings(IDialogController dialogController, PagedStateManager.PagedSettings settings, Action<PagedStateManager.PagedSettings, bool> saveSettings, bool isDefault)
		{
			DialogController = dialogController;
			InitializeComponent();
			Icon = Properties.Resources.TAStudioIcon;

			_saveSettings = saveSettings;
			_settings = settings;
			_isDefault = isDefault;
			if (!isDefault)
				Text = "Savestate History Settings";
			SettingsPropertyGrid.SelectedObject = _settings;
		}

		private void GreenzoneSettings_Load(object sender, EventArgs e)
		{
			SettingsPropertyGrid.AdjustDescriptionHeightToFit();
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			var keep = !_isDefault && DialogController.ShowMessageBox2("Attempt to keep old states?", "Keep old states?");
			_saveSettings(_settings, keep);
			Close();
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void DefaultsButton_Click(object sender, EventArgs e)
		{
			_settings = new PagedStateManager.PagedSettings();
			SettingsPropertyGrid.SelectedObject = _settings;
		}
	}
}
