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

		public RewindConfig()
		{
			InitializeComponent();
		}

		private void RewindConfig_Load(object sender, EventArgs e)
		{
			StateSize = Global.Emulator.SaveStateBinary().Length;

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
			StateSizeLabel.Text = String.Format("{0:0.00}", num) + " kb";

			SmallLabel1.Text = "Small savestates (less than " + (MediumStateSize / 1024).ToString() + "kb)";
			MediumLabel1.Text = "Medium savestates (" + (MediumStateSize / 1024).ToString()
				+ " - " + (LargeStateSize / 1024) + "kb)";
			LargeLabel1.Text = "Large savestates (" + (LargeStateSize / 1024) + "kb or more)";

			if (StateSize >= LargeStateSize)
			{
				SmallLabel1.Font = new Font(SmallLabel1.Font, FontStyle.Regular);
				MediumLabel1.Font = new Font(SmallLabel1.Font, FontStyle.Regular);
				LargeLabel1.Font = new Font(SmallLabel1.Font, FontStyle.Italic);
			}
			else if (StateSize >= MediumStateSize)
			{
				SmallLabel1.Font = new Font(SmallLabel1.Font, FontStyle.Regular);
				MediumLabel1.Font = new Font(SmallLabel1.Font, FontStyle.Italic);
				LargeLabel1.Font = new Font(SmallLabel1.Font, FontStyle.Regular);
			}
			else
			{
				SmallLabel1.Font = new Font(SmallLabel1.Font, FontStyle.Italic);
				MediumLabel1.Font = new Font(SmallLabel1.Font, FontStyle.Regular);
				LargeLabel1.Font = new Font(SmallLabel1.Font, FontStyle.Regular);
			}
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
	}
}
