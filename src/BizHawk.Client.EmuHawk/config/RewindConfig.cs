using System.Numerics;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class RewindConfig : Form
	{
		private ulong _avgStateSize;

		private readonly Config _config;

		private readonly double _framerate;

		private readonly Action _recreateRewinder;

		private readonly Func<IRewinder> _getRewinder;

		private readonly IStatable _statableCore;

		public RewindConfig(
			Config config,
			double framerate,
			IStatable statableCore,
			Action recreateRewinder,
			Func<IRewinder> getRewinder)
		{
			_config = config;
			_framerate = framerate;
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
				var fullnessRatio = rewinder.FullnessRatio;
				FullnessLabel.Text = $"{fullnessRatio:P2}";
				var stateCount = rewinder.Count;
				RewindFramesUsedLabel.Text = stateCount.ToString();
				_avgStateSize = stateCount is 0 ? 0UL : (ulong) Math.Round(rewinder.Size * fullnessRatio / stateCount);
			}
			else
			{
				FullnessLabel.Text = "N/A";
				RewindFramesUsedLabel.Text = "N/A";
				_avgStateSize = (ulong) _statableCore.CloneSavestate().Length;
			}

			RewindEnabledBox.Checked = _config.Rewind.Enabled;
			UseCompression.Checked = _config.Rewind.UseCompression;
			cbDeltaCompression.Checked = _config.Rewind.UseDelta;
			BufferSizeUpDown.Value = Math.Max(
				BufferSizeUpDown.Minimum,
				_config.Rewind.BufferSize < 0L
					? 0.0M
					: new decimal(BitOperations.Log2(unchecked((ulong) _config.Rewind.BufferSize)))
			);
			TargetFrameLengthRadioButton.Checked = !_config.Rewind.UseFixedRewindInterval;
			TargetRewindIntervalRadioButton.Checked = _config.Rewind.UseFixedRewindInterval;
			TargetFrameLengthNumeric.Value = Math.Max(_config.Rewind.TargetFrameLength, TargetFrameLengthNumeric.Minimum);
			TargetRewindIntervalNumeric.Value = Math.Max(_config.Rewind.TargetRewindInterval, TargetRewindIntervalNumeric.Minimum);
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

		private string FormatKB(ulong n)
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
			_config.Rewind.BufferSize = PutRewindSetting(_config.Rewind.BufferSize, 1L << (int) BufferSizeUpDown.Value);
			_config.Rewind.UseFixedRewindInterval = PutRewindSetting(_config.Rewind.UseFixedRewindInterval, TargetRewindIntervalRadioButton.Checked);
			_config.Rewind.TargetFrameLength = PutRewindSetting(_config.Rewind.TargetFrameLength, (int)TargetFrameLengthNumeric.Value);
			_config.Rewind.TargetRewindInterval = PutRewindSetting(_config.Rewind.TargetRewindInterval, (int)TargetRewindIntervalNumeric.Value);
			_config.Rewind.UseDelta = PutRewindSetting(_config.Rewind.UseDelta, cbDeltaCompression.Checked);

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
			double estFrames = 0.0;
			if (_avgStateSize is not 0UL)
			{
				var bufferSize = 1L << (int) BufferSizeUpDown.Value;
				labelEx1.Text = bufferSize.ToString();
				bufferSize *= 1024 * 1024;
				estFrames = bufferSize / (double) _avgStateSize;
			}
			ApproxFramesLabel.Text = $"{estFrames:n0} frames";
			EstTimeLabel.Text = $"{estFrames / _framerate / 60.0:n} minutes";
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
