using System.Linq;
using System.Windows.Forms;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Atari.A7800Hawk;

namespace BizHawk.Client.EmuHawk
{
	public partial class A7800FilterSettings : Form
	{
		private readonly ISettingsAdapter _settable;

		private readonly A7800Hawk.A7800SyncSettings _syncSettings;

		public A7800FilterSettings(ISettingsAdapter settable)
		{
			_settable = settable;
			_syncSettings = (A7800Hawk.A7800SyncSettings) _settable.GetSyncSettings();
			InitializeComponent();
			Icon = Properties.Resources.GameControllerIcon;
		}

		private void A7800FilterSettings_Load(object sender, EventArgs e)
		{
			var possibleFilters = A7800Hawk.ValidFilterTypes.Select(t => t.Key);

			foreach (var val in possibleFilters)
			{
				Port1ComboBox.Items.Add(val);
			}

			Port1ComboBox.SelectedItem = _syncSettings.Filter;
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			bool changed = _syncSettings.Filter != Port1ComboBox.SelectedItem.ToString();

			if (changed)
			{
				_syncSettings.Filter = Port1ComboBox.SelectedItem.ToString();
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
