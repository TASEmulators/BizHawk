using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk.config.NES
{
	public partial class NESSyncSettingsForm : Form
	{
		DataTableDictionaryBind<string, string> DTDB;
		BizHawk.Emulation.Cores.Nintendo.NES.NES.NESSyncSettings SyncSettings;

		public NESSyncSettingsForm()
		{
			InitializeComponent();
			SyncSettings = (BizHawk.Emulation.Cores.Nintendo.NES.NES.NESSyncSettings)Global.Emulator.GetSyncSettings();
			DTDB = new DataTableDictionaryBind<string, string>(SyncSettings.BoardProperties);
			dataGridView1.DataSource = DTDB.Table;
		}

		private void buttonCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			if (DTDB.WasModified)
				GlobalWin.MainForm.PutCoreSyncSettings(SyncSettings);
		}

		private void buttonHelp_Click(object sender, EventArgs e)
		{
			MessageBox.Show(this, "Board Properties are special per-mapper system settings.  They are only useful to advanced users creating Tool Assisted Superplays.  No support will be provided if you break something with them.", "Help");
		}
	}
}
