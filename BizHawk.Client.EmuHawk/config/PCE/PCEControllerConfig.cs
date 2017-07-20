using System;
using System.Windows.Forms;

using BizHawk.Emulation.Cores.PCEngine;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class PCEControllerConfig : Form
	{
		private PCEngine.PCESyncSettings _controllerSettings;

		public PCEControllerConfig()
		{
			InitializeComponent();
		}

		private void PCEControllerConfig_Load(object sender, EventArgs e)
		{
			var pceSettings = ((PCEngine)Global.Emulator).GetSyncSettings();
			_controllerSettings = pceSettings; // Assumes only controller data is in sync settings! If there are ever more sync settings, this dialog should just become a general sync settings dialog (or both settings/sync settings)
			ControllerPropertyGrid.SelectedObject = _controllerSettings;
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			GlobalWin.MainForm.PutCoreSyncSettings(_controllerSettings);
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
