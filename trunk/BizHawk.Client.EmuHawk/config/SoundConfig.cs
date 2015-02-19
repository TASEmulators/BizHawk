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

			SoundOnCheckBox.Checked = Global.Config.SoundEnabled;
			MuteFrameAdvance.Checked = Global.Config.MuteFrameAdvance;
#if !WINDOWS
			rbOutputMethodDirectSound.Enabled = false;
			rbOutputMethodXAudio2.Enabled = false;
#endif
			rbOutputMethodDirectSound.Checked = Global.Config.SoundOutputMethod == Config.ESoundOutputMethod.DirectSound;
			rbOutputMethodXAudio2.Checked = Global.Config.SoundOutputMethod == Config.ESoundOutputMethod.XAudio2;
			rbOutputMethodOpenAL.Checked = Global.Config.SoundOutputMethod == Config.ESoundOutputMethod.OpenAL;
			BufferSizeNumeric.Value = Global.Config.SoundBufferSizeMs;
			SoundVolBar.Value = Global.Config.SoundVolume;
			SoundVolNumeric.Value = Global.Config.SoundVolume;
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
			Global.Config.SoundEnabled = SoundOnCheckBox.Checked;
			Global.Config.MuteFrameAdvance = MuteFrameAdvance.Checked;
			if (rbOutputMethodDirectSound.Checked) Global.Config.SoundOutputMethod = Config.ESoundOutputMethod.DirectSound;
			if (rbOutputMethodXAudio2.Checked) Global.Config.SoundOutputMethod = Config.ESoundOutputMethod.XAudio2;
			if (rbOutputMethodOpenAL.Checked) Global.Config.SoundOutputMethod = Config.ESoundOutputMethod.OpenAL;
			Global.Config.SoundBufferSizeMs = (int)BufferSizeNumeric.Value;
			Global.Config.SoundVolume = SoundVolBar.Value;
			Global.Config.SoundDevice = (string)listBoxSoundDevices.SelectedItem ?? "<default>";
			GlobalWin.Sound.StopSound();
			if (Global.Config.SoundOutputMethod != oldOutputMethod)
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
			SoundVolNumeric.Value = SoundVolBar.Value;
		}

		private void SoundVolNumeric_ValueChanged(object sender, EventArgs e)
		{
			SoundVolBar.Value = (int)SoundVolNumeric.Value;

			// If the user is changing the volume, automatically turn on/off sound accordingly
			if (!_programmaticallyChangingValue)
				SoundOnCheckBox.Checked = SoundVolBar.Value != 0;
		}

		private void SoundOnCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			UpdateSoundDialog();
		}

		private void UpdateSoundDialog()
		{
			MuteFrameAdvance.Enabled = SoundOnCheckBox.Checked;
		}
	}
}
