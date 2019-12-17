using System;
using System.Windows.Forms;
using System.Drawing;

using BizHawk.Emulation.Common.IEmulatorExtensions;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class RewindConfig : Form
	{
		private long _stateSize;
		private int _mediumStateSize;
		private int _largeStateSize;
		private int _stateSizeCategory = 1; // 1 = small, 2 = med, 3 = larg //TODO: enum

		public RewindConfig()
		{
			InitializeComponent();
		}

		private void RewindConfig_Load(object sender, EventArgs e)
		{
			if (Global.Rewinder.HasBuffer)
			{
				FullnessLabel.Text = $"{Global.Rewinder.FullnessRatio * 100:0.00}%";
				RewindFramesUsedLabel.Text = Global.Rewinder.Count.ToString();
			}
			else
			{
				FullnessLabel.Text = "N/A";
				RewindFramesUsedLabel.Text = "N/A";
			}

			RewindSpeedNumeric.Value = Global.Config.RewindSpeedMultiplier;
			DiskBufferCheckbox.Checked = Global.Config.Rewind_OnDisk;
			RewindIsThreadedCheckbox.Checked = Global.Config.Rewind_IsThreaded;
			_stateSize = Global.Emulator.AsStatable().SaveStateBinary().Length;
			BufferSizeUpDown.Value = Math.Max(Global.Config.Rewind_BufferSize, BufferSizeUpDown.Minimum);

			_mediumStateSize = Global.Config.Rewind_MediumStateSize;
			_largeStateSize = Global.Config.Rewind_LargeStateSize;

			UseDeltaCompression.Checked = Global.Config.Rewind_UseDelta;

			SmallSavestateNumeric.Value = Global.Config.RewindFrequencySmall;
			MediumSavestateNumeric.Value = Global.Config.RewindFrequencyMedium;
			LargeSavestateNumeric.Value = Global.Config.RewindFrequencyLarge;

			SmallStateEnabledBox.Checked = Global.Config.RewindEnabledSmall;
			MediumStateEnabledBox.Checked = Global.Config.RewindEnabledMedium;
			LargeStateEnabledBox.Checked = Global.Config.RewindEnabledLarge;

			SetSmallEnabled();
			SetMediumEnabled();
			SetLargeEnabled();

			SetStateSize();

			var mediumStateSizeKb = Global.Config.Rewind_MediumStateSize / 1024;
			var largeStateSizeKb = Global.Config.Rewind_LargeStateSize / 1024;

			MediumStateTrackbar.Value = mediumStateSizeKb;
			MediumStateUpDown.Value = mediumStateSizeKb;
			LargeStateTrackbar.Value = largeStateSizeKb;
			LargeStateUpDown.Value = largeStateSizeKb;

			nudCompression.Value = Global.Config.SaveStateCompressionLevelNormal;

			rbStatesDefault.Checked = Global.Config.SaveStateType == Config.SaveStateTypeE.Default;
			rbStatesBinary.Checked = Global.Config.SaveStateType == Config.SaveStateTypeE.Binary;
			rbStatesText.Checked = Global.Config.SaveStateType == Config.SaveStateTypeE.Text;

			BackupSavestatesCheckbox.Checked = Global.Config.BackupSavestates;
			ScreenshotInStatesCheckbox.Checked = Global.Config.SaveScreenshotWithStates;
			LowResLargeScreenshotsCheckbox.Checked = !Global.Config.NoLowResLargeScreenshotWithStates;
			BigScreenshotNumeric.Value = Global.Config.BigScreenshotSize / 1024;

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
			double num = _stateSize / 1024.0;

			StateSizeLabel.Text = FormatKB(_stateSize);

			SmallLabel1.Text = $"Small savestates (less than {_mediumStateSize / 1024}KB)";
			MediumLabel1.Text = $"Medium savestates ({_mediumStateSize / 1024} - {_largeStateSize / 1024}KB)";
			LargeLabel1.Text = $"Large savestates ({_largeStateSize / 1024}KB or more)";

			if (_stateSize >= _largeStateSize)
			{
				_stateSizeCategory = 3;
				SmallLabel1.Font = new Font(SmallLabel1.Font, FontStyle.Regular);
				MediumLabel1.Font = new Font(SmallLabel1.Font, FontStyle.Regular);
				LargeLabel1.Font = new Font(SmallLabel1.Font, FontStyle.Italic);
			}
			else if (_stateSize >= _mediumStateSize)
			{
				_stateSizeCategory = 2;
				SmallLabel1.Font = new Font(SmallLabel1.Font, FontStyle.Regular);
				MediumLabel1.Font = new Font(SmallLabel1.Font, FontStyle.Italic);
				LargeLabel1.Font = new Font(SmallLabel1.Font, FontStyle.Regular);
			}
			else
			{
				_stateSizeCategory = 1;
				SmallLabel1.Font = new Font(SmallLabel1.Font, FontStyle.Italic);
				MediumLabel1.Font = new Font(SmallLabel1.Font, FontStyle.Regular);
				LargeLabel1.Font = new Font(SmallLabel1.Font, FontStyle.Regular);
			}

			CalculateEstimates();
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			Close();
		}

		private bool TriggerRewindSettingsReload { get; set; }

		private void PutRewindSetting<T>(ref T setting, T value) where T : IEquatable<T>
		{
			if (setting.Equals(value))
			{
				return;
			}

			setting = value;
			TriggerRewindSettingsReload = true;
		}

		private void Ok_Click(object sender, EventArgs e)
		{
			// These settings are used by DoRewindSettings, which we'll only call if anything actually changed (i.e. preserve rewind history if possible)
			PutRewindSetting(ref Global.Config.RewindEnabledSmall, SmallStateEnabledBox.Checked);
			PutRewindSetting(ref Global.Config.RewindEnabledMedium, MediumStateEnabledBox.Checked);
			PutRewindSetting(ref Global.Config.RewindEnabledLarge, LargeStateEnabledBox.Checked);
			PutRewindSetting(ref Global.Config.RewindFrequencySmall, (int)SmallSavestateNumeric.Value);
			PutRewindSetting(ref Global.Config.RewindFrequencyMedium, (int)MediumSavestateNumeric.Value);
			PutRewindSetting(ref Global.Config.RewindFrequencyLarge, (int)LargeSavestateNumeric.Value);
			PutRewindSetting(ref Global.Config.Rewind_OnDisk, DiskBufferCheckbox.Checked);
			PutRewindSetting(ref Global.Config.Rewind_UseDelta, UseDeltaCompression.Checked);
			PutRewindSetting(ref Global.Config.Rewind_IsThreaded, RewindIsThreadedCheckbox.Checked);
			PutRewindSetting(ref Global.Config.Rewind_BufferSize, (int)BufferSizeUpDown.Value);
			PutRewindSetting(ref Global.Config.Rewind_MediumStateSize, (int)MediumStateUpDown.Value * 1024);
			PutRewindSetting(ref Global.Config.Rewind_LargeStateSize, (int)LargeStateUpDown.Value * 1024);
			if (TriggerRewindSettingsReload)
			{
				Global.Rewinder.Initialize();
			}

			// These settings are not used by DoRewindSettings
			Global.Config.RewindSpeedMultiplier = (int)RewindSpeedNumeric.Value;
			Global.Config.SaveStateCompressionLevelNormal = (int)nudCompression.Value;
			if (rbStatesDefault.Checked) Global.Config.SaveStateType = Config.SaveStateTypeE.Default;
			if (rbStatesBinary.Checked) Global.Config.SaveStateType = Config.SaveStateTypeE.Binary;
			if (rbStatesText.Checked) Global.Config.SaveStateType = Config.SaveStateTypeE.Text;
			Global.Config.BackupSavestates = BackupSavestatesCheckbox.Checked;
			Global.Config.SaveScreenshotWithStates = ScreenshotInStatesCheckbox.Checked;
			Global.Config.NoLowResLargeScreenshotWithStates = !LowResLargeScreenshotsCheckbox.Checked;
			Global.Config.BigScreenshotSize = (int)BigScreenshotNumeric.Value * 1024;

			Close();
		}

		private void SetSmallEnabled()
		{
			SmallLabel1.Enabled = SmallLabel2.Enabled
				= SmallSavestateNumeric.Enabled = SmallLabel3.Enabled
				= SmallStateEnabledBox.Checked;
		}

		private void SetMediumEnabled()
		{
			MediumLabel1.Enabled = MediumLabel2.Enabled
				= MediumSavestateNumeric.Enabled = MediumLabel3.Enabled
				= MediumStateEnabledBox.Checked;
		}

		private void SetLargeEnabled()
		{
			LargeLabel1.Enabled = LargeLabel2.Enabled
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

		private void LargeLabel1_Click(object sender, EventArgs e)
		{
			LargeStateEnabledBox.Checked ^= true;
		}

		private void MediumLabel1_Click(object sender, EventArgs e)
		{
			MediumStateEnabledBox.Checked ^= true;
		}

		private void SmallLabel1_Click(object sender, EventArgs e)
		{
			SmallStateEnabledBox.Checked ^= true;
		}

		private void MediumStateTrackbar_ValueChanged(object sender, EventArgs e)
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

		private void LargeStateTrackbar_ValueChanged(object sender, EventArgs e)
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
				if (Global.Rewinder.Count > 0)
				{
					avgStateSize = Global.Rewinder.Size / Global.Rewinder.Count;
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
			nudCompression.Value = Config.DefaultSaveStateCompressionLevelNormal;
		}
	}
}
