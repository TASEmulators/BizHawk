using System;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class RewindConfig : Form
	{
		private double _avgStateSize;

		private readonly Config _config;

		private readonly Action _recreateRewinder;

		private readonly Func<IRewinder> _getRewinder;

		private readonly IStatable _statableCore;

		public RewindConfig(Config config, Action recreateRewinder, Func<IRewinder> getRewinder, IStatable statableCore)
		{
			_config = config;
			_recreateRewinder = recreateRewinder;
			_getRewinder = getRewinder;
			_statableCore = statableCore;
			InitializeComponent();
			btnResetCompression.Image = Properties.Resources.Reboot;
		}

		private void RewindConfig_Load(object sender, EventArgs e)
		{
			//TODO can this be moved to the ctor post-InitializeComponent?
			var rewinder = _getRewinder();
			if (rewinder?.Active == true)
			{
				FullnessLabel.Text = $"{rewinder.FullnessRatio * 100:0.00}%";
				RewindFramesUsedLabel.Text = rewinder.Count.ToString();
				_avgStateSize = rewinder.Size * rewinder.FullnessRatio / rewinder.Count;
			}
			else
			{
				FullnessLabel.Text = "N/A";
				RewindFramesUsedLabel.Text = "N/A";
				_avgStateSize = _statableCore.CloneSavestate().Length;
			}

			RewindEnabledBox.Checked = _config.Rewind.Enabled;
			UseCompression.Checked = _config.Rewind.UseCompression;
			BufferSizeUpDown.Value = Math.Max(_config.Rewind.BufferSize, BufferSizeUpDown.Minimum);
			TargetFrameLengthNumeric.Value = Math.Max(_config.Rewind.TargetFrameLength, TargetFrameLengthNumeric.Minimum);
			StateSizeLabel.Text = FormatKB(_avgStateSize);
			CalculateEstimates();

			nudCompression.Value = _config.Savestates.CompressionLevelNormal;

			rbStatesBinary.Checked = _config.Savestates.Type == SaveStateType.Binary;
			rbStatesText.Checked = _config.Savestates.Type == SaveStateType.Text;

			BackupSavestatesCheckbox.Checked = _config.Savestates.MakeBackups;
			ScreenshotInStatesCheckbox.Checked = _config.Savestates.SaveScreenshot;
			LowResLargeScreenshotsCheckbox.Checked = !_config.Savestates.NoLowResLargeScreenshots;
			BigScreenshotNumeric.Value = _config.Savestates.BigScreenshotSize / 1024;

			ScreenshotInStatesCheckbox_CheckedChanged(null, null);
		}

		private void ScreenshotInStatesCheckbox_CheckedChanged(object sender, EventArgs e)
		{
			LowResLargeScreenshotsCheckbox.Enabled =
				BigScreenshotNumeric.Enabled =
				KbLabel.Enabled =
				ScreenshotInStatesCheckbox.Checked;
		}

		private string FormatKB(double n)
		{
			double num = n / 1024.0;

			if (num >= 1024)
			{
				num /= 1024.0;
				return $"{num:0.00} MB";
			}

			return $"{num:0.00} KB";
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			Close();
		}

		private bool TriggerRewindSettingsReload { get; set; }

		private T PutRewindSetting<T>(T setting, T value) where T : IEquatable<T>
		{
			if (setting.Equals(value))
			{
				return setting;
			}

			TriggerRewindSettingsReload = true;
			return value;
		}

		private void Ok_Click(object sender, EventArgs e)
		{
			// These settings are used by DoRewindSettings, which we'll only call if anything actually changed (i.e. preserve rewind history if possible)
			_config.Rewind.UseCompression = PutRewindSetting(_config.Rewind.UseCompression, UseCompression.Checked);
			_config.Rewind.Enabled = PutRewindSetting(_config.Rewind.Enabled, RewindEnabledBox.Checked);
			_config.Rewind.BufferSize = PutRewindSetting(_config.Rewind.BufferSize, (int)BufferSizeUpDown.Value);
			_config.Rewind.TargetFrameLength = PutRewindSetting(_config.Rewind.TargetFrameLength, (int)TargetFrameLengthNumeric.Value);

			// These settings are not used by DoRewindSettings
			_config.Savestates.CompressionLevelNormal = (int)nudCompression.Value;
			if (rbStatesBinary.Checked) _config.Savestates.Type = SaveStateType.Binary;
			if (rbStatesText.Checked) _config.Savestates.Type = SaveStateType.Text;
			_config.Savestates.MakeBackups = BackupSavestatesCheckbox.Checked;
			_config.Savestates.SaveScreenshot = ScreenshotInStatesCheckbox.Checked;
			_config.Savestates.NoLowResLargeScreenshots = !LowResLargeScreenshotsCheckbox.Checked;
			_config.Savestates.BigScreenshotSize = (int)BigScreenshotNumeric.Value * 1024;

			if (TriggerRewindSettingsReload)
			{
				_recreateRewinder();
			}

			DialogResult = DialogResult.OK;
			Close();
		}

		private void CalculateEstimates()
		{
			var bufferSize = (long)BufferSizeUpDown.Value;
			bufferSize *= 1024 * 1024;
			var estFrames = bufferSize / _avgStateSize;

			double estTotalFrames = estFrames;
			double minutes = estTotalFrames / 60 / 60;

			ApproxFramesLabel.Text = $"{estFrames:n0} frames";
			EstTimeLabel.Text = $"{minutes:n} minutes";
		}

		private void BufferSizeUpDown_ValueChanged(object sender, EventArgs e)
		{
			CalculateEstimates();
		}

		private void UseCompression_CheckedChanged(object sender, EventArgs e)
		{
			CalculateEstimates();
		}

		private void NudCompression_ValueChanged(object sender, EventArgs e)
		{
			trackBarCompression.Value = (int)((NumericUpDown)sender).Value;
		}

		private void TrackBarCompression_ValueChanged(object sender, EventArgs e)
		{
			// TODO - make a UserControl which is TrackBar and NUD combined
			nudCompression.Value = ((TrackBar)sender).Value;
		}

		private void BtnResetCompression_Click(object sender, EventArgs e)
		{
			nudCompression.Value = SaveStateConfig.DefaultCompressionLevelNormal;
		}
	}
}
