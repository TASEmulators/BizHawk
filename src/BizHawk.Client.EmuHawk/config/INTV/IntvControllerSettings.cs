using System.Windows.Forms;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Intellivision;

namespace BizHawk.Client.EmuHawk
{
	public partial class IntvControllerSettings : Form
	{
		private readonly ISettingsAdapter _settable;

		private readonly Intellivision.IntvSyncSettings _syncSettings;

		public IntvControllerSettings(ISettingsAdapter settable)
		{
			_settable = settable;
			_syncSettings = (Intellivision.IntvSyncSettings) _settable.GetSyncSettings();
			InitializeComponent();
			Icon = Properties.Resources.GameControllerIcon;
		}

		private void IntvControllerSettings_Load(object sender, EventArgs e)
		{
			foreach (var val in IntellivisionControllerDeck.ControllerCtors.Keys)
			{
				Port1ComboBox.Items.Add(val);
				Port2ComboBox.Items.Add(val);
			}

			Port1ComboBox.SelectedItem = _syncSettings.Port1;
			Port2ComboBox.SelectedItem = _syncSettings.Port2;
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			bool changed =
				_syncSettings.Port1 != Port1ComboBox.SelectedItem.ToString()
				|| _syncSettings.Port2 != Port2ComboBox.SelectedItem.ToString();

			if (changed)
			{
				_syncSettings.Port1 = Port1ComboBox.SelectedItem.ToString();
				_syncSettings.Port2 = Port2ComboBox.SelectedItem.ToString();

				_settable.PutCoreSyncSettings(_syncSettings);
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
