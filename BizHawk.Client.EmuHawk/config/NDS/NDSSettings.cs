using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Cores.Consoles.Nintendo.NDS;

namespace BizHawk.Client.EmuHawk
{
	public partial class NDSSettings : Form
	{
		public NDSSettings()
		{
			InitializeComponent();
		}

		MelonDS.MelonSyncSettings syncSettings;

		private void NDSSettings_Load(object sender, EventArgs e)
		{
			syncSettings = Global.Config.GetCoreSyncSettings<MelonDS>() as MelonDS.MelonSyncSettings;

			chkBootToFirmware.Checked = syncSettings.bootToFirmware;
		}

		private void btnCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void btnSave_Click(object sender, EventArgs e)
		{
			syncSettings.bootToFirmware = chkBootToFirmware.Checked;

			Global.Config.PutCoreSyncSettings<MelonDS>(syncSettings);
			bool reboot = (Global.Emulator as MelonDS).PutSyncSettings(syncSettings);
			DialogResult = reboot ? DialogResult.Yes : DialogResult.OK;
			Close();
		}
	}
}
