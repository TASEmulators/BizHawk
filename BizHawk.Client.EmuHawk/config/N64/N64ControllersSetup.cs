using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Emulation.Cores.Nintendo.N64;

namespace BizHawk.Client.EmuHawk
{
	public partial class N64ControllersSetup : Form
	{
		private readonly MainForm _mainForm;
		private readonly N64SyncSettings _syncSettings;

		private List<N64ControllerSettingControl> ControllerSettingControls => Controls
			.OfType<N64ControllerSettingControl>()
			.OrderBy(n => n.ControllerNumber)
			.ToList();

		public N64ControllersSetup(
			MainForm mainForm,
			N64SyncSettings syncSettings)
		{
			_mainForm = mainForm;
			_syncSettings = syncSettings;
			InitializeComponent();
		}

		private void N64ControllersSetup_Load(object sender, EventArgs e)
		{
			ControllerSettingControls
				.ForEach(c =>
				{
					c.IsConnected = _syncSettings.Controllers[c.ControllerNumber - 1].IsConnected;
					c.PakType = _syncSettings.Controllers[c.ControllerNumber - 1].PakType;
					c.Refresh();
				});
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			ControllerSettingControls
				.ForEach(c =>
				{
					_syncSettings.Controllers[c.ControllerNumber - 1].IsConnected = c.IsConnected;
					_syncSettings.Controllers[c.ControllerNumber - 1].PakType = c.PakType;
				});

			_mainForm.PutCoreSyncSettings(_syncSettings);

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
