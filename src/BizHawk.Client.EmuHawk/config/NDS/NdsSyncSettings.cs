using System;
using System.Windows.Forms;
using BizHawk.Emulation.Cores.Consoles.Nintendo.NDS;

namespace BizHawk.Client.EmuHawk
{
	public partial class NdsSyncSettings : Form
	{
		private readonly IMainFormForConfig _mainForm;
		private readonly MelonDS.MelonSyncSettings _syncSettings;

		public NdsSyncSettings(
			IMainFormForConfig mainForm,
			MelonDS.MelonSyncSettings syncSettings)
		{
			_mainForm = mainForm;
			_syncSettings = syncSettings;

			InitializeComponent();
		}

		private void NDSSettings_Load(object sender, EventArgs e)
		{
			chkBootToFirmware.Checked = _syncSettings.BootToFirmware;
			txtName.Text = _syncSettings.Nickname;
			cbxFavColor.SelectedIndex = _syncSettings.FavoriteColor;
			numBirthDay.Value = _syncSettings.BirthdayDay;
			numBirthMonth.Value = _syncSettings.BirthdayMonth;
			dtpStartupTime.Value = DateTimeOffset.FromUnixTimeSeconds(_syncSettings.TimeAtBoot).UtcDateTime;
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

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			_mainForm.AddOnScreenMessage("Core emulator settings aborted");
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void SaveBtn_Click(object sender, EventArgs e)
		{
			_syncSettings.BootToFirmware = chkBootToFirmware.Checked;
			_syncSettings.Nickname = txtName.Text;
			_syncSettings.FavoriteColor = (byte)cbxFavColor.SelectedIndex;
			_syncSettings.BirthdayDay = (byte)numBirthDay.Value;
			_syncSettings.BirthdayMonth = (byte)numBirthMonth.Value;

			// Converting to local time is necessary, because user-set values are "unspecified" which ToUnixTimeSeconds assumes are local.
			// But ToLocalTime assumes these are UTC. So here we are adding and then subtracting the UTC-to-local offset.
			_syncSettings.TimeAtBoot = (uint)new DateTimeOffset(dtpStartupTime.Value.ToLocalTime()).ToUnixTimeSeconds();

			_mainForm.PutCoreSyncSettings(_syncSettings);
			DialogResult =  DialogResult.OK;
			Close();
		}

		private void DefaultBtn_Click(object sender, EventArgs e)
		{
			if (MessageBox.Show("Revert to and save default settings?", "default settings", MessageBoxButtons.OKCancel).IsOk())
			{
				_mainForm.PutCoreSyncSettings(new MelonDS.MelonSyncSettings());
				DialogResult = DialogResult.OK;
				Close();
			}
		}
	}
}
