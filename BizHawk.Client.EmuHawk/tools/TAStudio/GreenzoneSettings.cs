using System;
using System.Windows.Forms;

using BizHawk.Emulation.Common;
using BizHawk.Client.Common;

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
			_stateSizeMb = Statable.SaveStateBinary().Length / (decimal)1024 / (decimal)1024;

			if (Environment.Is64BitProcess) // ?
			{
				MemCapacityNumeric.Maximum = 1024 * 8;
			}
			else
			{
				MemCapacityNumeric.Maximum = 1024;
			}

			MemCapacityNumeric.Value = _settings.Capacitymb < MemCapacityNumeric.Maximum ?
				_settings.Capacitymb : MemCapacityNumeric.Maximum;
			DiskCapacityNumeric.Value = _settings.DiskCapacitymb < MemCapacityNumeric.Maximum ?
				_settings.DiskCapacitymb : MemCapacityNumeric.Maximum;
			SaveCapacityNumeric.Value = _settings.DiskSaveCapacitymb < MemCapacityNumeric.Maximum ?
				_settings.DiskSaveCapacitymb : MemCapacityNumeric.Maximum;

			StateGap.Value = _settings.StateGap;
			SavestateSizeLabel.Text = Math.Round(_stateSizeMb, 2).ToString() + " mb";
			CapacityNumeric_ValueChanged(null, null);
			SaveCapacityNumeric_ValueChanged(null, null);
			BranchStatesInTasproj.Checked = _settings.BranchStatesInTasproj;
			EraseBranchStatesFirst.Checked = _settings.EraseBranchStatesFirst;
		}

		private int MaxStatesInCapacity => (int)Math.Floor(MemCapacityNumeric.Value / _stateSizeMb)
			+ (int)Math.Floor(DiskCapacityNumeric.Value / _stateSizeMb);

		private void OkBtn_Click(object sender, EventArgs e)
		{
			_settings.Capacitymb = (int)MemCapacityNumeric.Value;
			_settings.DiskCapacitymb = (int)DiskCapacityNumeric.Value;
			_settings.DiskSaveCapacitymb = (int)SaveCapacityNumeric.Value;
			_settings.StateGap = (int)StateGap.Value;
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
			NumSaveStatesLabel.Text = ((int)Math.Floor(SaveCapacityNumeric.Value / _stateSizeMb)).ToString();
		}

		private void BranchStatesInTasproj_CheckedChanged(object sender, EventArgs e)
		{
			_settings.BranchStatesInTasproj = BranchStatesInTasproj.Checked;
		}

		private void EraseBranchStatesFIrst_CheckedChanged(object sender, EventArgs e)
		{
			_settings.EraseBranchStatesFirst = EraseBranchStatesFirst.Checked;
		}

		private void StateGap_ValueChanged(object sender, EventArgs e)
		{
			NumFramesLabel.Text = StateGap.Value == 0
				? "frame"
				: $"{1 << (int)StateGap.Value} frames";
		}
	}
}
