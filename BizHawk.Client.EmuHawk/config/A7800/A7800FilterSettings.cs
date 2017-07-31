using System;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Cores.Atari.A7800Hawk;

namespace BizHawk.Client.EmuHawk
{
	public partial class A7800FilterSettings : Form
	{
		private A7800Hawk.A7800SyncSettings _syncSettings;

		public A7800FilterSettings()
		{
			InitializeComponent();
		}

		private void A7800FilterSettings_Load(object sender, EventArgs e)
		{
			_syncSettings = ((A7800Hawk)Global.Emulator).GetSyncSettings().Clone();

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

				GlobalWin.MainForm.PutCoreSyncSettings(_syncSettings);
			}

			DialogResult = DialogResult.OK;
			Close();
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			GlobalWin.OSD.AddMessage("Filter settings aborted");
			DialogResult = DialogResult.Cancel;
			Close();
		}
	}
}
