using System;
using System.Windows.Forms;

using BizHawk.Emulation.Common;
using BizHawk.Client.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Client.EmuHawk
{
	public partial class StateHistorySettingsForm : Form
	{
		public IStatable Statable { get; set; }

		private readonly TasStateManagerSettings _settings;
		private decimal _stateSizeMb;

		public StateHistorySettingsForm(TasStateManagerSettings settings)
		{
			_settings = settings;
			InitializeComponent();
		}

		private void StateHistorySettings_Load(object sender, EventArgs e)
		{
			_stateSizeMb = Statable.SaveStateBinary().Length / 1048576.0M;

			MemCapacityNumeric.Maximum = 1024 * 8;
			MemCapacityNumeric.Minimum = _stateSizeMb + 1;

			MemStateGapDividerNumeric.Maximum = Statable.SaveStateBinary().Length / 1024 / 2 + 1;
			MemStateGapDividerNumeric.Minimum = Math.Max(Statable.SaveStateBinary().Length / 1024 / 16, 1);

			MemCapacityNumeric.Value = NumberExtensions.Clamp(_settings.CapacityMb, MemCapacityNumeric.Minimum, MemCapacityNumeric.Maximum);
			DiskCapacityNumeric.Value = NumberExtensions.Clamp(_settings.DiskCapacityMb, MemCapacityNumeric.Minimum, MemCapacityNumeric.Maximum);
			FileCapacityNumeric.Value = NumberExtensions.Clamp(_settings.DiskSaveCapacityMb, MemCapacityNumeric.Minimum, MemCapacityNumeric.Maximum);
			MemStateGapDividerNumeric.Value = NumberExtensions.Clamp(_settings.MemStateGapDivider, MemStateGapDividerNumeric.Minimum, MemStateGapDividerNumeric.Maximum);

			FileStateGapNumeric.Value = _settings.FileStateGap;
			SavestateSizeLabel.Text = $"{Math.Round(_stateSizeMb, 2)} MB";
			CapacityNumeric_ValueChanged(null, null);
			SaveCapacityNumeric_ValueChanged(null, null);
		}

		private int MaxStatesInCapacity => (int)Math.Floor(MemCapacityNumeric.Value / _stateSizeMb)
			+ (int)Math.Floor(DiskCapacityNumeric.Value / _stateSizeMb);

		private void OkBtn_Click(object sender, EventArgs e)
		{
			_settings.CapacityMb = (int)MemCapacityNumeric.Value;
			_settings.DiskCapacityMb = (int)DiskCapacityNumeric.Value;
			_settings.DiskSaveCapacityMb = (int)FileCapacityNumeric.Value;
			_settings.MemStateGapDivider = (int)MemStateGapDividerNumeric.Value;
			_settings.FileStateGap = (int)FileStateGapNumeric.Value;
			DialogResult = DialogResult.OK;
			Close();
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void CapacityNumeric_ValueChanged(object sender, EventArgs e)
		{
			// TODO: Setting space for 2.6 (2) states in memory and 2.6 (2) on disk results in 5 total.
			// Easy to fix the display, but the way TasStateManager works the total used actually is 5.
			NumStatesLabel.Text = MaxStatesInCapacity.ToString();
		}

		private void SaveCapacityNumeric_ValueChanged(object sender, EventArgs e)
		{
			NumSaveStatesLabel.Text = ((int)Math.Floor(FileCapacityNumeric.Value / _stateSizeMb)).ToString();
		}

		private void FileStateGap_ValueChanged(object sender, EventArgs e)
		{
			FileNumFramesLabel.Text = FileStateGapNumeric.Value == 0
				? "frame"
				: $"{1 << (int)FileStateGapNumeric.Value} frames";
		}

		private void MemStateGapDivider_ValueChanged(object sender, EventArgs e)
		{
			int val = (int)(Statable.SaveStateBinary().Length / MemStateGapDividerNumeric.Value / 1024);

			if (val <= 1)
				MemStateGapDividerNumeric.Maximum = MemStateGapDividerNumeric.Value;

			MemFramesLabel.Text = val <= 1
				? "frame"
				: $"{val} frames";
		}
	}
}
