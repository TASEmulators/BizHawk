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
using BizHawk.Common.NumberExtensions;
namespace BizHawk.Client.EmuHawk
{
	public partial class NESSyncSettingsForm : Form
	{
		DataTableDictionaryBind<string, string> DTDB;
		NES.NESSyncSettings SyncSettings;

		public NESSyncSettingsForm()
		{
			InitializeComponent();

			SyncSettings = ((NES)Global.Emulator).GetSyncSettings();

			if ((Global.Emulator as NES).HasMapperProperties)
			{
				
				DTDB = new DataTableDictionaryBind<string, string>(SyncSettings.BoardProperties);
				dataGridView1.DataSource = DTDB.Table;
				InfoLabel.Visible = false;
			}
			else
			{
				BoardPropertiesGroupBox.Enabled = false;
				dataGridView1.DataSource = null;
				dataGridView1.Enabled = false;
				InfoLabel.Visible = true;
			}

			RegionComboBox.Items.AddRange(Enum.GetNames(typeof(NES.NESSyncSettings.Region)));
			RegionComboBox.SelectedItem = Enum.GetName(typeof(NES.NESSyncSettings.Region), SyncSettings.RegionOverride);

			if (SyncSettings.InitialWRamStatePattern != null && SyncSettings.InitialWRamStatePattern.Any())
			{
				var sb = new StringBuilder();
				foreach (var b in SyncSettings.InitialWRamStatePattern)
				{
					sb.Append(b.ToHexString(2));
				}

				RamPatternOverrideBox.Text = sb.ToString();
			}
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{

			var old = SyncSettings.RegionOverride;
			SyncSettings.RegionOverride = (NES.NESSyncSettings.Region)
				Enum.Parse(
				typeof(NES.NESSyncSettings.Region),
				(string)RegionComboBox.SelectedItem);

			List<byte> oldRam = SyncSettings.InitialWRamStatePattern != null ? SyncSettings.InitialWRamStatePattern.ToList() : new List<byte>();

			if (!string.IsNullOrWhiteSpace(RamPatternOverrideBox.Text))
			{
				SyncSettings.InitialWRamStatePattern = Enumerable.Range(0, RamPatternOverrideBox.Text.Length)
					 .Where(x => x % 2 == 0)
					 .Select(x => Convert.ToByte(RamPatternOverrideBox.Text.Substring(x, 2), 16))
					 .ToList();
			}
			else
			{
				SyncSettings.InitialWRamStatePattern = null;
			}

			bool changed = (DTDB != null && DTDB.WasModified) ||
				old != SyncSettings.RegionOverride ||
				!(oldRam.SequenceEqual(SyncSettings.InitialWRamStatePattern ?? new List<byte>()));

			DialogResult = DialogResult.OK;
			if (changed)
			{
				GlobalWin.MainForm.PutCoreSyncSettings(SyncSettings);
			}
		}

		private void HelpBtn_Click(object sender, EventArgs e)
		{
			MessageBox.Show(
				this,
				"Board Properties are special per-mapper system settings.  They are only useful to advanced users creating Tool Assisted Superplays.  No support will be provided if you break something with them.",
				"Help",
				MessageBoxButtons.OK,
				MessageBoxIcon.Information);
		}
	}
}
