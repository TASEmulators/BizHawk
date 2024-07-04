using System.Collections.Generic;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class SoundConfig : Form, IDialogParent
	{
		private readonly Config _config;

		private readonly Func<ESoundOutputMethod, IEnumerable<string>> _getDeviceNamesCallback;

		private bool _programmaticallyChangingValue;

		public IDialogController DialogController { get; }

		public SoundConfig(IDialogController dialogController, Config config, Func<ESoundOutputMethod, IEnumerable<string>> getDeviceNamesCallback)
		{
			_config = config;
			_getDeviceNamesCallback = getDeviceNamesCallback;
			DialogController = dialogController;
			InitializeComponent();
		}

		private void SoundConfig_Load(object sender, EventArgs e)
		{
			_programmaticallyChangingValue = true;

			cbEnableMaster.Checked = _config.SoundEnabled;
			cbEnableNormal.Checked = _config.SoundEnabledNormal;
			cbEnableRWFF.Checked = _config.SoundEnabledRWFF;
			cbMuteFrameAdvance.Checked = _config.MuteFrameAdvance;

			rbOutputMethodXAudio2.Enabled = HostCapabilityDetector.HasXAudio2;

			rbOutputMethodXAudio2.Checked = _config.SoundOutputMethod == ESoundOutputMethod.XAudio2;
			rbOutputMethodOpenAL.Checked = _config.SoundOutputMethod == ESoundOutputMethod.OpenAL;
			BufferSizeNumeric.Value = _config.SoundBufferSizeMs;
			tbNormal.Value = _config.SoundVolume;
			nudNormal.Value = _config.SoundVolume;
			tbRWFF.Value = _config.SoundVolumeRWFF;
			nudRWFF.Value = _config.SoundVolumeRWFF;
			UpdateSoundDialog();

			_programmaticallyChangingValue = false;
		}

		private ESoundOutputMethod GetSelectedOutputMethod()
		{
			if (!OSTailoredCode.IsUnixHost)
			{
				if (rbOutputMethodXAudio2.Checked) return ESoundOutputMethod.XAudio2;
			}
			if (rbOutputMethodOpenAL.Checked) return ESoundOutputMethod.OpenAL;
			return ESoundOutputMethod.Dummy;
		}

		private void Ok_Click(object sender, EventArgs e)
		{
			_config.SoundEnabled = cbEnableMaster.Checked;
			_config.SoundEnabledNormal = cbEnableNormal.Checked;
			_config.SoundEnabledRWFF = cbEnableRWFF.Checked;
			_config.MuteFrameAdvance = cbMuteFrameAdvance.Checked;
			_config.SoundOutputMethod = GetSelectedOutputMethod();
			_config.SoundBufferSizeMs = (int)BufferSizeNumeric.Value;
			_config.SoundVolume = tbNormal.Value;
			_config.SoundVolumeRWFF = tbRWFF.Value;
			_config.SoundDevice = (string)listBoxSoundDevices.SelectedItem ?? "<default>";
			DialogResult = DialogResult.OK;
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void PopulateDeviceList()
		{
			listBoxSoundDevices.Items.Clear();
			listBoxSoundDevices.Items.Add("<default>");
			listBoxSoundDevices.SelectedIndex = 0;
			foreach (var name in _getDeviceNamesCallback(GetSelectedOutputMethod()))
			{
				listBoxSoundDevices.Items.Add(name);
				if (name == _config.SoundDevice)
				{
					listBoxSoundDevices.SelectedItem = name;
				}
			}
		}

		private void OutputMethodRadioButtons_CheckedChanged(object sender, EventArgs e)
		{
			if (!((RadioButton)sender).Checked)
			{
				return;
			}

			PopulateDeviceList();
		}

		private void TrackBar1_Scroll(object sender, EventArgs e)
		{
			nudNormal.Value = tbNormal.Value;
		}

		private void TbRwff_Scroll(object sender, EventArgs e)
		{
			nudRWFF.Value = tbRWFF.Value;
		}

		private void SoundVolNumeric_ValueChanged(object sender, EventArgs e)
		{
			tbNormal.Value = (int)nudNormal.Value;

			// If the user is changing the volume, automatically turn on/off sound accordingly
			if (!_programmaticallyChangingValue)
			{
				cbEnableNormal.Checked = tbNormal.Value != 0;
			}
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

		private void nudRWFF_ValueChanged(object sender, EventArgs e)
		{
			tbRWFF.Value = (int)nudRWFF.Value;
		}
	}
}
