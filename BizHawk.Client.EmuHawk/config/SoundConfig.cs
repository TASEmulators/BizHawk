using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Common;
using BizHawk.WinForms.Controls;

namespace BizHawk.Client.EmuHawk
{
	public partial class SoundConfig : Form
	{
		private readonly Config _config;
		private bool _programmaticallyChangingValue;

		public SoundConfig(Config config)
		{
			_config = config;
			InitializeComponent();
		}

		private void SoundConfig_Load(object sender, EventArgs e)
		{
			_programmaticallyChangingValue = true;

			cbMasterEnable.Checked = _config.SoundEnabled;
			cbFullSpeedEnable.Checked = _config.SoundEnabledNormal;
			cbRewindFFWEnable.Checked = _config.SoundEnabledRWFF;
			cbMuteFrameAdvance.Checked = _config.MuteFrameAdvance;

			if (OSTailoredCode.IsUnixHost)
			{
				// Disable DirectSound and XAudio2 on Mono
				rbSoundMethodDirectSound.Enabled = false;
				rbSoundMethodXAudio2.Enabled = false;
			}

			rbSoundMethodDirectSound.Checked = _config.SoundOutputMethod == ESoundOutputMethod.DirectSound;
			rbSoundMethodXAudio2.Checked = _config.SoundOutputMethod == ESoundOutputMethod.XAudio2;
			rbSoundMethodOpenAL.Checked = _config.SoundOutputMethod == ESoundOutputMethod.OpenAL;
			nudBufferSize.Value = _config.SoundBufferSizeMs;
			tbFullSpeedVolume.Value = _config.SoundVolume;
			nudFullSpeedVolume.Value = _config.SoundVolume;
			tbRewindFFWVolume.Value = _config.SoundVolumeRWFF;
			nudRewindFFWVolume.Value = _config.SoundVolumeRWFF;
			UpdateSoundDialog();

			_programmaticallyChangingValue = false;
		}

		private void btnDialogOK_Click(object sender, EventArgs e)
		{
			if (rbSoundMethodDirectSound.Checked && (int)nudBufferSize.Value < 60)
			{
				MessageBox.Show("Buffer size must be at least 60 milliseconds for DirectSound.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			var oldOutputMethod = _config.SoundOutputMethod;
			var oldDevice = _config.SoundDevice;
			_config.SoundEnabled = cbMasterEnable.Checked;
			_config.SoundEnabledNormal = cbFullSpeedEnable.Checked;
			_config.SoundEnabledRWFF = cbRewindFFWEnable.Checked;
			_config.MuteFrameAdvance = cbMuteFrameAdvance.Checked;
			if (rbSoundMethodDirectSound.Checked) _config.SoundOutputMethod = ESoundOutputMethod.DirectSound;
			if (rbSoundMethodXAudio2.Checked) _config.SoundOutputMethod = ESoundOutputMethod.XAudio2;
			if (rbSoundMethodOpenAL.Checked) _config.SoundOutputMethod = ESoundOutputMethod.OpenAL;
			_config.SoundBufferSizeMs = (int)nudBufferSize.Value;
			_config.SoundVolume = tbFullSpeedVolume.Value;
			_config.SoundVolumeRWFF = tbRewindFFWVolume.Value;
			_config.SoundDevice = (string)listDevices.SelectedItem ?? "<default>";
			GlobalWin.Sound.StopSound();
			if (_config.SoundOutputMethod != oldOutputMethod
				|| _config.SoundDevice != oldDevice)
			{
				GlobalWin.Sound.Dispose();
				GlobalWin.Sound = new Sound(Owner.Handle);
			}

			DialogResult = DialogResult.OK;
		}

		private void btnDialogCancel_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void PopulateDeviceList()
		{
			IEnumerable<string> deviceNames = Enumerable.Empty<string>();
			if (!OSTailoredCode.IsUnixHost)
			{
				if (rbSoundMethodDirectSound.Checked) deviceNames = DirectSoundSoundOutput.GetDeviceNames();
				if (rbSoundMethodXAudio2.Checked) deviceNames = XAudio2SoundOutput.GetDeviceNames();
			}
			if (rbSoundMethodOpenAL.Checked) deviceNames = OpenALSoundOutput.GetDeviceNames();

			listDevices.Items.Clear();
			listDevices.Items.Add("<default>");
			listDevices.SelectedIndex = 0;
			foreach (var name in deviceNames)
			{
				listDevices.Items.Add(name);
				if (name == _config.SoundDevice)
				{
					listDevices.SelectedItem = name;
				}
			}
		}

		private void rbSoundMethodAllRadios_CheckedChanged(object sender, EventArgs e)
		{
			if (!((RadioButtonEx)sender).Checked)
			{
				return;
			}

			PopulateDeviceList();
		}

		private void tbFullSpeedVolume_Scroll(object sender, EventArgs e)
		{
			nudFullSpeedVolume.Value = tbFullSpeedVolume.Value;
		}

		private void tbRewindFFWVolume_Scroll(object sender, EventArgs e)
		{
			nudRewindFFWVolume.Value = tbRewindFFWVolume.Value;
		}

		private void nudFullSpeedVolume_ValueChanged(object sender, EventArgs e)
		{
			tbFullSpeedVolume.Value = (int)nudFullSpeedVolume.Value;

			// If the user is changing the volume, automatically turn on/off sound accordingly
			if (!_programmaticallyChangingValue)
			{
				cbFullSpeedEnable.Checked = tbFullSpeedVolume.Value != 0;
			}
		}

		private void UpdateSoundDialog()
		{
			cbRewindFFWEnable.Enabled = cbFullSpeedEnable.Checked;
			grpVolume.Enabled = cbMasterEnable.Checked;
		}

		private void cbMasterOrFullSpeed_CheckedChanged(object sender, EventArgs e)
		{
			UpdateSoundDialog();
		}
	}
}
