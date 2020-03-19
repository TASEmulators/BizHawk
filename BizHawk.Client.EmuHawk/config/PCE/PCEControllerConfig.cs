using System;
using System.Windows.Forms;
using BizHawk.Emulation.Cores.PCEngine;

namespace BizHawk.Client.EmuHawk
{
	public partial class PCEControllerConfig : Form
	{
		private readonly MainForm _mainForm;
		private readonly PCEngine.PCESyncSettings _syncSettings;

		public PCEControllerConfig(
			MainForm mainForm,
			PCEngine.PCESyncSettings syncSettings)
		{
			_mainForm = mainForm;
			_syncSettings = syncSettings;
			InitializeComponent();
		}

		private void PCEControllerConfig_Load(object sender, EventArgs e)
		{
			ControllerPropertyGrid.SelectedObject = _syncSettings;
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
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
