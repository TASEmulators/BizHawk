using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using BizHawk.Emulation.Cores.Nintendo.SNES;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class SNESOptions : Form
	{
		public SNESOptions()
		{
			InitializeComponent();
		}

		public string Profile
		{
			get { return rbCompatibility.Checked ? "Compatibility" : "Performance"; }
			set
			{
				rbCompatibility.Checked = (value == "Compatibility");
				rbPerformance.Checked = (value == "Performance");
			}
		}

		public bool UseRingBuffer
		{
			get { return cbRingbuf.Checked; }
			set { cbRingbuf.Checked = value; }
		}

		public bool AlwaysDoubleSize
		{
			get { return cbDoubleSize.Checked; }
			set { cbDoubleSize.Checked = value; }
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			DialogResult = System.Windows.Forms.DialogResult.OK;
			Close();
		}

		private void btnCancel_Click(object sender, EventArgs e)
		{
			DialogResult = System.Windows.Forms.DialogResult.Cancel;
			Close();
		}

		public static void DoSettingsDialog(IWin32Window owner)
		{
			var s = (LibsnesCore.SnesSettings)Global.Emulator.GetSettings();
			var ss = (LibsnesCore.SnesSyncSettings)Global.Emulator.GetSyncSettings();
			var dlg = new SNESOptions
			{
				UseRingBuffer = s.UseRingBuffer,
				AlwaysDoubleSize = s.AlwaysDoubleSize,
				Profile = ss.Profile
			};

			var result = dlg.ShowDialog(owner);
			if (result == DialogResult.OK)
			{
				s.UseRingBuffer = dlg.UseRingBuffer;
				s.AlwaysDoubleSize = dlg.AlwaysDoubleSize;
				ss.Profile = dlg.Profile;
				GlobalWin.MainForm.PutCoreSettings(s);
				GlobalWin.MainForm.PutCoreSyncSettings(ss);
			}
		}
	}
}
