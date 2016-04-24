using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class StateHistorySettingsForm : Form
	{
		public IStatable Statable { get; set; }

		private readonly TasStateManagerSettings Settings;
		private decimal _stateSizeMb;
		public StateHistorySettingsForm(TasStateManagerSettings settings)
		{
			Settings = settings;
			InitializeComponent();
		}

		private void StateHistorySettings_Load(object sender, EventArgs e)
		{
			_stateSizeMb = Statable.SaveStateBinary().Length / (decimal)1024 / (decimal)1024;

			if (Environment.Is64BitProcess) // ?
				MemCapacityNumeric.Maximum = 1024 * 8;
			else
				MemCapacityNumeric.Maximum = 1024;

			MemCapacityNumeric.Value = Settings.Capacitymb < MemCapacityNumeric.Maximum ?
				Settings.Capacitymb : MemCapacityNumeric.Maximum;
			DiskCapacityNumeric.Value = Settings.DiskCapacitymb < MemCapacityNumeric.Maximum ?
				Settings.DiskCapacitymb : MemCapacityNumeric.Maximum;
			SaveCapacityNumeric.Value = Settings.DiskSaveCapacitymb < MemCapacityNumeric.Maximum ?
				Settings.DiskSaveCapacitymb : MemCapacityNumeric.Maximum;

			StateGap.Value = Settings.StateGap;
			SavestateSizeLabel.Text = Math.Round(_stateSizeMb, 2).ToString() + " mb";
			CapacityNumeric_ValueChanged(null, null);
			SaveCapacityNumeric_ValueChanged(null, null);
			BranchStatesInTasproj.Checked = Settings.BranchStatesInTasproj;
			EraseBranchStatesFirst.Checked = Settings.EraseBranchStatesFirst;
		}

		private int MaxStatesInCapacity
		{
			get { return (int)Math.Floor(MemCapacityNumeric.Value / _stateSizeMb)
				+ (int)Math.Floor(DiskCapacityNumeric.Value / _stateSizeMb);
			}
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			Settings.Capacitymb = (int)MemCapacityNumeric.Value;
			Settings.DiskCapacitymb = (int)DiskCapacityNumeric.Value;
			Settings.DiskSaveCapacitymb = (int)SaveCapacityNumeric.Value;
			Settings.StateGap = (int)StateGap.Value;
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
			Settings.BranchStatesInTasproj = BranchStatesInTasproj.Checked;
		}

		private void EraseBranchStatesFIrst_CheckedChanged(object sender, EventArgs e)
		{
			Settings.EraseBranchStatesFirst = EraseBranchStatesFirst.Checked;
		}

		private void StateGap_ValueChanged(object sender, EventArgs e)
		{
			NumFramesLabel.Text = ((StateGap.Value == 0) ? "frame" : (1 << (int)StateGap.Value).ToString() + " frames");
		}
	}
}
