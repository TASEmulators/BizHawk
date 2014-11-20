using System;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class SoundConfig : Form
	{
		public SoundConfig()
		{
			InitializeComponent();
		}

		private void SoundConfig_Load(object sender, EventArgs e)
		{
			SoundOnCheckBox.Checked = Global.Config.SoundEnabled;
			MuteFrameAdvance.Checked = Global.Config.MuteFrameAdvance;
			ThrottlecheckBox.Checked = Global.Config.SoundThrottle;
			SoundVolBar.Value = Global.Config.SoundVolume;
			SoundVolNumeric.Value = Global.Config.SoundVolume;
			UpdateSoundDialog();

			// vestigal
			ThrottlecheckBox.Visible = false;


			var dd = SoundEnumeration.DeviceNames();
			listBoxSoundDevices.Items.Add("<default>");
			listBoxSoundDevices.SelectedIndex = 0;
			foreach (var d in dd)
			{
				listBoxSoundDevices.Items.Add(d);
				if (d == Global.Config.SoundDevice)
				{
					listBoxSoundDevices.SelectedItem = d;
				}
			}
		}

		private void OK_Click(object sender, EventArgs e)
		{
			Global.Config.SoundEnabled = SoundOnCheckBox.Checked;
			Global.Config.MuteFrameAdvance = MuteFrameAdvance.Checked;
			Global.Config.SoundVolume = SoundVolBar.Value;
			Global.Config.SoundThrottle = ThrottlecheckBox.Checked;
			Global.Config.SoundDevice = (string)listBoxSoundDevices.SelectedItem ?? "<default>";
			GlobalWin.Sound.ChangeVolume(Global.Config.SoundVolume);
			GlobalWin.Sound.UpdateSoundSettings();
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
			//This is changed through the user or the Above Scroll Bar
			//Is it Zero?  Mute
			if (SoundVolBar.Value == 0)
			{
				SoundOnCheckBox.Checked = false;
			}
			// Not Zero.  Unmute
			else
			{
				SoundOnCheckBox.Checked = true;
			}
		}

		private void SoundOnCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			UpdateSoundDialog();
		}
		private void UpdateSoundDialog()
		{
			//Ocean Prince commented this out
			//SoundVolGroup.Enabled =
			MuteFrameAdvance.Enabled =
			ThrottlecheckBox.Enabled =
				SoundOnCheckBox.Checked;
		}
	}
}
