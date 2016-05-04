using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class SoundConfig : Form
	{
		private bool _programmaticallyChangingValue;

		public SoundConfig()
		{
			InitializeComponent();
		}

		private void SoundConfig_Load(object sender, EventArgs e)
		{
			_programmaticallyChangingValue = true;

			cbEnableMaster.Checked = Global.Config.SoundEnabled;
			cbEnableNormal.Checked = Global.Config.SoundEnabledNormal;
			cbEnableRWFF.Checked = Global.Config.SoundEnabledRWFF;
			cbMuteFrameAdvance.Checked = Global.Config.MuteFrameAdvance;
#if !WINDOWS
			rbOutputMethodDirectSound.Enabled = false;
			rbOutputMethodXAudio2.Enabled = false;
#endif
			rbOutputMethodDirectSound.Checked = Global.Config.SoundOutputMethod == Config.ESoundOutputMethod.DirectSound;
			rbOutputMethodXAudio2.Checked = Global.Config.SoundOutputMethod == Config.ESoundOutputMethod.XAudio2;
			rbOutputMethodOpenAL.Checked = Global.Config.SoundOutputMethod == Config.ESoundOutputMethod.OpenAL;
			BufferSizeNumeric.Value = Global.Config.SoundBufferSizeMs;
			tbNormal.Value = Global.Config.SoundVolume;
			nudNormal.Value = Global.Config.SoundVolume;
			tbRWFF.Value = Global.Config.SoundVolumeRWFF;
			nudRWFF.Value = Global.Config.SoundVolumeRWFF;
			UpdateSoundDialog();

			_programmaticallyChangingValue = false;
		}

		private void OK_Click(object sender, EventArgs e)
		{
			if (rbOutputMethodDirectSound.Checked && (int)BufferSizeNumeric.Value < 60)
			{
				MessageBox.Show("Buffer size must be at least 60 milliseconds for DirectSound.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			var oldOutputMethod = Global.Config.SoundOutputMethod;
			var oldDevice = Global.Config.SoundDevice;
			Global.Config.SoundEnabled = cbEnableMaster.Checked;
			Global.Config.SoundEnabledNormal = cbEnableNormal.Checked;
			Global.Config.SoundEnabledRWFF = cbEnableRWFF.Checked;
			Global.Config.MuteFrameAdvance = cbMuteFrameAdvance.Checked;
			if (rbOutputMethodDirectSound.Checked) Global.Config.SoundOutputMethod = Config.ESoundOutputMethod.DirectSound;
			if (rbOutputMethodXAudio2.Checked) Global.Config.SoundOutputMethod = Config.ESoundOutputMethod.XAudio2;
			if (rbOutputMethodOpenAL.Checked) Global.Config.SoundOutputMethod = Config.ESoundOutputMethod.OpenAL;
			Global.Config.SoundBufferSizeMs = (int)BufferSizeNumeric.Value;
			Global.Config.SoundVolume = tbNormal.Value;
			Global.Config.SoundVolumeRWFF = tbRWFF.Value;
			Global.Config.SoundDevice = (string)listBoxSoundDevices.SelectedItem ?? "<default>";
			GlobalWin.Sound.StopSound();
			if (Global.Config.SoundOutputMethod != oldOutputMethod ||
				Global.Config.SoundDevice != oldDevice)
			{
				GlobalWin.Sound.Dispose();
				GlobalWin.Sound = new Sound(GlobalWin.MainForm.Handle);
			}
			GlobalWin.Sound.StartSound();
			GlobalWin.OSD.AddMessage("Sound settings saved");
			DialogResult = DialogResult.OK;
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			GlobalWin.OSD.AddMessage("Sound config aborted");
			Close();
		}

		private void PopulateDeviceList()
		{
			IEnumerable<string> deviceNames = Enumerable.Empty<string>();
#if WINDOWS
			if (rbOutputMethodDirectSound.Checked) deviceNames = DirectSoundSoundOutput.GetDeviceNames();
			if (rbOutputMethodXAudio2.Checked) deviceNames = XAudio2SoundOutput.GetDeviceNames();
#endif
			if (rbOutputMethodOpenAL.Checked) deviceNames = OpenALSoundOutput.GetDeviceNames();
			listBoxSoundDevices.Items.Clear();
			listBoxSoundDevices.Items.Add("<default>");
			listBoxSoundDevices.SelectedIndex = 0;
			foreach (var name in deviceNames)
			{
				listBoxSoundDevices.Items.Add(name);
				if (name == Global.Config.SoundDevice)
				{
					listBoxSoundDevices.SelectedItem = name;
				}
			}
		}

		private void OutputMethodRadioButtons_CheckedChanged(object sender, EventArgs e)
		{
			if (!((RadioButton)sender).Checked) return;
			PopulateDeviceList();
		}

		private void trackBar1_Scroll(object sender, EventArgs e)
		{
			nudNormal.Value = tbNormal.Value;
		}

		private void tbRWFF_Scroll(object sender, EventArgs e)
		{
			nudRWFF.Value = tbRWFF.Value;
		}

		private void SoundVolNumeric_ValueChanged(object sender, EventArgs e)
		{
			tbNormal.Value = (int)nudNormal.Value;

			// If the user is changing the volume, automatically turn on/off sound accordingly
			if (!_programmaticallyChangingValue)
				cbEnableNormal.Checked = tbNormal.Value != 0;
		}

		private void UpdateSoundDialog()
		{
			cbEnableRWFF.Enabled = cbEnableNormal.Checked;
			grpSoundVol.Enabled = cbEnableMaster.Checked;
		}


		private void UpdateSoundDialog(object sender, EventArgs e)
		{
			UpdateSoundDialog();
		}

	}
}
