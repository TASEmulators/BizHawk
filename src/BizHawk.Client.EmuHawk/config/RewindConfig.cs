using System;
using System.Windows.Forms;
using System.Drawing;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class RewindConfig : Form
	{
		private readonly Rewinder _rewinder;
		private readonly Config _config;
		private readonly IStatable _statableCore;

		private long _stateSize;
		private int _mediumStateSize;
		private int _largeStateSize;
		private int _stateSizeCategory = 1; // 1 = small, 2 = med, 3 = large // TODO: enum

		public RewindConfig(Rewinder rewinder, Config config, IStatable statableCore)
		{
			_rewinder = rewinder;
			_config = config;
			_statableCore = statableCore;
			InitializeComponent();
		}

		private void RewindConfig_Load(object sender, EventArgs e)
		{
			if (_rewinder.HasBuffer)
			{
				FullnessLabel.Text = $"{_rewinder.FullnessRatio * 100:0.00}%";
				RewindFramesUsedLabel.Text = _rewinder.Count.ToString();
			}
			else
			{
				FullnessLabel.Text = "N/A";
				RewindFramesUsedLabel.Text = "N/A";
			}

			RewindSpeedNumeric.Value = _config.Rewind.SpeedMultiplier;
			DiskBufferCheckbox.Checked = _config.Rewind.OnDisk;
			RewindIsThreadedCheckbox.Checked = _config.Rewind.IsThreaded;
			_stateSize = _statableCore.CloneSavestate().Length;
			BufferSizeUpDown.Value = Math.Max(_config.Rewind.BufferSize, BufferSizeUpDown.Minimum);

			_mediumStateSize = _config.Rewind.MediumStateSize;
			_largeStateSize = _config.Rewind.LargeStateSize;

			UseDeltaCompression.Checked = _config.Rewind.UseDelta;

			SmallSavestateNumeric.Value = _config.Rewind.FrequencySmall;
			MediumSavestateNumeric.Value = _config.Rewind.FrequencyMedium;
			LargeSavestateNumeric.Value = _config.Rewind.FrequencyLarge;

			SmallStateEnabledBox.Checked = _config.Rewind.EnabledSmall;
			MediumStateEnabledBox.Checked = _config.Rewind.EnabledMedium;
			LargeStateEnabledBox.Checked = _config.Rewind.EnabledLarge;

			SetSmallEnabled();
			SetMediumEnabled();
			SetLargeEnabled();

			SetStateSize();

			var mediumStateSizeKb = _config.Rewind.MediumStateSize / 1024;
			var largeStateSizeKb = _config.Rewind.LargeStateSize / 1024;

			MediumStateTrackbar.Value = mediumStateSizeKb;
			MediumStateUpDown.Value = mediumStateSizeKb;
			LargeStateTrackbar.Value = largeStateSizeKb;
			LargeStateUpDown.Value = largeStateSizeKb;

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

		private string FormatKB(long n)
		{
			double num = n / 1024.0;

			if (num >= 1024)
			{
				num /= 1024.0;
				return $"{num:0.00} MB";
			}

			return $"{num:0.00} KB";
		}

		private void SetStateSize()
		{
			StateSizeLabel.Text = FormatKB(_stateSize);

			SmallLabel.Text = $"Small savestates (less than {_mediumStateSize / 1024}KB)";
			MediumLabel.Text = $"Medium savestates ({_mediumStateSize / 1024} - {_largeStateSize / 1024}KB)";
			LargeLabel.Text = $"Large savestates ({_largeStateSize / 1024}KB or more)";

			if (_stateSize >= _largeStateSize)
			{
				_stateSizeCategory = 3;
				SmallLabel.Font = new Font(SmallLabel.Font, FontStyle.Regular);
				MediumLabel.Font = new Font(SmallLabel.Font, FontStyle.Regular);
				LargeLabel.Font = new Font(SmallLabel.Font, FontStyle.Italic);
			}
			else if (_stateSize >= _mediumStateSize)
			{
				_stateSizeCategory = 2;
				SmallLabel.Font = new Font(SmallLabel.Font, FontStyle.Regular);
				MediumLabel.Font = new Font(SmallLabel.Font, FontStyle.Italic);
				LargeLabel.Font = new Font(SmallLabel.Font, FontStyle.Regular);
			}
			else
			{
				_stateSizeCategory = 1;
				SmallLabel.Font = new Font(SmallLabel.Font, FontStyle.Italic);
				MediumLabel.Font = new Font(SmallLabel.Font, FontStyle.Regular);
				LargeLabel.Font = new Font(SmallLabel.Font, FontStyle.Regular);
			}

			CalculateEstimates();
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
			_config.Rewind.UseDelta = PutRewindSetting(_config.Rewind.UseDelta, UseDeltaCompression.Checked);
			_config.Rewind.EnabledSmall = PutRewindSetting(_config.Rewind.EnabledSmall, SmallStateEnabledBox.Checked);
			_config.Rewind.EnabledMedium = PutRewindSetting(_config.Rewind.EnabledMedium, MediumStateEnabledBox.Checked);
			_config.Rewind.EnabledLarge = PutRewindSetting(_config.Rewind.EnabledLarge, LargeStateEnabledBox.Checked);
			_config.Rewind.FrequencySmall = PutRewindSetting(_config.Rewind.FrequencySmall, (int)SmallSavestateNumeric.Value);
			_config.Rewind.FrequencyMedium = PutRewindSetting(_config.Rewind.FrequencyMedium, (int)MediumSavestateNumeric.Value);
			_config.Rewind.FrequencyLarge = PutRewindSetting(_config.Rewind.FrequencyLarge, (int)LargeSavestateNumeric.Value);
			_config.Rewind.MediumStateSize = PutRewindSetting(_config.Rewind.MediumStateSize, (int)MediumStateUpDown.Value * 1024);
			_config.Rewind.LargeStateSize = PutRewindSetting(_config.Rewind.LargeStateSize, (int)LargeStateUpDown.Value * 1024);
			_config.Rewind.BufferSize = PutRewindSetting(_config.Rewind.BufferSize, (int)BufferSizeUpDown.Value);
			_config.Rewind.OnDisk = PutRewindSetting(_config.Rewind.OnDisk, DiskBufferCheckbox.Checked);
			_config.Rewind.IsThreaded = PutRewindSetting(_config.Rewind.IsThreaded, RewindIsThreadedCheckbox.Checked);

			if (TriggerRewindSettingsReload)
			{
				_rewinder.Initialize(_statableCore, _config.Rewind);
			}

			// These settings are not used by DoRewindSettings
			_config.Rewind.SpeedMultiplier = (int)RewindSpeedNumeric.Value;
			_config.Savestates.CompressionLevelNormal = (int)nudCompression.Value;
			if (rbStatesBinary.Checked) _config.Savestates.Type = SaveStateType.Binary;
			if (rbStatesText.Checked) _config.Savestates.Type = SaveStateType.Text;
			_config.Savestates.MakeBackups = BackupSavestatesCheckbox.Checked;
			_config.Savestates.SaveScreenshot = ScreenshotInStatesCheckbox.Checked;
			_config.Savestates.NoLowResLargeScreenshots = !LowResLargeScreenshotsCheckbox.Checked;
			_config.Savestates.BigScreenshotSize = (int)BigScreenshotNumeric.Value * 1024;

			DialogResult = DialogResult.OK;
			Close();
		}

		private void SetSmallEnabled()
		{
			SmallLabel.Enabled = SmallLabel2.Enabled
				= SmallSavestateNumeric.Enabled = SmallLabel3.Enabled
				= SmallStateEnabledBox.Checked;
		}

		private void SetMediumEnabled()
		{
			MediumLabel.Enabled = MediumLabel2.Enabled
				= MediumSavestateNumeric.Enabled = MediumLabel3.Enabled
				= MediumStateEnabledBox.Checked;
		}

		private void SetLargeEnabled()
		{
			LargeLabel.Enabled = LargeLabel2.Enabled
				= LargeSavestateNumeric.Enabled = LargeLabel3.Enabled
				= LargeStateEnabledBox.Checked;
		}

		private void SmallStateEnabledBox_CheckStateChanged(object sender, EventArgs e)
		{
			SetSmallEnabled();
		}

		private void MediumStateEnabledBox_CheckStateChanged(object sender, EventArgs e)
		{
			SetMediumEnabled();
		}

		private void LargeStateEnabledBox_CheckStateChanged(object sender, EventArgs e)
		{
			SetLargeEnabled();
		}

		private void LargeLabel_Click(object sender, EventArgs e)
		{
			LargeStateEnabledBox.Checked ^= true;
		}

		private void MediumLabel_Click(object sender, EventArgs e)
		{
			MediumStateEnabledBox.Checked ^= true;
		}

		private void SmallLabel_Click(object sender, EventArgs e)
		{
			SmallStateEnabledBox.Checked ^= true;
		}

		private void MediumStateTrackBar_ValueChanged(object sender, EventArgs e)
		{
			MediumStateUpDown.Value = ((TrackBar)sender).Value;
			if (MediumStateUpDown.Value > LargeStateUpDown.Value)
			{
				LargeStateUpDown.Value = MediumStateUpDown.Value;
				LargeStateTrackbar.Value = (int)MediumStateUpDown.Value;
			}

			_mediumStateSize = MediumStateTrackbar.Value * 1024;
			_largeStateSize = LargeStateTrackbar.Value * 1024;
			SetStateSize();
		}

		private void MediumStateUpDown_ValueChanged(object sender, EventArgs e)
		{
			MediumStateTrackbar.Value = (int)((NumericUpDown)sender).Value;
			if (MediumStateUpDown.Value > LargeStateUpDown.Value)
			{
				LargeStateUpDown.Value = MediumStateUpDown.Value;
				LargeStateTrackbar.Value = (int)MediumStateUpDown.Value;
			}

			_mediumStateSize = MediumStateTrackbar.Value * 1024;
			_largeStateSize = LargeStateTrackbar.Value * 1024;
			SetStateSize();
		}

		private void LargeStateTrackBar_ValueChanged(object sender, EventArgs e)
		{
			if (LargeStateTrackbar.Value < MediumStateTrackbar.Value)
			{
				LargeStateTrackbar.Value = MediumStateTrackbar.Value;
				LargeStateUpDown.Value = MediumStateTrackbar.Value;
			}
			else
			{
				LargeStateUpDown.Value = ((TrackBar)sender).Value;
			}

			_mediumStateSize = MediumStateTrackbar.Value * 1024;
			_largeStateSize = LargeStateTrackbar.Value * 1024;
			SetStateSize();
		}

		private void LargeStateUpDown_ValueChanged(object sender, EventArgs e)
		{
			if (LargeStateUpDown.Value < MediumStateUpDown.Value)
			{
				LargeStateTrackbar.Value = MediumStateTrackbar.Value;
				LargeStateUpDown.Value = MediumStateTrackbar.Value;
			}
			else
			{
				LargeStateTrackbar.Value = (int)((NumericUpDown)sender).Value;
			}

			_mediumStateSize = MediumStateTrackbar.Value * 1024;
			_largeStateSize = LargeStateTrackbar.Value * 1024;
			SetStateSize();
		}

		private void CalculateEstimates()
		{
			long avgStateSize;

			if (UseDeltaCompression.Checked || _stateSize == 0)
			{
				if (_rewinder.Count > 0)
				{
					avgStateSize = _rewinder.Size / _rewinder.Count;
				}
				else
				{
					avgStateSize = _stateSize;
				}
			}
			else
			{
				avgStateSize = _stateSize;
			}

			var bufferSize = (long)BufferSizeUpDown.Value;
			bufferSize *= 1024 * 1024;
			var estFrames = bufferSize / avgStateSize;

			long estFrequency = 0;
			switch (_stateSizeCategory)
			{
				case 1:
					estFrequency = (long)SmallSavestateNumeric.Value;
					break;
				case 2:
					estFrequency = (long)MediumSavestateNumeric.Value;
					break;
				case 3:
					estFrequency = (long)LargeSavestateNumeric.Value;
					break;
			}

			long estTotalFrames = estFrames * estFrequency;
			double minutes = estTotalFrames / 60 / 60;

			AverageStoredStateSizeLabel.Text = FormatKB(avgStateSize);
			ApproxFramesLabel.Text = $"{estFrames:n0} frames";
			EstTimeLabel.Text = $"{minutes:n} minutes";
		}

		private void BufferSizeUpDown_ValueChanged(object sender, EventArgs e)
		{
			CalculateEstimates();
		}

		private void UseDeltaCompression_CheckedChanged(object sender, EventArgs e)
		{
			CalculateEstimates();
		}

		private void SmallSavestateNumeric_ValueChanged(object sender, EventArgs e)
		{
			CalculateEstimates();
		}

		private void MediumSavestateNumeric_ValueChanged(object sender, EventArgs e)
		{
			CalculateEstimates();
		}

		private void LargeSavestateNumeric_ValueChanged(object sender, EventArgs e)
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
