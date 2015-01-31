using System;
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
			UseNewOutputBuffer.Checked = Global.Config.UseNewOutputBuffer;
			BufferSizeNumeric.Value = Global.Config.SoundBufferSizeMs;
			SoundVolBar.Value = Global.Config.SoundVolume;
			SoundVolNumeric.Value = Global.Config.SoundVolume;
			UpdateSoundDialog();

			listBoxSoundDevices.Items.Add("<default>");
			listBoxSoundDevices.SelectedIndex = 0;
			foreach (var name in DirectSoundSoundOutput.GetDeviceNames())
			{
				listBoxSoundDevices.Items.Add(name);
				if (name == Global.Config.SoundDevice)
				{
					listBoxSoundDevices.SelectedItem = name;
				}
			}

			_programmaticallyChangingValue = false;
		}

		private void OK_Click(object sender, EventArgs e)
		{
			Global.Config.SoundEnabled = SoundOnCheckBox.Checked;
			Global.Config.MuteFrameAdvance = MuteFrameAdvance.Checked;
			Global.Config.UseNewOutputBuffer = UseNewOutputBuffer.Checked;
			Global.Config.SoundBufferSizeMs = (int)BufferSizeNumeric.Value;
			Global.Config.SoundVolume = SoundVolBar.Value;
			Global.Config.SoundDevice = (string)listBoxSoundDevices.SelectedItem ?? "<default>";
			GlobalWin.Sound.StopSound();
			GlobalWin.Sound.StartSound();
			GlobalWin.OSD.AddMessage("Sound settings saved");
			Close();
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			GlobalWin.OSD.AddMessage("Sound config aborted");
			Close();
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
