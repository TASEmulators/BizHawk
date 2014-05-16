using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using BizHawk.Client.Common;
using BizHawk.Emulation.Cores.Nintendo.NES;

namespace BizHawk.Client.EmuHawk
{
	public partial class NESSyncSettingsForm : Form
	{
		DataTableDictionaryBind<string, string> DTDB;
		NES.NESSyncSettings SyncSettings;

		public NESSyncSettingsForm()
		{
			InitializeComponent();
			SyncSettings = (NES.NESSyncSettings)Global.Emulator.GetSyncSettings();
			DTDB = new DataTableDictionaryBind<string, string>(SyncSettings.BoardProperties);
			dataGridView1.DataSource = DTDB.Table;

			comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
			comboBox1.Items.AddRange(Enum.GetNames(typeof(NES.NESSyncSettings.Region)));
			comboBox1.SelectedItem = Enum.GetName(typeof(NES.NESSyncSettings.Region), SyncSettings.RegionOverride);
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{

			var old = SyncSettings.RegionOverride;
			SyncSettings.RegionOverride = (BizHawk.Emulation.Cores.Nintendo.NES.NES.NESSyncSettings.Region)
				Enum.Parse(
				typeof(BizHawk.Emulation.Cores.Nintendo.NES.NES.NESSyncSettings.Region),
				(string)comboBox1.SelectedItem);

			bool changed = DTDB.WasModified ||
				old != SyncSettings.RegionOverride;

			DialogResult = DialogResult.OK;
			if (changed)
			{
				GlobalWin.MainForm.PutCoreSyncSettings(SyncSettings);
			}
		}

		private void HelpBtn_Click(object sender, EventArgs e)
		{
			MessageBox.Show(this, "Board Properties are special per-mapper system settings.  They are only useful to advanced users creating Tool Assisted Superplays.  No support will be provided if you break something with them.", "Help");
		}

		private void NESSyncSettingsForm_Load(object sender, EventArgs e)
		{

		}
	}
}
