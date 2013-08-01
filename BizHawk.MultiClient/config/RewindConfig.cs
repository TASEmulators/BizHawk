using System;
using System.Windows.Forms;
using System.Drawing;

namespace BizHawk.MultiClient
{
	public partial class RewindConfig : Form
	{
		private long StateSize;
		private int MediumStateSize;
		private int LargeStateSize;
        private int StateSizeCategory = 1; //1 = small, 2 = med, 3 = larg //TODO: enum
		public RewindConfig()
		{
			InitializeComponent();
		}

		private void RewindConfig_Load(object sender, EventArgs e)
		{
			FullnessLabel.Text = String.Format("{0:0.00}", Global.MainForm.Rewind_FullnessRatio * 100) + "%";
			RewindFramesUsedLabel.Text = Global.MainForm.Rewind_Count.ToString();
			StateSize = Global.Emulator.SaveStateBinary().Length;
			RewindIsThreadedCheckbox.Checked = Global.Config.Rewind_IsThreaded;
			DiskBufferCheckbox.Checked = Global.Config.Rewind_OnDisk;
			BufferSizeUpDown.Value = Global.Config.Rewind_BufferSize;

			MediumStateSize = Global.Config.Rewind_MediumStateSize;
			LargeStateSize = Global.Config.Rewind_LargeStateSize;

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

			int medium_state_size_kb = Global.Config.Rewind_MediumStateSize / 1024;
			int large_state_size_kb = Global.Config.Rewind_LargeStateSize / 1024;

			MediumStateTrackbar.Value = medium_state_size_kb;
			MediumStateUpDown.Value = (decimal)medium_state_size_kb;
			LargeStateTrackbar.Value = large_state_size_kb;
			LargeStateUpDown.Value = (decimal)large_state_size_kb;
		}

		private void SetStateSize()
		{
			double num = StateSize / 1024.0;

            if (num >= 1024)
			{
				num /= 1024.0;
				StateSizeLabel.Text = String.Format("{0:0.00}", num) + " mb";
			}
			else
			{
				StateSizeLabel.Text = String.Format("{0:0.00}", num) + " kb";
			}


			SmallLabel1.Text = "Small savestates (less than " + (MediumStateSize / 1024).ToString() + "kb)";
			MediumLabel1.Text = "Medium savestates (" + (MediumStateSize / 1024).ToString()
				+ " - " + (LargeStateSize / 1024) + "kb)";
			LargeLabel1.Text = "Large savestates (" + (LargeStateSize / 1024) + "kb or more)";

			if (StateSize >= LargeStateSize)
			{
                StateSizeCategory = 3;
                SmallLabel1.Font = new Font(SmallLabel1.Font, FontStyle.Regular);
				MediumLabel1.Font = new Font(SmallLabel1.Font, FontStyle.Regular);
				LargeLabel1.Font = new Font(SmallLabel1.Font, FontStyle.Italic);
			}
			else if (StateSize >= MediumStateSize)
			{
                StateSizeCategory = 2;
                SmallLabel1.Font = new Font(SmallLabel1.Font, FontStyle.Regular);
				MediumLabel1.Font = new Font(SmallLabel1.Font, FontStyle.Italic);
				LargeLabel1.Font = new Font(SmallLabel1.Font, FontStyle.Regular);
			}
			else
			{
                StateSizeCategory = 1;
                SmallLabel1.Font = new Font(SmallLabel1.Font, FontStyle.Italic);
				MediumLabel1.Font = new Font(SmallLabel1.Font, FontStyle.Regular);
				LargeLabel1.Font = new Font(SmallLabel1.Font, FontStyle.Regular);
			}

            CalculateEstimates();
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			Global.OSD.AddMessage("Rewind config aborted");
			Close();
		}

		private void OK_Click(object sender, EventArgs e)
		{
			Global.OSD.AddMessage("Rewind settings saved");

			Global.Config.RewindFrequencySmall = (int)SmallSavestateNumeric.Value;
			Global.Config.RewindFrequencyMedium = (int)MediumSavestateNumeric.Value;
			Global.Config.RewindFrequencyLarge = (int)LargeSavestateNumeric.Value;

			Global.Config.RewindEnabledSmall = SmallStateEnabledBox.Checked;
			Global.Config.RewindEnabledMedium = MediumStateEnabledBox.Checked;
			Global.Config.RewindEnabledLarge = LargeStateEnabledBox.Checked;

			Global.MainForm.DoRewindSettings();

			Global.Config.Rewind_UseDelta = UseDeltaCompression.Checked;

			Global.Config.Rewind_MediumStateSize = (int)(MediumStateUpDown.Value * 1024);
			Global.Config.Rewind_LargeStateSize = (int)(LargeStateUpDown.Value * 1024);
			Global.Config.Rewind_OnDisk = DiskBufferCheckbox.Checked;
			Global.Config.Rewind_BufferSize = (int)BufferSizeUpDown.Value;
			if (Global.Config.Rewind_IsThreaded != RewindIsThreadedCheckbox.Checked)
			{
				Global.MainForm.FlagNeedsReboot();
				Global.Config.Rewind_IsThreaded = RewindIsThreadedCheckbox.Checked;
			}

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
			MediumStateUpDown.Value = (sender as TrackBar).Value;
			if (MediumStateUpDown.Value > LargeStateUpDown.Value)
			{
				LargeStateUpDown.Value = MediumStateUpDown.Value;
				LargeStateTrackbar.Value = (int)MediumStateUpDown.Value;
			}
			MediumStateSize = MediumStateTrackbar.Value * 1024;
			LargeStateSize = LargeStateTrackbar.Value * 1024;
			SetStateSize();
		}

		private void MediumStateUpDown_ValueChanged(object sender, EventArgs e)
		{
			MediumStateTrackbar.Value = (int)(sender as NumericUpDown).Value;
			if (MediumStateUpDown.Value > LargeStateUpDown.Value)
			{
				LargeStateUpDown.Value = MediumStateUpDown.Value;
				LargeStateTrackbar.Value = (int)MediumStateUpDown.Value;
			}
			MediumStateSize = MediumStateTrackbar.Value * 1024;
			LargeStateSize = LargeStateTrackbar.Value * 1024;
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
				LargeStateUpDown.Value = (sender as TrackBar).Value;
			}
			MediumStateSize = MediumStateTrackbar.Value * 1024;
			LargeStateSize = LargeStateTrackbar.Value * 1024;
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
				LargeStateTrackbar.Value = (int)(sender as NumericUpDown).Value;
			}
			MediumStateSize = MediumStateTrackbar.Value * 1024;
			LargeStateSize = LargeStateTrackbar.Value * 1024;
			SetStateSize();
		}

        private void CalculateEstimates()
        {
            long avg_state_size = 0;

            if (UseDeltaCompression.Checked)
            {

                avg_state_size = (long)(Global.MainForm.Rewind_Size / Global.MainForm.Rewind_Count);
            }
            else
            {
                avg_state_size = StateSize;
            }

            long buffer_size = (long)(BufferSizeUpDown.Value);
            buffer_size *= 1024 * 1024;
            long est_frames = buffer_size / avg_state_size;


            
            long est_frequency = 0;
            switch (StateSizeCategory)
            {
                case 1:
                    est_frequency = (long)SmallSavestateNumeric.Value;
                    break;
                case 2:
                    est_frequency = (long)MediumSavestateNumeric.Value;
                    break;
                case 3:
                    est_frequency = (long)LargeSavestateNumeric.Value;
                    break;
            }
            long est_total_frames = est_frames * est_frequency;
            double minutes = est_total_frames / 60 / 60;

            AverageStoredStateSizeLabel.Text = String.Format("{0:n0}", avg_state_size) + " bytes";
            ApproxFramesLabel.Text = String.Format("{0:n0}", est_frames) + " frames";
            EstTimeLabel.Text = String.Format("{0:n}", minutes) + " minutes";
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
	}
}
