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
			txtName.Text = syncSettings.nickname;
			cbxFavColor.SelectedIndex = syncSettings.favoriteColor;
			numBirthDay.Value = syncSettings.birthdayDay;
			numBirthMonth.Value = syncSettings.birthdayMonth;
		}

		private void btnCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void btnSave_Click(object sender, EventArgs e)
		{
			syncSettings.bootToFirmware = chkBootToFirmware.Checked;
			syncSettings.nickname = txtName.Text;
			syncSettings.favoriteColor = (byte)cbxFavColor.SelectedIndex;
			syncSettings.birthdayDay = (byte)numBirthDay.Value;
			syncSettings.birthdayMonth = (byte)numBirthMonth.Value;

			Global.Config.PutCoreSyncSettings<MelonDS>(syncSettings);
			bool reboot = (Global.Emulator as MelonDS).PutSyncSettings(syncSettings);
			DialogResult = reboot ? DialogResult.Yes : DialogResult.OK;
			Close();
		}

		private void numBirthMonth_ValueChanged(object sender, EventArgs e)
		{
			switch (numBirthMonth.Value)
			{
				case 1:
				case 3:
				case 5:
				case 7:
				case 8:
				case 10:
				case 12:
					numBirthDay.Maximum = 31;
					break;
				case 4:
				case 6:
				case 9:
				case 11:
					numBirthDay.Maximum = 30;
					break;
				case 2:
					numBirthDay.Maximum = 29;
					break;
			}
		}

		private void btnDefault_Click(object sender, EventArgs e)
		{
			if (MessageBox.Show("Revert to and save default settings?", "default settings", MessageBoxButtons.OKCancel) == DialogResult.OK)
			{
				bool reboot = (Global.Emulator as MelonDS).PutSyncSettings(null);
				syncSettings = (Global.Emulator as MelonDS).GetSyncSettings();
				Global.Config.PutCoreSyncSettings<MelonDS>(syncSettings);
				DialogResult = reboot ? DialogResult.Yes : DialogResult.OK;
				Close();
			}
		}
	}
}
